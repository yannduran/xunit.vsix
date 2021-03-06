﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Xunit.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Xunit.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Initializes global services for DTE version {0}.
        /// </summary>
        internal static string GlobalServices_InitializedDte {
            get {
                return ResourceManager.GetString("GlobalServices_InitializedDte", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to retrieve currently running DTE.
        /// </summary>
        internal static string GlobalServices_NoDte {
            get {
                return ResourceManager.GetString("GlobalServices_NoDte", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to attach debugger to running Visual Studio {0}{1}.
        /// </summary>
        internal static string VsClient_FailedToAttach {
            get {
                return ResourceManager.GetString("VsClient_FailedToAttach", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to connect to remote runner on Visual Studio {0}{1}.
        /// </summary>
        internal static string VsClient_FailedToConnect {
            get {
                return ResourceManager.GetString("VsClient_FailedToConnect", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to inject target process {0}.
        /// </summary>
        internal static string VsClient_FailedToInject {
            get {
                return ResourceManager.GetString("VsClient_FailedToInject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to start Visual Studio {0}{1}.
        /// </summary>
        internal static string VsClient_FailedToStart {
            get {
                return ResourceManager.GetString("VsClient_FailedToStart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Environment variables from Visual Studio process:
        ///{0}.
        /// </summary>
        internal static string VsClient_RemoteEnvVars {
            get {
                return ResourceManager.GetString("VsClient_RemoteEnvVars", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to attach debugger. Attempt {0} of {1}.
        /// </summary>
        internal static string VsClient_RetryAttach {
            get {
                return ResourceManager.GetString("VsClient_RetryAttach", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Environment variables from test runner process:
        ///{0}.
        /// </summary>
        internal static string VsClient_RunnerEnvVars {
            get {
                return ResourceManager.GetString("VsClient_RunnerEnvVars", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Setting configured max threads to 1 for debugging.
        /// </summary>
        internal static string VsixTestFramework_DebugMaxThreads {
            get {
                return ResourceManager.GetString("VsixTestFramework_DebugMaxThreads", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Overriding configured max threads to match the number of detected installed Visual Studio versions: {0}.
        /// </summary>
        internal static string VsixTestFramework_SettingMaxThreads {
            get {
                return ResourceManager.GetString("VsixTestFramework_SettingMaxThreads", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to start remote runner.
        /// </summary>
        internal static string VsStartup_Failed {
            get {
                return ResourceManager.GetString("VsStartup_Failed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Remote runner started successfully.
        /// </summary>
        internal static string VsStartup_Started {
            get {
                return ResourceManager.GetString("VsStartup_Started", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Starting  remote runner.
        /// </summary>
        internal static string VsStartup_Starting {
            get {
                return ResourceManager.GetString("VsStartup_Starting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Determined current Visual Studio version: {0}.
        /// </summary>
        internal static string VsVersions_CurrentVersion {
            get {
                return ResourceManager.GetString("VsVersions_CurrentVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Detected installed Visual Studio versions: {0}.
        /// </summary>
        internal static string VsVersions_InstalledVersions {
            get {
                return ResourceManager.GetString("VsVersions_InstalledVersions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Determined latest Visual Studio version installed: {0}.
        /// </summary>
        internal static string VsVersions_LatestVersion {
            get {
                return ResourceManager.GetString("VsVersions_LatestVersion", resourceCulture);
            }
        }
    }
}
