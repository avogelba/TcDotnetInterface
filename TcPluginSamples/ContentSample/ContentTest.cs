using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Security;

using OY.TotalCommander.TcPluginInterface.Content;

namespace OY.TotalCommander.TcPlugins.ContentSample {
    public sealed class ContentTest: ContentPlugin {
        private const int FieldCount = 5;
        private const int FileTypeIdx = 0;
        private const int Size32Idx = 1;
        private const int CreateDateIdx = 2;
        private const int CreateTimeIdx = 3;
        private const int CreateDateTimeIdx = 4;

        private const int IndexForCompareFiles = 10000;
        private static readonly string[] FieldNames = new[] {
            "FileType", "Size_F", "CreateDate", "CreateTime" , "CreateDateTime"
        };

        private static readonly string[] FieldUnits = new[] {
            "", "", "", "", ""
        };

        private static readonly ContentFieldType[] FieldTypes = new[] {
            ContentFieldType.WideString, ContentFieldType.NumericFloating, ContentFieldType.Date,
            ContentFieldType.Time, ContentFieldType.DateTime
        };



        #region Constructors

        public ContentTest(StringDictionary pluginSettings)
            : base(pluginSettings) {
            Title = "Content-Test";
            DetectString = "!(EXT=\"TXT\" | EXT=\"PDF\")";
        }

        #endregion

        #region IContentPlugin Members

        public override ContentFieldType GetSupportedField(int fieldIndex, out string fieldName,
                out string units, int maxLen) {
            fieldName = String.Empty;
            units = String.Empty;
            if (fieldIndex == IndexForCompareFiles) {
                fieldName = "Compare as text";
                return ContentFieldType.CompareContent;
            }
            if (fieldIndex < 0 || fieldIndex >= FieldCount)
                return ContentFieldType.NoMoreFields;
            fieldName = FieldNames[fieldIndex];
            if (fieldName.Length > maxLen)
                fieldName = fieldName.Substring(0, maxLen);
            units = FieldUnits[fieldIndex];
            if (units.Length > maxLen)
                units = units.Substring(0, maxLen);
            return FieldTypes[fieldIndex];
        }

        public override GetValueResult GetValue(string fileName, int fieldIndex, int unitIndex,
                int maxLen, GetValueFlags flags, out string fieldValue, out ContentFieldType fieldType) {
            getAborted = false;
            GetValueResult result = GetValueResult.FieldEmpty;
            fieldType = ContentFieldType.NoMoreFields;
            fieldValue = null;
            if (String.IsNullOrEmpty(fileName))
                return result;
            if (Directory.Exists(fileName)) {
                DirectoryInfo info = new DirectoryInfo(fileName);
                string timeString = info.CreationTime.ToString("G");
                switch (fieldIndex) {
                    case FileTypeIdx:
                        fieldValue = "Folder";
                        fieldType = ContentFieldType.WideString;
                        break;
                    case Size32Idx:
                        if ((flags & GetValueFlags.DelayIfSlow) != 0) {
                            fieldValue = "?";
                            result = GetValueResult.OnDemand;
                        } else {
                            try {
                                long size = GetDirectorySize(fileName);
                                fieldType = ContentFieldType.NumericFloating; 
                                fieldValue = GetSizeValue(size);
                            } catch (IOException) {
                                // Directory changed, stop long operation
                                result = GetValueResult.FieldEmpty;
                            }
                        }
                        break;
                    case CreateDateTimeIdx:
                        fieldValue = timeString;
                        fieldType = ContentFieldType.DateTime;
                        break;
                    case CreateDateIdx:
                        fieldValue = timeString;
                        fieldType = ContentFieldType.Date;
                        break;
                    case CreateTimeIdx:
                        fieldValue = timeString;
                        fieldType = ContentFieldType.Time;
                        break;
                    default:
                        result = GetValueResult.NoSuchField;
                        break;
                }
            } else if (File.Exists(fileName)) {
                FileInfo info = new FileInfo(fileName);
                string timeString = info.CreationTime.ToString("G");
                switch (fieldIndex) {
                    case FileTypeIdx:
                        string fileType;
                        switch (info.Extension.ToLower()) {
                            case ".exe":
                            case ".dll":
                            case ".sys":
                            case ".com":
                                fileType = "Program";
                                break;
                            case ".zip":
                            case ".rar":
                            case ".cab":
                            case ".7z":
                                fileType = "Archive";
                                break;
                            case ".bmp":
                            case ".jpg":
                            case ".png":
                            case ".gif":
                                fileType = "Это Image";
                                break;
                            case ".mp3":
                            case ".avi":
                            case ".wav":
                                fileType = "Multimedia";
                                break;
                            case ".htm":
                            case ".html":
                                fileType = "Web Page";
                                break;
                            default:
                                fileType = "File";
                                break;
                        }
                        if (!String.IsNullOrEmpty(fileType)) {
                            fieldValue = fileType;
                            fieldType = ContentFieldType.WideString;
                        }
                        break;
                    case Size32Idx:
                        long size = info.Length;
                        fieldType = ContentFieldType.NumericFloating;
                        fieldValue = GetSizeValue(size);
                        break;
                    case CreateDateTimeIdx:
                        fieldValue = timeString;
                        fieldType = ContentFieldType.DateTime;
                        break;
                    case CreateDateIdx:
                        fieldValue = timeString;
                        fieldType = ContentFieldType.Date;
                        break;
                    case CreateTimeIdx:
                        fieldValue = timeString;
                        fieldType = ContentFieldType.Time;
                        break;
                    default:
                        result = GetValueResult.NoSuchField;
                        break;
                }
            } else
                result = GetValueResult.FileError;
            if (!fieldType.Equals(ContentFieldType.NoMoreFields))
                result = GetValueResult.Success;
            return result;
        }

