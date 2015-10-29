﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Xunit.Abstractions;
using Xunit.Properties;
using Xunit.Sdk;

namespace Xunit
{
	class VsClient : IVsClient
	{
		static readonly TraceSource tracer = Constants.Tracer;
		const string BindingPathKey = "{00000000-17C9-470C-AED2-2D4E97CC5686}";
		const int MaxOperationRetries = 5;
		const int RetryInterval = 200;

		bool initializedExtension;

		string visualStudioVersion;
		string pipeName;
		string rootSuffix;
		string devEnvPath;

		IChannel clientChannel;
		IVsRemoteRunner runner;

		ConcurrentDictionary<IMessageBus, RemoteMessageBus> remoteBuses = new ConcurrentDictionary<IMessageBus, RemoteMessageBus>();
		ConcurrentBag<MarshalByRefObject> remoteObjects = new ConcurrentBag<MarshalByRefObject> ();

		public VsClient (string visualStudioVersion, string pipeName, string rootSuffix)
		{
			this.visualStudioVersion = visualStudioVersion;
			this.pipeName = pipeName;
			this.rootSuffix = rootSuffix;

			devEnvPath = GetDevEnvPath ();
		}

		public Process Process { get; private set; }

		public void Recycle ()
		{
			Stop ();
		}

		public async Task<RunSummary> RunAsync (VsixTestCase testCase, IMessageBus messageBus, ExceptionAggregator aggregator)
		{
			if (!EnsureConnected (testCase, messageBus)) {
				return new RunSummary {
					Failed = 1,
				};
			}

			var xunitTest = new XunitTest (testCase, testCase.DisplayName);

			try {
				var remoteBus = remoteBuses.GetOrAdd(messageBus, bus => {
					var instance = new RemoteMessageBus (bus);
					remoteObjects.Add (instance);
					return instance;
				});

				var outputBus = new InterceptingMessageBus (remoteBus, message => {
					var resultMessage = message as ITestResultMessage;
					if (resultMessage != null) {
						Trace.WriteLine (resultMessage.Output);
						Debugger.Log (0, "", resultMessage.Output);
						Console.WriteLine (resultMessage.Output);
					}
				});

				var summary = await Task.Run (
					() => runner.Run (testCase, outputBus))
					.TimeoutAfter (testCase.TimeoutSeconds * 1000);

				if (summary.Exception != null)
					aggregator.Add (summary.Exception);

				return summary.ToRunSummary ();
			} catch (Exception ex) {
				if (ex is RemotingException)
					Stop ();

				aggregator.Add (ex);
				messageBus.QueueMessage (new TestFailed (xunitTest, 0, ex.Message, ex));
				return new RunSummary {
					Failed = 1
				};
			}
		}

		public void Dispose ()
		{
			Stop ();
			ClearBindingPaths ();
		}

		bool EnsureConnected (VsixTestCase testCase, IMessageBus messageBus)
		{
			if (!EnsureStarted (testCase, messageBus))
				return false;

			if (runner == null) {
				var hostUrl = RemotingUtil.GetHostUri (pipeName);
				var clientPipeName = Guid.NewGuid ().ToString ("N");

				clientChannel = RemotingUtil.CreateChannel (Constants.ClientChannelName + clientPipeName, clientPipeName);
				runner = (IVsRemoteRunner)RemotingServices.Connect (typeof (IVsRemoteRunner), hostUrl);
			}

			var retries = 0;
			var connected = false;
			while (retries++ < MaxOperationRetries && !(connected = TryPing (runner))) {
				Stop ();
				if (!EnsureStarted (testCase, messageBus))
					return false;

				if (runner == null) {
					var hostUrl = RemotingUtil.GetHostUri (pipeName);
					var clientPipeName = Guid.NewGuid ().ToString ("N");

					clientChannel = RemotingUtil.CreateChannel (Constants.ClientChannelName + clientPipeName, clientPipeName);
					runner = (IVsRemoteRunner)RemotingServices.Connect (typeof (IVsRemoteRunner), hostUrl);
				}

				Thread.Sleep (RetryInterval);
			}

			if (!connected) {
				Stop ();
				var message = Strings.VsClient.FailedToConnect(testCase.VisualStudioVersion, testCase.RootSuffix);
                messageBus.QueueMessage (new TestFailed (new XunitTest (testCase, testCase.DisplayName), 0, message, new InvalidOperationException (message)));
				return false;
			}

			// If successfully connected, attach trace listeners to remote runner.
			if (Debugger.IsAttached) {
				// Add default trace listeners to the remote process.
				foreach (var listener in Trace.Listeners.OfType<TraceListener> ()) {
					runner.AddListener (listener);
				}
			}

			return true;
		}

