namespace OY.TotalCommander.TcPluginTools {
    public enum PluginLifetimeStatus {
        NotLoaded,           // plugin was not loaded, error
        Active,              // plugin loaded and not expired, proxy is active
        Expired,             // plugin remote object has expired, plugin AppDomain was not unloaded 
                             // (due to unload error or configuration)
                             // all following calls to the proxy will generate errors 
        PluginUnloaded       // plugin remote object was expired, plugin AppDomain was unloaded, proxy is garbage;
                             // next call to the proxy has to reload managed plugin
    }
}
