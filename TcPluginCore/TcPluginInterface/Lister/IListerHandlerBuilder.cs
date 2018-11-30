using System;

namespace OY.TotalCommander.TcPluginInterface.Lister {
    public interface IListerHandlerBuilder {
        ListerPlugin Plugin { get; set; }
        IntPtr GetHandle(object listerControl, IntPtr parentHandle);
    }
}