		bool EnsureStarted(VsixTestCase testCase, IMessageBus messageBus)
		{
			if (Process == null) {
				var retries = 0;
				var started = false;
				while (retries++ < MaxOperationRetries && !(started = Start ())) {
					Stop ();
					Thread.Sleep (RetryInterval);
				}

				if (!started) {
					Stop ();

					tracer.TraceEvent(TraceEventType.Error, 0, Strings.VsClient.FailedToStart (visualStudioVersion, rootSuffix));
					messageBus.QueueMessage (new TestFailed (new XunitTest (testCase, testCase.DisplayName), 0,
						Strings.VsClient.FailedToStart (visualStudioVersion, rootSuffix),
						new TimeoutException ()));

					return false;
				}
			}

			return true;
		}

		bool TryPing (IVsRemoteRunner runner)
		{
			try {
				runner.Ping ();
				return true;
			} catch (RemotingException) {
				return false;
			}
		}

		bool Start ()
		{
			AddBindingPaths ();

			// This environment variable is used by the VsRemoveRunner to set up the right
			// server channel named pipe, which is later used by the test runner to execute
			// tests in the VS app domain.
			Environment.SetEnvironmentVariable (Constants.PipeNameEnvironmentVariable, pipeName);

			Process = new Process {
				StartInfo = {
					FileName = devEnvPath,
					Arguments = string.IsNullOrEmpty (rootSuffix) ? "" : "/RootSuffix " + rootSuffix,
					UseShellExecute = false,
					WorkingDirectory = Directory.GetCurrentDirectory (),
				},
			};

			Process.Start ();

			// This forces us to wait until VS is fully started.
			var dte = RunningObjects.GetDTE (visualStudioVersion, Process.Id, TimeSpan.FromMinutes (1));
			if (dte == null)
				return false;

			var services = new Microsoft.VisualStudio.Shell.ServiceProvider ((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte);
			IVsShell shell;
			while ((shell = (IVsShell)services.GetService (typeof (SVsShell))) == null) {
				Thread.Sleep (100);
			}

			object zombie;
			while ((int?)(zombie = shell.GetProperty ((int)__VSSPROPID.VSSPROPID_Zombie, out zombie)) != 0) {
				Thread.Sleep (100);
			}

			if (Debugger.IsAttached) {
				// When attached via TD.NET, there will be an environment variable named DTE_MainWindow=2296172
				var mainWindow = Environment.GetEnvironmentVariable ("DTE_MainWindow");
				if (!string.IsNullOrEmpty (mainWindow)) {
					var attached = false;
					var retries = 0;

					while (retries++ < MaxOperationRetries && !attached) {
						try {
							var mainHWnd = int.Parse (mainWindow);
							var mainDte = GetAllDtes ().FirstOrDefault (x => x.MainWindow.HWnd == mainHWnd);
							if (mainDte != null) {
								var startedVs = mainDte.Debugger.LocalProcesses.OfType<EnvDTE.Process> ().FirstOrDefault (x => x.ProcessID == Process.Id);
								if (startedVs != null) {
									startedVs.Attach ();
									attached = true;
									break;
								}
							}
						} catch (Exception ex) {
							tracer.TraceEvent(TraceEventType.Warning, 0, Strings.VsClient.RetryAttach (retries, MaxOperationRetries) + Environment.NewLine + ex.ToString());
						}

						Thread.Sleep (RetryInterval);
					}

					if (!attached)
						tracer.TraceEvent(TraceEventType.Error, 0, Strings.VsClient.FailedToAttach (visualStudioVersion, rootSuffix));
				}
			}

			try {
				Injector.Launch (Process.MainWindowHandle,
					GetType ().Assembly.Location,
					typeof (VsStartup).FullName, "Start");
			} catch (Exception ex) {
				tracer.TraceEvent(TraceEventType.Error, 0, Strings.VsClient.FailedToInject (Process.Id) + Environment.NewLine + ex.ToString());
				return false;
			}

			return true;
		}

		void AddBindingPaths ()
		{
			if (initializedExtension)
				return;

			// Add all currently loaded assemblies paths to the resolve paths.
			var probingPaths = AppDomain.CurrentDomain.GetAssemblies ()
				.Select (x => Path.GetDirectoryName (x.ManifestModule.FullyQualifiedName))
				.Where (x => x.StartsWith (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData)))
				.Concat (new[] { Directory.GetCurrentDirectory() })
				.Distinct ()
				.ToArray ();

			// Loading just the current path into the VS binding paths causes all
			// other deps to fail.
			//var probingPaths = new [] { GetType().Assembly.ManifestModule.FullyQualifiedName };

			var extensionsPath = Path.Combine (
				Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData),
				@"Microsoft\VisualStudio",
				visualStudioVersion + rootSuffix,
				"Extensions");

			var bindingPathKeyName = @"Software\Microsoft\VisualStudio\" +
				visualStudioVersion + rootSuffix + @"_Config\BindingPaths\";

			var pathsKey = Registry.CurrentUser.OpenSubKey (bindingPathKeyName, true);

