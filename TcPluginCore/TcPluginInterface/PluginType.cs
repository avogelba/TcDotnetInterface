using System;

namespace OY.TotalCommander.TcPluginInterface {
    [Serializable]
    public enum PluginType {
        Content,
        FileSystem,
        Lister,
        Packer,
        QuickSearch,
        Unknown
    }
}