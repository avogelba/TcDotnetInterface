using System;
#if TRACE
using System.Diagnostics;
#endif
using System.Runtime.InteropServices;

using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.Content;
using OY.TotalCommander.TcPluginInterface.FileSystem;
using OY.TotalCommander.TcPluginInterface.Packer;

namespace OY.TotalCommander.TcPluginTools {
    public static class TcCallback {
        #region Constants

        public const string PluginCallbackDataName = "PluginCallbackData";
        public const int CryptPasswordMaxLen = NativeMethods.MAX_PATH_UNI;

        // Error Messages
        private const string ErrorMsg1 = "Callback: Plugin not found.";
        private const string ErrorMsg2 = "Callback: Domain error.";
        private const string ErrorMsg3 = "Callback: Wrong argument.";

        // Trace Messages
#if TRACE
        private const string TraceMsg1 = "Callback";
        private const string TraceMsg2 = "OnProgress ({0}, {1}): {2} => {3} - {4}.";
        private const string TraceMsg3 = "OnLog ({0}, {1}): {2}.";
        private const string TraceMsg4 = "DomainInfo";
        private const string TraceMsg5 = "OnRequest ({0}, {1}): {2}";
        private const string TraceMsg6 = "OnCrypt ({0}, {1}, {2}): {3}";
        private const string TraceMsg7 = "{0} - {1}.";
        private const string TraceMsg8 = "OnCompareProgress ({0}) - {1}.";
#endif

        #endregion Constants

        #region Variables

#if TRACE
        private static bool writeTrace;

        // to trace Progress callback
        const int ProgressTraceChunk = 25;
        private static int prevPercDone = -ProgressTraceChunk - 1;
#endif

        #endregion Variables


        #region Main Handler

        // This handler is called in main AppDomain
        public static void TcPluginCallbackHandler() {
            AppDomain domain = AppDomain.CurrentDomain;
            string pluginId;
            try {
                pluginId = (string)domain.GetData(PluginCallbackDataName);
            } finally {
                domain.SetData(PluginCallbackDataName, null);
            }
            TcPlugin tp = TcPluginLoader.GetTcPluginById(pluginId);
            if (tp == null)
                throw new InvalidOperationException(ErrorMsg1);
            if (!domain.Equals(tp.MainDomain))
                throw new InvalidOperationException(ErrorMsg2);
#if TRACE
            writeTrace = tp.WriteTrace;
#endif
            string callbackDataBufferName = tp.DataBufferName;
            try {
                object o = domain.GetData(callbackDataBufferName);
                if (o == null || !(o is PluginEventArgs))
                    throw new ArgumentException(ErrorMsg3);
                HandleTcPluginEvent(tp, o as PluginEventArgs);
            } finally {
#if TRACE
                writeTrace = false;
#endif
            }
        }

        public static void HandleTcPluginEvent(object sender, PluginEventArgs e) {
            if (e is CryptEventArgs)
                CryptCallback(e as CryptEventArgs);
            else if (e is ProgressEventArgs)
                FsProgressCallback(e as ProgressEventArgs);
            else if (e is LogEventArgs)
                FsLogCallback(e as LogEventArgs);
            else if (e is RequestEventArgs)
                FsRequestCallback(e as RequestEventArgs);
            else if (e is ContentProgressEventArgs)
                ContentProgressCallback(e as ContentProgressEventArgs);
            else if (e is PackerProcessEventArgs)
                PackerProcessCallback(e as PackerProcessEventArgs);
            else if (e is PackerChangeVolEventArgs)
                PackerChangeVolCallback(e as PackerChangeVolEventArgs);
        }

        #endregion Main Handler

        #region FS Callbacks

        private static ProgressCallback progressCallback;
        private static ProgressCallbackW progressCallbackW;
        private static LogCallback logCallback;
        private static LogCallbackW logCallbackW;
        private static RequestCallback requestCallback;
        private static RequestCallbackW requestCallbackW;
        private static FsCryptCallback fsCryptCallback;
        private static FsCryptCallbackW fsCryptCallbackW;

        public static void SetFsPluginCallbacks(
            ProgressCallback progress, ProgressCallbackW progressW,
            LogCallback log, LogCallbackW logW,
            RequestCallback request, RequestCallbackW requestW,
            FsCryptCallback crypt, FsCryptCallbackW cryptW) {
            if (progressCallback == null)
                progressCallback = progress;
            if (progressCallbackW == null)
                progressCallbackW = progressW;
            if (logCallback == null)
                logCallback = log;
            if (logCallbackW == null)
                logCallbackW = logW;
            if (requestCallback == null)
                requestCallback = request;
            if (requestCallbackW == null)
                requestCallbackW = requestW;
            if (fsCryptCallback == null)
                fsCryptCallback = crypt;
            if (fsCryptCallbackW == null)
                fsCryptCallbackW = cryptW;
        }