			try {
				if (pathsKey == null) {
					FirstRun ();
					pathsKey = Registry.CurrentUser.OpenSubKey (bindingPathKeyName, true);
				}

				var bindingKey = pathsKey.OpenSubKey (BindingPathKey, true);
				if (bindingKey == null)
					bindingKey = pathsKey.CreateSubKey (BindingPathKey);

				using (bindingKey) {
					// Across multiple VsClient sessions within the same
					// test run (i.e. tests that request their own clean
					// instance of VS), these paths won't change.
					var bindingPaths = new HashSet<string> (bindingKey.GetValueNames ());

					if (probingPaths.Any (probingPath => !bindingPaths.Contains (probingPath))) {
						// There was a change, meaning it's another run, and typically all
						// assemblies change location because of shadow copying, so we
						// have to refresh all of them.
						foreach (var name in bindingKey.GetValueNames ()) {
							bindingKey.DeleteValue (name);
						}

						foreach (var probingPath in probingPaths) {
							bindingKey.SetValue (probingPath, "");
						}
					}
				}

			} finally {
				if (pathsKey != null)
					pathsKey.Dispose ();
			}

			initializedExtension = true;
		}

		/// <summary>
		/// If the given VS vesion + hive hasn't been run ever before, we need
		/// to do it once to give VS a chance to create the _Config registry
		/// key populated from available extensions.
		/// </summary>
		void FirstRun ()
		{
			var process = new Process {
				StartInfo = {
					FileName = devEnvPath,
					Arguments = string.IsNullOrEmpty (rootSuffix) ? "" : "/RootSuffix " + rootSuffix,
					UseShellExecute = false,
					WorkingDirectory = Directory.GetCurrentDirectory (),
				},
			};

			process.Start ();

			// This forces us to wait until VS is fully started.
			var dte = RunningObjects.GetDTE (visualStudioVersion, process.Id, TimeSpan.FromMinutes(2));
			if (dte != null) {
				dte.ExecuteCommand ("File.Exit");
				var timeout = SpinWait.SpinUntil (() => process.HasExited, TimeSpan.FromSeconds(15));

				if (timeout && !process.HasExited)
					process.Kill ();
			} else {
				process.Kill ();
			}
		}

		void ClearBindingPaths ()
		{
			using (var pathsKey = Registry.CurrentUser.OpenSubKey (@"Software\Microsoft\VisualStudio\" +
				visualStudioVersion + rootSuffix + @"_Config\BindingPaths\", true)) {
				if (pathsKey != null)
					pathsKey.DeleteSubKey (BindingPathKey, false);
			}
		}

		void Stop ()
		{
			try {
				if (runner != null)
					runner.Dispose ();
			} catch { }

			foreach (var mbr in remoteObjects) {
				try {
					var disposable = mbr as IDisposable;
					if (disposable != null)
						disposable.Dispose ();
				} catch { }

				try {
					RemotingServices.Disconnect (mbr);
				} catch { }
			}

			if (clientChannel != null)
				ChannelServices.UnregisterChannel (clientChannel);

			clientChannel = null;
			runner = null;

			if (Process != null) {
				if (!Process.HasExited)
					Process.Kill ();

				Process = null;
			}
		}

		string GetDevEnvPath ()
		{
			var varName = "VS" + visualStudioVersion.Replace (".", "") + "COMNTOOLS";
			var varValue = Environment.GetEnvironmentVariable (varName);
			if (string.IsNullOrEmpty (varValue) || !Directory.Exists (varValue))
				throw new ArgumentException (string.Format ("Visual Studio Version '{0}' path was not found in environment variable '{1}'.", visualStudioVersion, varName));

			// Path is like: C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\Tools\..\IDE\devenv.exe
			// We need to move up one level and down to IDE for the final devenv path.
			var path = Path.Combine (varValue, @"..\IDE\devenv.exe");
			if (!File.Exists (path))
				throw new ArgumentException (string.Format ("Visual Studio Version '{0}' executable was not found at the expected location '{1}' according to the environment variable '{2}'.", visualStudioVersion, path, varName));
			return path;
		}

		IEnumerable<EnvDTE.DTE> GetAllDtes ()
		{
			IRunningObjectTable table;
			IEnumMoniker moniker;
			if (ErrorHandler.Failed (NativeMethods.GetRunningObjectTable (0, out table)))
				yield break;

			table.EnumRunning (out moniker);
			moniker.Reset ();
			var pceltFetched = IntPtr.Zero;
			var rgelt = new IMoniker[1];

			while (moniker.Next (1, rgelt, pceltFetched) == 0) {
				IBindCtx ctx;
				if (!ErrorHandler.Failed (NativeMethods.CreateBindCtx (0, out ctx))) {
					string displayName;
					rgelt[0].GetDisplayName (ctx, null, out displayName);
					if (displayName.Contains ("VisualStudio.DTE")) {
						object comObject;
						table.GetObject (rgelt[0], out comObject);
						yield return (EnvDTE.DTE)comObject;
					}
				}
			}
		}
	}
}
