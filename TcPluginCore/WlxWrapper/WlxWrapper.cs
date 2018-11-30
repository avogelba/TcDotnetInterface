using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.Lister;
using OY.TotalCommander.TcPluginTools;

namespace OY.TotalCommander.WlxWrapper {
    public class ListerWrapper {

        #region Variables

        private static ListerPlugin plugin;
        private static string pluginWrapperDll = Assembly.GetExecutingAssembly().Location;
        private static string callSignature;

        #endregion Variables

        #region Properties

        private static ListerPlugin Plugin {
            get {
                return plugin ??
                       (plugin = (ListerPlugin)TcPluginLoader.GetTcPlugin(pluginWrapperDll, PluginType.Lister));
            }
        }

        private static IListerHandlerBuilder ListerHandlerBuilder {
            get {
                return TcPluginLoader.GetListerHandlerBuilder(pluginWrapperDll);
            }
        }

        #endregion Properties

        private ListerWrapper() {
        }

        #region Lister Plugin Exported Functions

        #region Mandatory Methods

        #region ListLoad

        [DllExport(EntryPoint = "ListLoad")]
        public static IntPtr Load(IntPtr parentWin,
                [MarshalAs(UnmanagedType.LPStr)]string fileToLoad, int flags) {
            return LoadW(parentWin, fileToLoad, flags);
        }

