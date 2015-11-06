﻿using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	class VsixTestCollection : TestCollection
	{
		public VsixTestCollection (ITestAssembly testAssembly, ITypeInfo collectionDefinition,
			string visualStudioVersion, string rootSuffix)
			: base (testAssembly, collectionDefinition, visualStudioVersion + " (" + rootSuffix + ")")
		{
			VisualStudioVersion = visualStudioVersion;
			RootSuffix = rootSuffix;

			var settingsAttribute = testAssembly.Assembly.GetCustomAttributes(typeof(VsixRunnerAttribute)).FirstOrDefault();
			if (settingsAttribute == null)
				Settings = new VsixRunnerSettings ();
			else
				Settings = new VsixRunnerSettings (
					settingsAttribute.GetNamedArgument<int?> ("DebuggerAttachRetries"),
					settingsAttribute.GetNamedArgument<int?> ("RemoteConnectionRetries"),
					settingsAttribute.GetNamedArgument<int?> ("ProcessStartRetries"),
					settingsAttribute.GetNamedArgument<int?> ("RetrySleepInterval"));
        }

		public string VisualStudioVersion { get; private set; }

		public string RootSuffix { get; private set; }

		public VsixRunnerSettings Settings { get; private set; }
	}
}
