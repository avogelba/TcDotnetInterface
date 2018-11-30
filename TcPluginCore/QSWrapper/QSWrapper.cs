using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.QuickSearch;
using OY.TotalCommander.TcPluginTools;

namespace OY.TotalCommander.QSWrapper {
    public class QuickSearchWrapper {

        #region Variables

        private static QuickSearchPlugin plugin;
        private static string pluginWrapperDll = Assembly.GetExecutingAssembly().Location;
        private static string callSignature;

        #endregion Variables

        #region Properties

        private static QuickSearchPlugin Plugin {
            get {
                return plugin ??
                       (plugin = (QuickSearchPlugin)TcPluginLoader.GetTcPlugin(pluginWrapperDll, PluginType.QuickSearch));
            }
        }

        #endregion Properties

        private QuickSearchWrapper() {
        }

        #region QuickSearch Exported Functions

        #region Mandatory Methods

        [DllExport(EntryPoint = "MatchFileW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool MatchFile(IntPtr wcFilter, IntPtr wcFileName) {
            string filter = Marshal.PtrToStringUni(wcFilter);
            string fileName = Marshal.PtrToStringUni(wcFileName);

            bool result = false;
            callSignature = String.Format("MatchFileW(\"{0}\",\"{1}\")", fileName, filter);
            try {
                result = Plugin.MatchFile(filter, fileName);

                // !!! may produce much trace info !!!
                TraceCall(TraceLevel.Verbose, result ? "Yes" : "No");
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return result;
        }

        [DllExport]
        public static int MatchGetSetOptions(int status) {
            MatchOptions result;
            callSignature = String.Format("MatchGetSetOptions(\"{0}\")", status);
            try {
                result = Plugin.MatchGetSetOptions((ExactNameMatch)status);

                TraceCall(TraceLevel.Info, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
                result = MatchOptions.None;
            }
            return (int)result;
        }

        #endregion Mandatory Methods

        #endregion QuickSearch Exported Functions

        #region Tracing & Exceptions

        public static void ProcessException(Exception ex) {
            TcPluginLoader.ProcessException(plugin, false, callSignature, ex);
        }

        public static void TraceCall(TraceLevel level, string result) {
            TcTrace.TraceCall(plugin, level, callSignature, result);
            callSignature = null;
        }

        #endregion Tracing & Exceptions
    }
}