        [DllExport(EntryPoint = "ListLoadW")]
        public static IntPtr LoadW(IntPtr parentWin,
                [MarshalAs(UnmanagedType.LPWStr)]string fileToLoad, int flags) {
            IntPtr listerHandle = IntPtr.Zero;
            ShowFlags showFlags = (ShowFlags)flags;
            callSignature = String.Format("Load ({0}, {1})", fileToLoad, showFlags.ToString());
            try {
                object listerControl = Plugin.Load(fileToLoad, showFlags);
                listerHandle = ListerHandlerBuilder.GetHandle(listerControl, parentWin);
                if (listerHandle != IntPtr.Zero) {
                    Plugin.ListerHandle = listerHandle;
                    Plugin.ParentHandle = parentWin;
                    long windowState = NativeMethods.GetWindowLong(parentWin, NativeMethods.GWL_STYLE);
                    Plugin.IsQuickView = ((windowState & NativeMethods.WS_CHILD) != 0);
                    TcHandles.AddHandle(listerHandle, listerControl);
                    NativeMethods.SetParent(listerHandle, parentWin);
                }
                TraceCall(TraceLevel.Warning, listerHandle.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return listerHandle;
        }

        #endregion ListLoad

        #endregion Mandatory Methods

        #region Optional Methods

        #region ListLoadNext

        [DllExport(EntryPoint = "ListLoadNext")]
        public static int LoadNext(IntPtr parentWin, IntPtr listWin,
                [MarshalAs(UnmanagedType.LPStr)]string fileToLoad, int flags) {
            return LoadNextW(parentWin, listWin, fileToLoad, flags);
        }

        [DllExport(EntryPoint = "ListLoadNextW")]
        public static int LoadNextW(IntPtr parentWin, IntPtr listWin,
                [MarshalAs(UnmanagedType.LPWStr)]string fileToLoad, int flags) {
            ListerResult result = ListerResult.Error;
            ShowFlags showFlags = (ShowFlags)flags;
            callSignature = String.Format("LoadNext ({0}, {1}, {2})",
                listWin.ToString(), fileToLoad, showFlags.ToString());
            try {
                object listerControl = TcHandles.GetObject(listWin);
                result = Plugin.LoadNext(listerControl, fileToLoad, showFlags);
                TcHandles.UpdateHandle(listWin, listerControl);
                TraceCall(TraceLevel.Warning, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion ListLoadNext

        #region ListCloseWindow

        [DllExport(EntryPoint = "ListCloseWindow")]
        public static void CloseWindow(IntPtr listWin) {
            callSignature = String.Format("CloseWindow ({0})", listWin.ToString());
            try {
                object listerControl = TcHandles.GetObject(listWin);
                Plugin.CloseWindow(listerControl);
                int count = TcHandles.RemoveHandle(listWin);
                NativeMethods.DestroyWindow(listWin);
                TraceCall(TraceLevel.Warning, String.Format("{0} calls.", count));
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion ListCloseWindow

        #region ListGetDetectString

        // ListGetDetectString functionality is implemented here, not included to Lister Plugin interface.
        [DllExport(EntryPoint = "ListGetDetectString")]
        public static void GetDetectString(IntPtr detectString, int maxLen) {
            callSignature = "GetDetectString";
            try {
                TcUtils.WriteStringAnsi(Plugin.DetectString, detectString, maxLen);
                TraceCall(TraceLevel.Warning, Plugin.DetectString);
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion ListGetDetectString

        #region ListSearchText

        [DllExport(EntryPoint = "ListSearchText")]
        public static int SearchText(IntPtr listWin,
                [MarshalAs(UnmanagedType.LPStr)]string searchString, int searchParameter) {
            return SearchTextW(listWin, searchString, searchParameter);
        }

        [DllExport(EntryPoint = "ListSearchTextW")]
        public static int SearchTextW(IntPtr listWin,
                [MarshalAs(UnmanagedType.LPWStr)]string searchString, int searchParameter) {
            ListerResult result = ListerResult.Error;
            SearchParameter sp = (SearchParameter)searchParameter;
            callSignature = String.Format("SearchText ({0}, {1}, {2})",
                listWin.ToString(), searchString, sp.ToString());
            try {
                object listerControl = TcHandles.GetObject(listWin);
                result = Plugin.SearchText(listerControl, searchString, sp);
                TcHandles.UpdateHandle(listWin, listerControl);
                TraceCall(TraceLevel.Warning, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion ListSearchText

        #region ListSendCommand

        [DllExport(EntryPoint = "ListSendCommand")]
        public static int SendCommand(IntPtr listWin, int command, int parameter) {
            ListerResult result = ListerResult.Error;
            ListerCommand cmd = (ListerCommand)command;
            ShowFlags par = (ShowFlags)parameter;
            callSignature = String.Format("SendCommand ({0}, {1}, {2})",
                listWin.ToString(), cmd.ToString(), par.ToString());
            try {
                object listerControl = TcHandles.GetObject(listWin);
                result = Plugin.SendCommand(listerControl, cmd, par);
                TcHandles.UpdateHandle(listWin, listerControl);
                TraceCall(TraceLevel.Info, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion ListSendCommand

        #region ListPrint

        [DllExport(EntryPoint = "ListPrint")]
        public static int Print(IntPtr listWin, [MarshalAs(UnmanagedType.LPStr)]string fileToPrint,
                [MarshalAs(UnmanagedType.LPStr)]string defPrinter, int flags, PrintMargins margins) {
            return PrintW(listWin, fileToPrint, defPrinter, flags, margins);
        }

        [DllExport(EntryPoint = "ListPrintW")]
        public static int PrintW(IntPtr listWin, [MarshalAs(UnmanagedType.LPWStr)]string fileToPrint,
                [MarshalAs(UnmanagedType.LPWStr)]string defPrinter, int flags, PrintMargins margins) {
            ListerResult result = ListerResult.Error;
            PrintFlags printFlags = (PrintFlags)flags;
            callSignature = String.Format("Print ({0}, {1}, {2}, {3})",
                listWin.ToString(), fileToPrint, defPrinter, printFlags.ToString());
            try {
                object listerControl = TcHandles.GetObject(listWin);
                result = Plugin.Print(listerControl, fileToPrint, defPrinter, printFlags, margins);
                TcHandles.UpdateHandle(listWin, listerControl);
                TraceCall(TraceLevel.Warning, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion ListPrint

        #region ListNotificationReceived

        [DllExport(EntryPoint = "ListNotificationReceived")]
        public static int NotificationReceived(IntPtr listWin, int message, int wParam, int lParam)   // 32, 64 ???
        {
            int result = 0;
            callSignature = String.Format("NotificationReceived ({0}, {1}, {2}, {3})",
                listWin.ToString(), message, wParam, lParam);
            try {
                object listerControl = TcHandles.GetObject(listWin);
                result = Plugin.NotificationReceived(listerControl, message, wParam, lParam);
                TcHandles.UpdateHandle(listWin, listerControl);
                TraceCall(TraceLevel.Info, result.ToString(CultureInfo.InvariantCulture));
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return result;
        }

        #endregion ListNotificationReceived

        #region ListSetDefaultParams

        // ListSetDefaultParams functionality is implemented here, not included to Lister Plugin interface.
        [DllExport(EntryPoint = "ListSetDefaultParams")]
        public static void SetDefaultParams(ref PluginDefaultParams defParams) {
            callSignature = "SetDefaultParams";
            try {
                Plugin.DefaultParams = defParams;
                TraceCall(TraceLevel.Info, null);
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion ListSetDefaultParams

        #region ListGetPreviewBitmap

        [DllExport(EntryPoint = "ListGetPreviewBitmap")]
        public static IntPtr GetPreviewBitmap([MarshalAs(UnmanagedType.LPStr)]string fileToLoad, int width, int height,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] contentBuf, int contentBufLen) {
            return GetPreviewBitmapInternal(fileToLoad, width, height, contentBuf);
        }

        [DllExport(EntryPoint = "ListGetPreviewBitmapW")]
        public static IntPtr GetPreviewBitmapW([MarshalAs(UnmanagedType.LPWStr)]string fileToLoad, int width, int height,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] contentBuf, int contentBufLen) {
            return GetPreviewBitmapInternal(fileToLoad, width, height, contentBuf);
        }

        public static IntPtr GetPreviewBitmapInternal(string fileToLoad, int width, int height, byte[] contentBuf) {
            IntPtr result;
            callSignature = String.Format("GetPreviewBitmap '{0}' ({1} x {2})",
                fileToLoad, width, height);
            try {
                Bitmap bitmap = Plugin.GetPreviewBitmap(fileToLoad, width, height, contentBuf);
                result = bitmap.GetHbitmap(Plugin.BitmapBackgroundColor);
                TraceCall(TraceLevel.Info, result.Equals(IntPtr.Zero) ? "OK" : "None");
            } catch (Exception ex) {
#if TRACE
                TcTrace.TraceOut(TraceLevel.Error,
                    String.Format("{0}: {1}", callSignature, ex.Message),
                    String.Format("ERROR ({0})", Plugin.TraceTitle));
#endif
                result = IntPtr.Zero;
            }
            return result;
        }

        #endregion ListGetPreviewBitmap

        #region ListSearchDialog

        [DllExport(EntryPoint = "ListSearchDialog")]
        public static int SearchDialog(IntPtr listWin, int findNext) {
            ListerResult result = ListerResult.Error;
            callSignature = String.Format("SearchDialog ({0}, {1})",
                listWin.ToString(), findNext);
            try {
                object listerControl = TcHandles.GetObject(listWin);
                result = Plugin.SearchDialog(listerControl, (findNext != 0));
                TraceCall(TraceLevel.Info, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion ListSearchDialog

        #endregion Optional Methods

        #endregion Lister Plugin Exported Functions

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