        public override void StopGetValue(string fileName) {
            getAborted = true;
        }

        public override DefaultSortOrder GetDefaultSortOrder(int fieldIndex) {
            if (fieldIndex == Size32Idx)        // field Size_32 - order descending
                return DefaultSortOrder.Desc;
            return DefaultSortOrder.Asc;
        }

        //public override  void PluginUnloading()
        //{
        //    // do nothing
        //}

        public override SupportedFieldOptions GetSupportedFieldFlags(int fieldIndex) {
            if (fieldIndex == -1)
                return SupportedFieldOptions.Edit | SupportedFieldOptions.SubstMask;
            switch (fieldIndex) {
                case CreateDateTimeIdx:
                    return SupportedFieldOptions.Edit | SupportedFieldOptions.SubstDateTime;
                case CreateDateIdx:
                    return SupportedFieldOptions.Edit | SupportedFieldOptions.SubstDate;
                case CreateTimeIdx:
                    return SupportedFieldOptions.Edit | SupportedFieldOptions.SubstTime;
                default:
                    return SupportedFieldOptions.None;
            }
        }

        public override SetValueResult SetValue(string fileName, int fieldIndex, int unitIndex,
                ContentFieldType fieldType, string fieldValue, SetValueFlags flags) {
            if (String.IsNullOrEmpty(fileName) && fieldIndex < 0)    // change attributes operation has ended
                return SetValueResult.NoSuchField;
            if (String.IsNullOrEmpty(fieldValue))
                return SetValueResult.NoSuchField;
            SetValueResult result = SetValueResult.NoSuchField;
            DateTime created = DateTime.Parse(fieldValue);
            bool dateOnly = (flags & SetValueFlags.OnlyDate) != 0;
            if (Directory.Exists(fileName)) {
                DirectoryInfo dirInfo = new DirectoryInfo(fileName);
                if (SetCombinedDateTime(ref created, dirInfo.CreationTime, fieldType, dateOnly)) {
                    Directory.SetCreationTime(fileName, created);
                    result = SetValueResult.Success;
                }
            } else if (File.Exists(fileName)) {
                FileInfo fileInfo = new FileInfo(fileName);
                if (SetCombinedDateTime(ref created, fileInfo.CreationTime, fieldType, dateOnly)) {
                    File.SetCreationTime(fileName, created);
                    result = SetValueResult.Success;
                }
            } else
                result = SetValueResult.FileError;
            return result;
        }

        public override bool GetDefaultView(out string viewContents, out string viewHeaders, out string viewWidths,
                out string viewOptions, int maxLen) {
            viewContents = "[=<fs>.FileType]\\n[=<fs>.Size_F]\\n[=<fs>.CreateDateTime]";
            viewHeaders = "File Type\\nSize(Float)\\nCreated";
            viewWidths = "80,30,80,-80,-80";
            viewOptions = "-1|1";
            return true;
        }

        public override void SendStateInformation(StateChangeInfo state, string path) {
            // do nothing, just for trace
        }

        public override ContentCompareResult CompareFiles(int compareIndex, string fileName1, 
                string fileName2, ContentFileDetails fileDetails, out int iconResourceId) {
            iconResourceId = -1;
            // You can call ContentProgressProc(int) here to inform TC about comparing progress. 
            return ContentCompareResult.CannotCompare;
        }

        #endregion IContentPlugin Members

        #region Private Methods

        private bool getAborted;

        private static bool SetCombinedDateTime(ref DateTime newTime, DateTime currentTime,
                ContentFieldType fieldType, bool dateOnly) {
            bool result = true;
            switch (fieldType) {
                case ContentFieldType.DateTime:
                    if (dateOnly) {
                        newTime = new DateTime(
                            newTime.Year, newTime.Month, newTime.Day,
                            currentTime.Hour, currentTime.Minute, currentTime.Second);
                    }
                    break;
                case ContentFieldType.Date:
                    newTime = new DateTime(
                        newTime.Year, newTime.Month, newTime.Day,
                        currentTime.Hour, currentTime.Minute, currentTime.Second);
                    break;
                case ContentFieldType.Time:
                    newTime = new DateTime(
                        currentTime.Year, currentTime.Month, currentTime.Day,
                        newTime.Hour, newTime.Minute, newTime.Second);
                    break;
                default:
                    result = false;
                    break;
            }
            return result;
        }

        private long GetDirectorySize(string dirPath) {
            if (getAborted)
                throw new IOException();
            long dirSize = 0;
            try {
                DirectoryInfo di = new DirectoryInfo(dirPath);
                foreach (FileInfo fi in di.GetFiles())
                    dirSize += fi.Length;
                foreach (DirectoryInfo cd in di.GetDirectories())
                    dirSize += GetDirectorySize(cd.FullName);
            } catch (SecurityException) { }
            return dirSize;
        }

        private static string GetSizeValue(long size) {
            string result = size.ToString(CultureInfo.InvariantCulture);
            double dSize = size;
            string altStr = null;
            if (dSize > 1024.0) {
                dSize /= 1024.0;
                altStr = String.Format("|{0:0} Kb", dSize);
            }
            if (dSize > 1024.0) {
                dSize /= 1024.0;
                altStr = String.Format("|{0:0} Mb", dSize);
            }
            if (dSize > 1024.0) {
                dSize /= 1024.0;
                altStr = String.Format("|{0:0} Gb", dSize);
            }
            if (!String.IsNullOrEmpty(altStr))
                result += altStr;
            return result;
        }

        #endregion Private Methods
    }
}
