using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.Packer;
using OY.TotalCommander.TcPluginTools;

namespace OY.TotalCommander.WcxWrapper {
    public class PackerWrapper {

        #region Variables

        private static PackerPlugin plugin;
        private static string pluginWrapperDll = Assembly.GetExecutingAssembly().Location;
        private static string callSignature;

        #endregion Variables

        #region Properties

        private static PackerPlugin Plugin {
            get {
                return plugin ??
                       (plugin = (PackerPlugin)TcPluginLoader.GetTcPlugin(pluginWrapperDll, PluginType.Packer));
            }
        }

        #endregion Properties

        private PackerWrapper() {
        }

        #region Packer Plugin Exported Functions

        #region Mandatory Methods

        #region OpenArchive

        [DllExport(EntryPoint = "OpenArchive")]
        public static IntPtr OpenArchive(IntPtr archiveData) {
            OpenArchiveData data = new OpenArchiveData(archiveData, false);
            return OpenArchiveInternal(data);
        }

        [DllExport(EntryPoint = "OpenArchiveW")]
        public static IntPtr OpenArchiveW(IntPtr archiveData) {
            OpenArchiveData data = new OpenArchiveData(archiveData, true);
            return OpenArchiveInternal(data);
        }

        public static IntPtr OpenArchiveInternal(OpenArchiveData data) {
            IntPtr result = IntPtr.Zero;
            callSignature = String.Format("OpenArchive {0} ({1})", data.ArchiveName, data.Mode.ToString());
            try {
                object o = Plugin.OpenArchive(ref data);
                if (o != null && data.Result == PackerResult.OK) {
                    result = TcHandles.AddHandle(o);
                    data.Update();
                }

                TraceCall(TraceLevel.Info, (result == IntPtr.Zero) ?
                    String.Format("Error ({0})", data.Result.ToString()) : result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
                result = IntPtr.Zero;
            }
            return result;
        }

        #endregion OpenArchive

        #region ReadHeader

        [DllExport(EntryPoint = "ReadHeader")]
        public static int ReadHeader(IntPtr arcData, IntPtr headerData) {
            return ReadHeaderInternal(arcData, headerData, HeaderDataMode.Ansi);
        }

        #endregion ReadHeader

        #region ReadHeaderEx

        [DllExport(EntryPoint = "ReadHeaderEx")]
        public static int ReadHeaderEx(IntPtr arcData, IntPtr headerData) {
            return ReadHeaderInternal(arcData, headerData, HeaderDataMode.ExAnsi);
        }

        [DllExport(EntryPoint = "ReadHeaderExW")]
        public static int ReadHeaderExW(IntPtr arcData, IntPtr headerData) {
            return ReadHeaderInternal(arcData, headerData, HeaderDataMode.ExUnicode);
        }

        public static int ReadHeaderInternal(IntPtr arcData, IntPtr headerData, HeaderDataMode mode) {
            PackerResult result = PackerResult.EndArchive;
            callSignature = String.Format("ReadHeader ({0})", arcData.ToString());
            try {
                object o = TcHandles.GetObject(arcData);
                if (o == null)
                    return (int)PackerResult.ErrorOpen;
                HeaderData header;
                result = Plugin.ReadHeader(ref o, out header);
                if (result == PackerResult.OK) {
                    header.CopyTo(headerData, mode);
                    TcHandles.UpdateHandle(arcData, o);
                }

                // !!! may produce much trace info !!!
                TraceCall(TraceLevel.Verbose, String.Format("{0} ({1})",
                    result.ToString(), (result == PackerResult.OK) ? header.FileName : null));
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion ReadHeaderEx

        #region ProcessFile

        [DllExport(EntryPoint = "ProcessFile")]
        public static int ProcessFile(IntPtr arcData, int operation,
                [MarshalAs(UnmanagedType.LPStr)]string destPath, 
                [MarshalAs(UnmanagedType.LPStr)]string destName) {
            return ProcessFileW(arcData, operation, destPath, destName);
        }

        [DllExport(EntryPoint = "ProcessFileW")]
        public static int ProcessFileW(IntPtr arcData, int operation,
                [MarshalAs(UnmanagedType.LPWStr)]string destPath,
                [MarshalAs(UnmanagedType.LPWStr)]string destName) {
            PackerResult result = PackerResult.NotSupported;
            ProcessFileOperation oper = (ProcessFileOperation)operation;
            string fileName = String.IsNullOrEmpty(destPath) ? destName : Path.Combine(destPath, destName);
            callSignature = String.Format("ProcessFile ({0}, {1}, {2})",
                arcData.ToString(), oper.ToString(), fileName);
            try {
                object o = TcHandles.GetObject(arcData);
                if (o != null) {
                    result = Plugin.ProcessFile(o, oper, fileName);
                    if (result == PackerResult.OK)
                        TcHandles.UpdateHandle(arcData, o);
                }

                // !!! may produce much trace info !!!
                TraceCall(TraceLevel.Verbose, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion ProcessFile

        #region CloseArchive

        [DllExport(EntryPoint = "CloseArchive")]
        public static int CloseArchive(IntPtr arcData) {
            PackerResult result = PackerResult.ErrorClose;
            callSignature = String.Format("FindClose ({0})", arcData.ToString());
            try {
                object o = TcHandles.GetObject(arcData);
                if (o != null) {
                    result = Plugin.CloseArchive(o);
                    IDisposable disp = o as IDisposable;
                    if (disp != null)
                        disp.Dispose();

                    int count = (TcHandles.RemoveHandle(arcData) - 1) / 2;

                    TraceCall(TraceLevel.Info, String.Format("{0} items.", count));
                }
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion CloseArchive

        #region SetChangeVolProc

        // SetChangeVolProc & SetChangeVolProcW functionality is implemented here, not included to Packer Plugin interface.
        [DllExport(EntryPoint = "SetChangeVolProc")]
        public static void SetChangeVolProc(IntPtr arcData, ChangeVolCallback changeVolProc) {
            callSignature = String.Format("SetChangeVolProc ({0})", arcData.ToString());
            try {
                TcCallback.SetPackerPluginCallbacks(changeVolProc, null, null, null, null, null);

                TraceCall(TraceLevel.Warning,
                    changeVolProc.Method.MethodHandle.GetFunctionPointer().ToString("X"));
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        [DllExport(EntryPoint = "SetChangeVolProcW")]
        public static void SetChangeVolProcW(IntPtr arcData, ChangeVolCallbackW changeVolProcW) {
            callSignature = String.Format("SetChangeVolProcW ({0})", arcData.ToString());
            try {
                TcCallback.SetPackerPluginCallbacks(null, changeVolProcW, null, null, null, null);

                TraceCall(TraceLevel.Warning,
                    changeVolProcW.Method.MethodHandle.GetFunctionPointer().ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion SetChangeVolProc

        #region SetProcessDataProc

        // SetProcessDataProc & SetProcessDataProcW functionality is implemented here, not included to Packer Plugin interface.
        [DllExport(EntryPoint = "SetProcessDataProc")]
        public static void SetProcessDataProc(IntPtr arcData, ProcessDataCallback processDataProc) {
            callSignature = String.Format("SetProcessDataProc ({0})", arcData.ToString());
            try {
                TcCallback.SetPackerPluginCallbacks(null, null, processDataProc, null, null, null);

                TraceCall(TraceLevel.Warning,
                    processDataProc.Method.MethodHandle.GetFunctionPointer().ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        [DllExport(EntryPoint = "SetProcessDataProcW")]
        public static void SetProcessDataProcW(IntPtr arcData, ProcessDataCallbackW processDataProcW) {
            callSignature = String.Format("SetProcessDataProcW ({0})", arcData.ToString());
            try {
                TcCallback.SetPackerPluginCallbacks(null, null, null, processDataProcW, null, null);
                TraceCall(TraceLevel.Warning,
                    processDataProcW.Method.MethodHandle.GetFunctionPointer().ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion SetProcessDataProc

        #endregion Mandatory Methods

        #region Optional Methods

        #region PackFiles

        [DllExport(EntryPoint = "PackFiles")]
        public static int PackFiles(
                [MarshalAs(UnmanagedType.LPStr)]string packedFile,
                [MarshalAs(UnmanagedType.LPStr)]string subPath,
                [MarshalAs(UnmanagedType.LPStr)]string srcPath,
                IntPtr addListPtr, int flags) {
            List<string> addList = TcUtils.ReadStringListAnsi(addListPtr);
            return PackFilesInternal(packedFile, subPath, srcPath, addList, (PackFilesFlags)flags);
        }

        [DllExport(EntryPoint = "PackFilesW")]
        public static int PackFilesW(
                [MarshalAs(UnmanagedType.LPWStr)]string packedFile,
                [MarshalAs(UnmanagedType.LPWStr)]string subPath,
                [MarshalAs(UnmanagedType.LPWStr)]string srcPath,
                IntPtr addListPtr, int flags) {
            List<string> addList = TcUtils.ReadStringListUni(addListPtr);
            return PackFilesInternal(packedFile, subPath, srcPath, addList, (PackFilesFlags)flags);
        }

        public static int PackFilesInternal(string packedFile, string subPath, string srcPath,
                List<string> addList, PackFilesFlags flags) {
            PackerResult result = PackerResult.NotSupported;
            callSignature = String.Format("PackFiles ({0}, {1}, {2}, {3}) - {4} files)",
                packedFile, subPath, srcPath, flags.ToString(), addList.Count);
            try {
                result = Plugin.PackFiles(packedFile, subPath, srcPath, addList, flags);

                TraceCall(TraceLevel.Info, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion PackFiles

        #region DeleteFiles

        [DllExport(EntryPoint = "DeleteFiles")]
        public static int DeleteFiles([MarshalAs(UnmanagedType.LPStr)]string packedFile, IntPtr deleteListPtr) {
            List<string> deleteList = TcUtils.ReadStringListAnsi(deleteListPtr);
            return DeleteFilesInternal(packedFile, deleteList);
        }

        [DllExport(EntryPoint = "DeleteFilesW")]
        public static int DeleteFilesW([MarshalAs(UnmanagedType.LPWStr)]string packedFile, IntPtr deleteListPtr) {
            List<string> deleteList = TcUtils.ReadStringListUni(deleteListPtr);
            return DeleteFilesInternal(packedFile, deleteList);
        }

        public static int DeleteFilesInternal(string packedFile, List<string> deleteList) {
            PackerResult result = PackerResult.NotSupported;
            callSignature = String.Format("DeleteFiles ({0}) - {1} files)",
                packedFile, deleteList.Count);
            try {
                result = Plugin.DeleteFiles(packedFile, deleteList);

                TraceCall(TraceLevel.Info, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion DeleteFiles

        #region GetPackerCaps

        // GetPackerCaps functionality is implemented here, not included to Packer Plugin interface.
        [DllExport(EntryPoint = "GetPackerCaps")]
        public static int GetPackerCaps() {
            callSignature = "GetPackerCaps";

            TraceCall(TraceLevel.Info, Plugin.Capabilities.ToString());
            return (int)Plugin.Capabilities;
        }

        #endregion GetPackerCaps

        #region ConfigurePacker

        [DllExport(EntryPoint = "ConfigurePacker")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static void ConfigurePacker(IntPtr parentWin, IntPtr dllInstance) {
            callSignature = "ConfigurePacker";
            try {
                Plugin.ConfigurePacker(new TcWindow(parentWin));

                TraceCall(TraceLevel.Info, null);
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion ConfigurePacker

        #region StartMemPack

        [DllExport(EntryPoint = "StartMemPack")]
        public static IntPtr StartMemPack(int options, [MarshalAs(UnmanagedType.LPStr)]string fileName) {
            return StartMemPackW(options, fileName);
        }

        [DllExport(EntryPoint = "StartMemPackW")]
        public static IntPtr StartMemPackW(int options, [MarshalAs(UnmanagedType.LPWStr)]string fileName) {
            IntPtr result = IntPtr.Zero;
            MemPackOptions mpOptions = (MemPackOptions)options;
            callSignature = String.Format("StartMemPack {0} ({1})", fileName, mpOptions.ToString());
            try {
                object o = Plugin.StartMemPack(mpOptions, fileName);
                if (o != null)
                    result = TcHandles.AddHandle(o);

                TraceCall(TraceLevel.Warning, (result == IntPtr.Zero) ? "ERROR" : result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return result;
        }

        #endregion StartMemPack

        #region PackToMem

        [DllExport(EntryPoint = "PackToMem")]
        public static int PackToMem(IntPtr hMemPack,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] bufIn,
                int inLen, ref int taken,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] byte[] bufOut,
                int outLen, ref int written, int seekBy) {
            PackerResult result = PackerResult.NotSupported;
            callSignature = String.Format("PackToMem ({0} - {1}, {2}, {3})",
                hMemPack.ToString(), inLen, outLen, seekBy);
            string traceRes = null;
            try {
                object o = TcHandles.GetObject(hMemPack);
                if (o != null) {
                    result = Plugin.PackToMem(ref o, bufIn, ref taken, bufOut, ref written, seekBy);
                    traceRes = result.ToString();
                    if (result == PackerResult.OK) {
                        TcHandles.UpdateHandle(hMemPack, o);
                        traceRes += String.Format(" - {0}, {1}", taken, written);
                    }
                }

                TraceCall(TraceLevel.Verbose, traceRes);
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion PackToMem

        #region DoneMemPack

        [DllExport(EntryPoint = "DoneMemPack")]
        public static int DoneMemPack(IntPtr hMemPack) {
            PackerResult result = PackerResult.ErrorClose;
            callSignature = String.Format("DoneMemPack ({0})", hMemPack.ToString());
            try {
                object o = TcHandles.GetObject(hMemPack);
                if (o != null) {
                    result = Plugin.DoneMemPack(o);
                    IDisposable disp = o as IDisposable;
                    if (disp != null)
                        disp.Dispose();

                    int count = TcHandles.RemoveHandle(hMemPack);
                    TraceCall(TraceLevel.Warning, String.Format("{0} calls.", count));
                }
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion DoneMemPack

        #region CanYouHandleThisFile

        [DllExport(EntryPoint = "CanYouHandleThisFile")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool CanYouHandleThisFile([MarshalAs(UnmanagedType.LPStr)]string fileName) {
            return CanYouHandleThisFileW(fileName);
        }

        [DllExport(EntryPoint = "CanYouHandleThisFileW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static bool CanYouHandleThisFileW([MarshalAs(UnmanagedType.LPWStr)]string fileName) {
            bool result = false;
            callSignature = String.Format("CanYouHandleThisFile ({0})", fileName);
            try {
                result = Plugin.CanYouHandleThisFile(fileName);

                TraceCall(TraceLevel.Warning, result ? "Yes" : "No");
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return result;
        }

        #endregion CanYouHandleThisFile

        #region PackSetDefaultParams

        // PackSetDefaultParams functionality is implemented here, not included to Packer Plugin interface.
        [DllExport(EntryPoint = "PackSetDefaultParams")]
        public static void SetDefaultParams(ref PluginDefaultParams defParams) {
            callSignature = "SetDefaultParams";
            try {
                Plugin.DefaultParams = defParams;

                TraceCall(TraceLevel.Info, null);
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion PackSetDefaultParams

        #region PkSetCryptCallback

        // PkSetCryptCallback & PkSetCryptCallbackW functionality is implemented here, not included to Packer Plugin interface.
        [DllExport(EntryPoint = "PkSetCryptCallback")]
        public static void SetCryptCallback(PkCryptCallback cryptProc, int cryptNumber, int flags) {
            callSignature = String.Format("PkSetCryptCallback ({0}, {1})", cryptNumber, flags);
            try {
                TcCallback.SetPackerPluginCallbacks(null, null, null, null, cryptProc, null);
                Plugin.CreatePassword(cryptNumber, flags);

                TraceCall(TraceLevel.Info,
                    cryptProc.Method.MethodHandle.GetFunctionPointer().ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        [DllExport(EntryPoint = "PkSetCryptCallbackW")]
        public static void SetCryptCallbackW(PkCryptCallbackW cryptProcW, int cryptNumber, int flags) {
            callSignature = String.Format("PkSetCryptCallbackW ({0}, {1})", cryptNumber, flags);
            try {
                TcCallback.SetPackerPluginCallbacks(null, null, null, null, null, cryptProcW);
                Plugin.CreatePassword(cryptNumber, flags);

                TraceCall(TraceLevel.Info,
                    cryptProcW.Method.MethodHandle.GetFunctionPointer().ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion PkSetCryptCallback

        #region GetBackgroundFlags

        // GetBackgroundFlags functionality is implemented here, not included to Packer Plugin interface.
        [DllExport(EntryPoint = "GetBackgroundFlags")]
        public static int GetBackgroundFlags() {
            PackBackgroundFlags result = PackBackgroundFlags.None;
            callSignature = "GetBackgroundFlags";
            try {
                result = Plugin.BackgroundFlags;

                TraceCall(TraceLevel.Info, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion FsGetBackgroundFlags

        #endregion Optional Methods

        #endregion Packer Plugin Exported Functions

        #region Tracing & Exceptions

        private static void ProcessException(Exception ex) {
            TcPluginLoader.ProcessException(plugin, false, callSignature, ex);
        }

        private static void TraceCall(TraceLevel level, string result) {
            TcTrace.TraceCall(plugin, level, callSignature, result);
            callSignature = null;
        }

        #endregion Tracing & Exceptions
    }
}
