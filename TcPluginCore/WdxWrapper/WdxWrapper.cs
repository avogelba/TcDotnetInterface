using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.Content;
using OY.TotalCommander.TcPluginTools;

namespace OY.TotalCommander.WdxWrapper {
    public class ContentWrapper {

        #region Variables

        private static ContentPlugin plugin;
        private static string pluginWrapperDll = Assembly.GetExecutingAssembly().Location;
        private static string callSignature;

        #endregion Variables

        #region Properties

        private static ContentPlugin Plugin {
            get {
                return plugin ??
                       (plugin = (ContentPlugin)TcPluginLoader.GetTcPlugin(pluginWrapperDll, PluginType.Content));
            }
        }

        #endregion Properties

        private ContentWrapper() {
        }

        #region Content Plugin Exported Functions

        #region ContentGetSupportedField

        [DllExport(EntryPoint = "ContentGetSupportedField")]
        public static int GetSupportedField(int fieldIndex, IntPtr fieldName, IntPtr units, int maxLen) {
            ContentFieldType result = ContentFieldType.NoMoreFields;
            callSignature = String.Format("ContentGetSupportedField ({0})", fieldIndex);
            try {
                string fieldNameStr, unitsStr;
                result = Plugin.GetSupportedField(fieldIndex, out fieldNameStr, out unitsStr, maxLen);
                if (result != ContentFieldType.NoMoreFields) {
                    if (String.IsNullOrEmpty(fieldNameStr))
                        result = ContentFieldType.NoMoreFields;
                    else {
                        TcUtils.WriteStringAnsi(fieldNameStr, fieldName, maxLen);
                        if (String.IsNullOrEmpty(unitsStr))
                            units = IntPtr.Zero;
                        else
                            TcUtils.WriteStringAnsi(unitsStr, units, maxLen);
                    }
                }

                // !!! may produce much trace info !!!
                TraceCall(TraceLevel.Verbose, String.Format("{0} - {1} - {2}",
                    result.ToString(), fieldNameStr, unitsStr));
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion ContentGetSupportedField

        #region ContentGetValue

        [DllExport(EntryPoint = "ContentGetValue")]
        public static int GetValue([MarshalAs(UnmanagedType.LPStr)]string fileName,
                int fieldIndex, int unitIndex, IntPtr fieldValue, int maxLen, int flags) {
            return GetValueW(fileName, fieldIndex, unitIndex, fieldValue, maxLen, flags);
        }

        [DllExport(EntryPoint = "ContentGetValueW")]
        public static int GetValueW([MarshalAs(UnmanagedType.LPWStr)]string fileName,
                int fieldIndex, int unitIndex, IntPtr fieldValue, int maxLen, int flags) {
            GetValueResult result;
            ContentFieldType fieldType = ContentFieldType.NoMoreFields;
            GetValueFlags gvFlags = (GetValueFlags)flags;
            callSignature = String.Format("ContentGetValue '{0}' ({1}/{2}/{3})",
                fileName, fieldIndex, unitIndex, gvFlags.ToString());
            try {
                string fieldValueStr;
                // TODO: add - comments where .NET plugin interface differs from TC
                result = Plugin.GetValue(fileName, fieldIndex, unitIndex,
                    maxLen, gvFlags, out fieldValueStr, out fieldType);
                if (result == GetValueResult.Success 
                        || result == GetValueResult.Delayed 
                        || result == GetValueResult.OnDemand) {
                    ContentFieldType resultType = 
                        result == GetValueResult.Success ? fieldType : ContentFieldType.WideString;
                    (new ContentValue(fieldValueStr, resultType)).CopyTo(fieldValue);
                }

                // !!! may produce much trace info !!!
                TraceCall(TraceLevel.Verbose, String.Format("{0} - {1}", result.ToString(), fieldValueStr));
            } catch (Exception ex) {
                ProcessException(ex);
                result = GetValueResult.NoSuchField;
            }
            return result == GetValueResult.Success ? (int)fieldType : (int)result;
        }

        #endregion ContentGetValue

        #region ContentStopGetValue

        [DllExport(EntryPoint = "ContentStopGetValue")]
        public static void StopGetValue([MarshalAs(UnmanagedType.LPStr)]string fileName) {
            StopGetValueW(fileName);
        }

        [DllExport(EntryPoint = "ContentStopGetValueW")]
        public static void StopGetValueW([MarshalAs(UnmanagedType.LPWStr)]string fileName) {
            callSignature = "ContentStopGetValue";
            try {
                Plugin.StopGetValue(fileName);

                TraceCall(TraceLevel.Info, null);
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion ContentStopGetValue

        #region ContentGetDefaultSortOrder

        [DllExport(EntryPoint = "ContentGetDefaultSortOrder")]
        public static int GetDefaultSortOrder(int fieldIndex) {
            DefaultSortOrder result = DefaultSortOrder.Asc;
            callSignature = String.Format("ContentGetDefaultSortOrder ({0})", fieldIndex);
            try {
                result = Plugin.GetDefaultSortOrder(fieldIndex);

                TraceCall(TraceLevel.Info, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion ContentGetDefaultSortOrder

        #region ContentPluginUnloading

        [DllExport(EntryPoint = "ContentPluginUnloading")]
        public static void PluginUnloading() {
            callSignature = "ContentPluginUnloading";
            try {
                Plugin.PluginUnloading();

                TraceCall(TraceLevel.Info, null);
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion ContentPluginUnloading

        #region ContentGetSupportedFieldFlags

        [DllExport(EntryPoint = "ContentGetSupportedFieldFlags")]
        public static int GetSupportedFieldFlags(int fieldIndex) {
            SupportedFieldOptions result = SupportedFieldOptions.None;
            callSignature = String.Format("ContentGetSupportedFieldFlags ({0})", fieldIndex);
            try {
                result = Plugin.GetSupportedFieldFlags(fieldIndex);

                TraceCall(TraceLevel.Verbose, result.ToString());
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return (int)result;
        }

        #endregion ContentGetSupportedFieldFlags

        #region ContentSetValue

        [DllExport(EntryPoint = "ContentSetValue")]
        public static int SetValue([MarshalAs(UnmanagedType.LPStr)]string fileName,
                int fieldIndex, int unitIndex, int fieldType, IntPtr fieldValue, int flags) {
            return SetValueW(fileName, fieldIndex, unitIndex, fieldType, fieldValue, flags);
        }

        [DllExport(EntryPoint = "ContentSetValueW")]
        public static int SetValueW([MarshalAs(UnmanagedType.LPWStr)]string fileName,
                int fieldIndex, int unitIndex, int fieldType, IntPtr fieldValue, int flags) {
            SetValueResult result;
            ContentFieldType fldType = (ContentFieldType)fieldType;
            SetValueFlags svFlags = (SetValueFlags)flags;
            callSignature = String.Format("ContentSetValue '{0}' ({1}/{2}/{3})",
                fileName, fieldIndex, unitIndex, svFlags.ToString());
            try {
                ContentValue value = new ContentValue(fieldValue, fldType);
                result = Plugin.SetValue(fileName, fieldIndex, unitIndex, fldType,
                    value.StrValue, svFlags);

                TraceCall(TraceLevel.Info, String.Format("{0} - {1}", result.ToString(), value.StrValue));
            } catch (Exception ex) {
                ProcessException(ex);
                result = SetValueResult.NoSuchField;
            }
            return (int)result;
        }

        #endregion ContentSetValue

        #region ContentGetDetectString

        // ContentGetDetectString functionality is implemented here, not included to Content Plugin interface.
        [DllExport(EntryPoint = "ContentGetDetectString")]
        public static int GetDetectString(IntPtr detectString, int maxLen) {
            callSignature = "GetDetectString";
            try {
                TcUtils.WriteStringAnsi(Plugin.DetectString, detectString, maxLen);

                TraceCall(TraceLevel.Info, Plugin.DetectString);
            } catch (Exception ex) {
                ProcessException(ex);
            }
            return 0;
        }

        #endregion ContentGetDetectString

        #region ContentSetDefaultParams

        // ContentSetDefaultParams functionality is implemented here, not included to Content Plugin interface.
        [DllExport(EntryPoint = "ContentSetDefaultParams")]
        public static void SetDefaultParams(ref PluginDefaultParams defParams) {
            callSignature = "SetDefaultParams";
            try {
                Plugin.DefaultParams = defParams;

                TraceCall(TraceLevel.Info, null);
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion ContentSetDefaultParams

        #region ContentEditValue

        [DllExport(EntryPoint = "ContentEditValue")]
        public static int EditValue(IntPtr parentWin, int fieldIndex, int unitIndex,
                int fieldType, IntPtr fieldValue, int maxLen, int flags,
                [MarshalAs(UnmanagedType.LPStr)]string langIdentifier) {
            SetValueResult result;
            ContentFieldType fldType = (ContentFieldType)fieldType;
            EditValueFlags evFlags = (EditValueFlags)flags;
            callSignature = String.Format("ContentEditValue ({0}/{1}/{2})",
                fieldIndex, unitIndex, evFlags.ToString());
            try {
                TcWindow tcWin = new TcWindow(parentWin);
                ContentValue value = new ContentValue(fieldValue, fldType);
                string strValue = value.StrValue;
                result = Plugin.EditValue(tcWin, fieldIndex, unitIndex, fldType,
                    ref strValue, maxLen, evFlags, langIdentifier);
                if (result == SetValueResult.Success) {
                    value.StrValue = strValue;
                    value.CopyTo(fieldValue);
                }

                TraceCall(TraceLevel.Info, String.Format("{0} - {1}", result.ToString(), value.StrValue));
            } catch (Exception ex) {
                ProcessException(ex);
                result = SetValueResult.NoSuchField;
            }
            return (int)result;
        }

        #endregion ContentEditValue

        #region ContentSendStateInformation

        [DllExport(EntryPoint = "ContentSendStateInformation")]
        public static void SendStateInformation(int state, [MarshalAs(UnmanagedType.LPStr)]string path) {
            SendStateInformationW(state, path);
        }

        [DllExport(EntryPoint = "ContentSendStateInformationW")]
        public static void SendStateInformationW(int state, [MarshalAs(UnmanagedType.LPWStr)]string path) {
            callSignature = "ContentSendStateInformation";
            try {
                Plugin.SendStateInformation((StateChangeInfo)state, path);

                TraceCall(TraceLevel.Info, null);
            } catch (Exception ex) {
                ProcessException(ex);
            }
        }

        #endregion ContentSendStateInformation

        #region ContentCompareFiles

        [DllExport(EntryPoint = "ContentCompareFiles")]
        public static int CompareFiles(ContentProgressCallback progressCallback, int compareIndex,
                [MarshalAs(UnmanagedType.LPStr)]string fileName1,
                [MarshalAs(UnmanagedType.LPStr)]string fileName2,
                ref ContentFileDetails contentFileDetails) {
            return CompareFilesW(progressCallback, compareIndex, fileName1, fileName2, ref contentFileDetails);
        }

        [DllExport(EntryPoint = "ContentCompareFilesW")]
        public static int CompareFilesW(ContentProgressCallback progressCallback, int compareIndex,
                [MarshalAs(UnmanagedType.LPWStr)]string fileName1,
                [MarshalAs(UnmanagedType.LPWStr)]string fileName2,
                ref ContentFileDetails contentFileDetails) {
            callSignature = String.Format("ContentCompareFiles '{0}' => '{1}' ({2})",
                fileName1, fileName2, compareIndex);
            TcCallback.SetContentPluginCallback(progressCallback);
            try {
                int iconResourceId;
                ContentCompareResult result = Plugin.CompareFiles(compareIndex, fileName1, fileName2,
                    contentFileDetails, out iconResourceId);

                TraceCall(TraceLevel.Info, result.ToString());

                if (result == ContentCompareResult.EqualWithIcon && iconResourceId >= 100)
                    return iconResourceId;
                else
                    return (int)result;
            } catch (Exception ex) {
                ProcessException(ex);
                return (int)ContentCompareResult.FileOpenError;
            } finally {
                TcCallback.SetContentPluginCallback(null);
            }
        }

        #endregion ContentCompareFiles

        #endregion Content Plugin Exported Functions

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
