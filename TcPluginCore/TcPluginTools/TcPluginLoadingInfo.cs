using System;

using OY.TotalCommander.TcPluginInterface;

namespace OY.TotalCommander.TcPluginTools {
    public class TcPluginLoadingInfo {
        public string WrapperFileName { get; private set; }
        public TcPlugin Plugin { get; set; }
        public AppDomain Domain { get; set; }
        public int PluginNumber { get; set; }
        public int CryptoNumber { get; set; }
        public int CryptoFlags { get; set; }
        public Boolean UnloadExpired { get; set; }
        public PluginLifetimeStatus LifetimeStatus { get; set; }

        public TcPluginLoadingInfo(string wrapperFileName, TcPlugin tcPlugin, AppDomain domain) {
            WrapperFileName = wrapperFileName;
            Plugin = tcPlugin;
            Domain = domain;
            PluginNumber = -1;
            CryptoNumber = -1;
            CryptoFlags = 0;
            UnloadExpired = true;
            LifetimeStatus = PluginLifetimeStatus.Active;
        }
    }
}
