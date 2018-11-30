using System;

namespace OY.TotalCommander.TcPluginInterface {
    // Indicates that the attributed method will be exposed to unmanaged code as a static entry point. 
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class DllExportAttribute: Attribute {
        // Gets or sets the name of the DLL entry point. If not set, attributed method name will be used as entry point name.
        public string EntryPoint { get; set; }
    }
}