        public static void FsProgressCallback(ProgressEventArgs e) {
            if (progressCallbackW != null || progressCallback != null) {
                int pluginNumber = e.PluginNumber;
                string sourceName = e.SourceName;
                string targetName = e.TargetName;
                int percentDone = e.PercentDone;

                if (progressCallbackW != null)
                    e.Result = progressCallbackW(pluginNumber, sourceName, targetName, percentDone);
                else if (progressCallback != null)
                    e.Result = progressCallback(pluginNumber, sourceName, targetName, percentDone);

#if TRACE
                if (percentDone - prevPercDone >= ProgressTraceChunk || percentDone == 100) {
                    TraceOut(TraceLevel.Verbose, String.Format(TraceMsg2, pluginNumber, percentDone, sourceName, targetName, e.Result), TraceMsg1);
                    if (percentDone == 100)
                        prevPercDone = -ProgressTraceChunk - 1;
                    else
                        prevPercDone = percentDone;
                }
#endif
            }
        }

        public static void FsLogCallback(LogEventArgs e) {
            if (logCallbackW != null || logCallback != null) {
                if (logCallbackW != null)
                    logCallbackW(e.PluginNumber, e.MessageType, e.LogText);
                else
                    logCallback(e.PluginNumber, e.MessageType, e.LogText);
#if TRACE
                TraceOut(TraceLevel.Info, String.Format(TraceMsg3, e.PluginNumber, ((LogMsgType)e.MessageType).ToString(), e.LogText), TraceMsg1);
#endif
            }
        }

        public static void FsRequestCallback(RequestEventArgs e) {
            if (e.RequestType == (int)RequestType.DomainInfo) {
                e.ReturnedText = TcPluginLoader.DomainInfo;
                e.Result = 1;
#if TRACE
                TraceOut(TraceLevel.Info, e.ReturnedText, TraceMsg4);
#endif
            } else if (requestCallbackW != null || requestCallback != null) {
                IntPtr retText = IntPtr.Zero;
                if (e.RequestType < (int)RequestType.MsgOk) {
                    if (requestCallbackW != null) {
                        retText = Marshal.AllocHGlobal(e.MaxLen * 2);
                        Marshal.Copy(new char[e.MaxLen], 0, retText, e.MaxLen);
                    } else {
                        retText = Marshal.AllocHGlobal(e.MaxLen);
                        Marshal.Copy(new byte[e.MaxLen], 0, retText, e.MaxLen);
                    }
                }
                try {
                    if (retText != IntPtr.Zero && !String.IsNullOrEmpty(e.ReturnedText)) {
                        if (requestCallbackW != null)
                            Marshal.Copy(e.ReturnedText.ToCharArray(), 0, retText, e.ReturnedText.Length);
                        else
                            TcUtils.WriteStringAnsi(e.ReturnedText, retText, 0);
                    }
                    if (requestCallbackW != null)
                        e.Result = requestCallbackW(e.PluginNumber, e.RequestType, e.CustomTitle, e.CustomText, retText, e.MaxLen) ? 1 : 0;
                    else
                        e.Result = requestCallback(e.PluginNumber, e.RequestType, e.CustomTitle, e.CustomText, retText, e.MaxLen) ? 1 : 0;
#if TRACE
                    string traceStr = String.Format(TraceMsg5, e.PluginNumber, ((RequestType)e.RequestType).ToString(), e.ReturnedText);
#endif
                    if (e.Result != 0 && retText != IntPtr.Zero) {
                        e.ReturnedText = (requestCallbackW != null) ?
                            Marshal.PtrToStringUni(retText) : Marshal.PtrToStringAnsi(retText);
#if TRACE
                        traceStr += " => " + e.ReturnedText;
#endif
                    }
#if TRACE
                    TraceOut(TraceLevel.Verbose, String.Format(TraceMsg7, traceStr, e.Result), TraceMsg1);
#endif
                } finally {
                    if (retText != IntPtr.Zero)
                        Marshal.FreeHGlobal(retText);
                }
            }
        }

        #endregion FS Callbacks

        #region Content Callbacks

        private static ContentProgressCallback contentProgressCallback;

        public static void SetContentPluginCallback(ContentProgressCallback contentProgress) {
            contentProgressCallback = contentProgress;
        }

        public static void ContentProgressCallback(ContentProgressEventArgs e) {
            if (contentProgressCallback != null) {
                e.Result = contentProgressCallback(e.NextBlockData);
#if TRACE
                TraceOut(TraceLevel.Verbose, String.Format(TraceMsg8, e.NextBlockData, e.Result), TraceMsg1);
#endif
            }
        }

        #endregion Content Callbacks

        #region Packer Callbacks

        private static ChangeVolCallback changeVolCallback;
        private static ChangeVolCallbackW changeVolCallbackW;
        private static ProcessDataCallback processDataCallback;
        private static ProcessDataCallbackW processDataCallbackW;
        private static PkCryptCallback pkCryptCallback;
        private static PkCryptCallbackW pkCryptCallbackW;

        public static void SetPackerPluginCallbacks(
            ChangeVolCallback changeVol, ChangeVolCallbackW changeVolW,
            ProcessDataCallback processData, ProcessDataCallbackW processDataW,
            PkCryptCallback crypt, PkCryptCallbackW cryptW) {
            if (changeVolCallback == null)
                changeVolCallback = changeVol;
            if (changeVolCallbackW == null)
                changeVolCallbackW = changeVolW;
            if (processDataCallback == null)
                processDataCallback = processData;
            if (processDataCallbackW == null)
                processDataCallbackW = processDataW;
            if (pkCryptCallback == null)
                pkCryptCallback = crypt;
            if (pkCryptCallbackW == null)
                pkCryptCallbackW = cryptW;
        }

        public static void PackerProcessCallback(PackerProcessEventArgs e) {
            if (processDataCallbackW != null || processDataCallback != null) {
                string fileName = e.FileName;
                int size = e.Size;

                if (processDataCallbackW != null)
                    e.Result = processDataCallbackW(fileName, size);
                else if (processDataCallback != null)
                    e.Result = processDataCallback(fileName, size);
#if TRACE
                TraceOut(TraceLevel.Verbose, 
                    String.Format("OnProcessData ({0}, {1}) - {2}.", fileName, size, e.Result), TraceMsg1);
#endif
            }
        }

        public static void PackerChangeVolCallback(PackerChangeVolEventArgs e) {
            if (changeVolCallbackW != null || changeVolCallback != null) {
                string arcName = e.ArcName;
                int mode = e.Mode;

                if (changeVolCallbackW != null)
                    e.Result = changeVolCallbackW(arcName, mode);
                else if (changeVolCallback != null)
                    e.Result = changeVolCallback(arcName, mode);
#if TRACE
                TraceOut(TraceLevel.Verbose, 
                    String.Format("OnChangeVol ({0}, {1}) - {2}.", arcName, mode, e.Result), TraceMsg1);
#endif
            }
        }

        public static void CryptCallback(CryptEventArgs e) {
            bool isUnicode;
            bool loadPassword = e.Mode == 2 || e.Mode == 3;  // LoadPassword or LoadPasswordNoUI
            if (e.PluginNumber < 0) {
                // Packer plugin call
                if (pkCryptCallbackW == null && pkCryptCallback == null)
                    return;
                isUnicode = (pkCryptCallbackW != null);
            } else {
                // File System plugin call
                if (fsCryptCallbackW == null && fsCryptCallback == null)
                    return;
                isUnicode = (fsCryptCallbackW != null);
            }
            IntPtr pswText = IntPtr.Zero;
            try {
                if (isUnicode) {
                    if (loadPassword)
                        pswText = Marshal.AllocHGlobal(CryptPasswordMaxLen * 2);
                    else if (!String.IsNullOrEmpty(e.Password))
                        pswText = Marshal.StringToHGlobalUni(e.Password);
                    e.Result = (e.PluginNumber < 0) ?
                        pkCryptCallbackW(e.CryptoNumber, e.Mode, e.StoreName, pswText, CryptPasswordMaxLen) :
                        fsCryptCallbackW(e.PluginNumber, e.CryptoNumber, e.Mode, e.StoreName, pswText, CryptPasswordMaxLen);
                } else {
                    if (loadPassword)
                        pswText = Marshal.AllocHGlobal(CryptPasswordMaxLen);
                    else if (!String.IsNullOrEmpty(e.Password))
                        pswText = Marshal.StringToHGlobalAnsi(e.Password);
                    e.Result = (e.PluginNumber < 0) ?
                        pkCryptCallback(e.CryptoNumber, e.Mode, e.StoreName, pswText, CryptPasswordMaxLen) :
                        fsCryptCallback(e.PluginNumber, e.CryptoNumber, e.Mode, e.StoreName, pswText, CryptPasswordMaxLen);
                }

                // tracing                    
#if TRACE
                string traceStr = String.Format(TraceMsg6, e.PluginNumber, e.CryptoNumber, e.Mode, e.StoreName);
#endif

                if (loadPassword && e.Result == 0) {
                    e.Password = isUnicode ?
                        Marshal.PtrToStringUni(pswText) : Marshal.PtrToStringAnsi(pswText);
#if TRACE
                    traceStr += " => (PASSWORD)"; //+ e.Password;
#endif
                } else
                    e.Password = String.Empty;

                // tracing                    
#if TRACE
                TraceOut(TraceLevel.Info, String.Format(TraceMsg7, traceStr, 
                    ((CryptResult)e.Result).ToString()), TraceMsg1);
#endif
            } finally {
                if (pswText != IntPtr.Zero)
                    Marshal.FreeHGlobal(pswText);
            }
        }

        #endregion Packer Callbacks

        #region Lister Callbacks

        //

        #endregion Lister Callbacks

        #region Tracing

#if TRACE
        private static void TraceOut(TraceLevel level, string text, string category) {
            if (writeTrace)
                TcTrace.TraceOut(level, text, category);
        }
#endif

        #endregion Tracing
    }
}
