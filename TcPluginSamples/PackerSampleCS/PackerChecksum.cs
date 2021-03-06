using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.Packer;

namespace OY.TotalCommander.TcPlugins.PackerSampleCS {
    public class PackerChecksum: PackerPlugin {
        private const string ExtensionsToOpen = ".MD5,.SHA,.DNMD5,.DNSHA";
        private const string ExtensionsToUpdate = ".DNMD5, .DNSHA";

        private static string archiveFile;
        private ArrayList files = new ArrayList();

        #region Constructors

        public PackerChecksum(StringDictionary pluginSettings)
                : base(pluginSettings) {
            Capabilities =
                PackerCapabilities.New | PackerCapabilities.Multiple | PackerCapabilities.Modify |
                PackerCapabilities.Delete | PackerCapabilities.Options | PackerCapabilities.ByContent |
                PackerCapabilities.Mempack;
            // TODO test - multi thread work 
            BackgroundFlags = PackBackgroundFlags.MemPack;
        }

        #endregion Constructors

        #region IPackerPlugin Members

        public override object OpenArchive(ref OpenArchiveData archiveData) {
            object result;
            archiveFile = archiveData.ArchiveName;
            string extension = Path.GetExtension(archiveFile);
            if (extension != null) {
                string arcExt = extension.ToUpper();
                if (!ExtensionsToOpen.Contains(arcExt))
                    return PackerResult.UnknownFormat;
            }
            files.Clear();
            using (StreamReader sr = new StreamReader(archiveFile)) {
                while (!sr.EndOfStream) {
                    string line = sr.ReadLine();
                    if (line != null) {
                        int pos = line.IndexOf(" *", StringComparison.Ordinal);
                        if (pos > 0) {
                            string[] node = new[] {
                                    line.Substring(pos + 2), 
                                    line.Substring(0, pos), 
                                    "false" 
                                };
                            files.Add(node);
                        }
                    }
                }
            }
            archiveData.Result = PackerResult.OK;

            result = files.GetEnumerator();
            return result;
        }

        public override PackerResult ReadHeader(ref object arcData, out HeaderData headerData) {
            headerData = null;
            if (!(arcData is IEnumerator))
                return PackerResult.ErrorOpen;
            IEnumerator fileEnum = (IEnumerator)arcData;
            if (fileEnum.MoveNext()) {
                object current = fileEnum.Current;
                if (current is Array) {
                    headerData = new HeaderData();
                    string[] node = (string[])current;
                    string fName = node[0];
                    bool found = GetHeaderData(fName, ref headerData);
                    node[2] = found.ToString();
                    return PackerResult.OK;
                }
                return PackerResult.ErrorRead;
            }
            return PackerResult.EndArchive;
        }

        public override PackerResult ProcessFile(object arcData, ProcessFileOperation operation, string destFile) {
            if (operation.Equals(ProcessFileOperation.Skip))
                return PackerResult.OK;
            string extension = Path.GetExtension(archiveFile);
            if (extension != null) {
                string arcExt = extension.ToUpper();
                if (!ExtensionsToOpen.Contains(arcExt))
                    return PackerResult.UnknownFormat;
                if (!(arcData is IEnumerator))
                    return PackerResult.ErrorRead;
                IEnumerator fileEnum = (IEnumerator)arcData;
                object current = fileEnum.Current;
                bool isOk = false;
                if (current != null) {
                    if (current is Array) {
                        string[] node = (string[])current;
                        string fName = node[0];
                        string nodeSum = node[1];
                        bool found = node.Length > 2 && Convert.ToBoolean(node[2]);

                        string checkSum = String.Empty;
                        string fileName = Path.Combine(Path.GetDirectoryName(archiveFile), fName);
                        if (found) {
                            checkSum = arcExt.EndsWith("MD5") ? 
                                GetFileHash(fileName, MD5.Create()) : GetFileHash(fileName, SHA1.Create());
                            isOk = (checkSum ?? String.Empty).Equals(nodeSum);
                        }
                        if (operation.Equals(ProcessFileOperation.Test))
                            return isOk ? PackerResult.OK : PackerResult.BadArchive;
                        using (StreamWriter sw = new StreamWriter(destFile)) {
                            sw.WriteLine(fileName + "\n");
                            sw.WriteLine("expected:	" + nodeSum);
                            sw.WriteLine("computed:	" + (found ? checkSum : "???"));
                            sw.WriteLine("\n" + (arcExt.EndsWith("MD5") ? "MD5" : "SHA") +
                                         " checksum " + (isOk ? "OK!" : "FAILED!"));
                            sw.Flush();
                        }
                        return PackerResult.OK;
                    }
                    return PackerResult.ErrorRead;
                }
                return PackerResult.ErrorRead;
            }
            return PackerResult.UnknownFormat;
        }

        public override PackerResult CloseArchive(object arcData) {
            files.Clear();
            archiveFile = null;
            return PackerResult.OK;
        }

        public override PackerResult PackFiles(string packedFile, string subPath, string srcPath,
                List<string> addList, PackFilesFlags flags) {
            if ((flags & PackFilesFlags.MoveFiles).Equals(PackFilesFlags.MoveFiles))
                return PackerResult.NotSupported;
            string arcFile = packedFile;
            string extension = Path.GetExtension(arcFile);
            if (extension != null) {
                string arcExt = extension.ToUpper();
                if (!ExtensionsToUpdate.Contains(arcExt)) {
                    MessageBox.Show(
                        "Cannot add to archive - allowed extension(s): " + ExtensionsToUpdate,
                        ".NET Packer Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return PackerResult.NotSupported;
                }
            }
            DriveInfo di = new DriveInfo(Path.GetPathRoot(srcPath));
            if (di.DriveType == DriveType.CDRom)
                return PackerResult.ErrorCreate;

            arcFile = Path.Combine(srcPath, Path.GetFileName(packedFile));
            return !File.Exists(arcFile) ? 
                CreateArchive(arcFile, srcPath, addList) : 
                UpdateArchive(arcFile, srcPath, addList);
        }

        private PackerResult CreateArchive(string arcFile, string sourcePath, List<string> fileList) {
            try {
                string extension = Path.GetExtension(arcFile);
                if (extension != null) {
                    string arcExt = extension.ToUpper();
                    using (StreamWriter sw = new StreamWriter(arcFile)) {
                        foreach (string fileName in fileList) {
                            string filePath = Path.Combine(sourcePath, fileName);
                            if (Directory.Exists(filePath)) {
                                // do nothing 
                            } else if (File.Exists(filePath)) {
                                string checkSum = arcExt.Equals(".DNMD5") ?
                                    GetFileHash(filePath, MD5.Create()) : GetFileHash(filePath, SHA1.Create());
                                sw.WriteLine(String.Format("{0} *{1}", checkSum, fileName));
                            }
                        }
                        sw.Flush();
                        return PackerResult.OK;
                    }
                }
                return PackerResult.ErrorCreate;
            } catch {
                if (File.Exists(arcFile))
                    File.Delete(arcFile);
                return PackerResult.ErrorCreate;
            }
        }

        private PackerResult UpdateArchive(string arcFile, string sourcePath, List<string> fileList) {
            string tmpFile = Path.GetTempFileName();
            try {
                string extension = Path.GetExtension(arcFile);
                if (extension != null) {
                    string arcExt = extension.ToUpper();
                    bool changed = false;
                    using (StreamReader sr = new StreamReader(arcFile))
                    using (StreamWriter sw = new StreamWriter(tmpFile, false)) {
                        while (!sr.EndOfStream) {
                            string line = sr.ReadLine();
                            if (line != null) {
                                int pos = line.IndexOf(" *", StringComparison.Ordinal);
                                if (pos > 0) {
                                    string file = line.Substring(pos + 2).Trim();
                                    if (fileList.IndexOf(file) < 0)
                                        sw.WriteLine(line);
                                    else {
                                        WriteItem(sw, sourcePath, file, arcExt);
                                        fileList.Remove(file);
                                        changed = true;
                                    }
                                } else
                                    sw.WriteLine(line);
                            }
                        }
                        foreach (string file in fileList) {
                            WriteItem(sw, sourcePath, file, arcExt);
                            changed = true;
                        }
                        sw.Flush();
                    }
                    if (changed) {
                        File.Delete(arcFile);
                        File.Move(tmpFile, arcFile);
                    } else
                        File.Delete(tmpFile);
                }
                return PackerResult.OK;
            } catch {
                if (File.Exists(tmpFile))
                    File.Delete(tmpFile);
                return PackerResult.ErrorWrite;
            }
        }

        private void WriteItem(StreamWriter sw, string sourcePath, string fileItem, string archiveExt) {
            string filePath = Path.Combine(sourcePath, fileItem);
            if (Directory.Exists(filePath)) {
                // do nothing 
            } else if (File.Exists(filePath)) {
                string checkSum = archiveExt.Equals(".DNMD5") ?
                    GetFileHash(filePath, MD5.Create()) :
                    GetFileHash(filePath, SHA1.Create());
                sw.WriteLine(String.Format("{0} *{1}", checkSum, fileItem));
            }
        }

        public override PackerResult DeleteFiles(string packedFile, List<string> deleteList) {
            string tmpFile = Path.GetTempFileName();
            try {
                string extension = Path.GetExtension(packedFile);
                if (extension != null) {
                    string arcExt = extension.ToUpper();
                    if (!ExtensionsToUpdate.Contains(arcExt)) {
                        MessageBox.Show(
                            "Cannot delete from archive - allowed extension(s): " + ExtensionsToUpdate,
                            ".NET Packer Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return PackerResult.NotSupported;
                    }
                }
                bool changed = false;
                using (StreamReader sr = new StreamReader(packedFile))
                using (StreamWriter sw = new StreamWriter(tmpFile, false)) {
                    while (!sr.EndOfStream) {
                        string line = sr.ReadLine();
                        if (line != null) {
                            int pos = line.IndexOf(" *", StringComparison.Ordinal);
                            if (pos > 0) {
                                string file = line.Substring(pos + 2).Trim();
                                if (deleteList.IndexOf(file) < 0)
                                    sw.WriteLine(line);
                                else
                                    changed = true;
                            } else
                                sw.WriteLine(line);
                        }
                    }
                }
                if (changed) {
                    File.Delete(packedFile);
                    File.Move(tmpFile, packedFile);
                } else
                    File.Delete(tmpFile);
                return PackerResult.OK;
            } catch {
                if (File.Exists(tmpFile))
                    File.Delete(tmpFile);
                return PackerResult.ErrorWrite;
            }
        }

        public override void ConfigurePacker(TcWindow parentWin) {
            MessageBox.Show(parentWin,
                "Provides MD5/SHA1 checksum generator/checker\n" +
                "from within Total Commander packer interface\n" +
                " ( idea of Stanislaw Y. Pusep,\n" +
                "   http://ghisler.fileburst.com/plugins/checksum.zip)\n\n" +
                "Author: Oleg Yuvashev",
                ".NET Test Packer Plugin",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private StringBuilder sb;
        private int totalTaken;
        private int totalWritten;
        public override object StartMemPack(MemPackOptions options, string fileName) {
            archiveFile = fileName;
            sb = new StringBuilder();
            totalTaken = 0;
            totalWritten = 0;
            return sb;
        }

        public override PackerResult PackToMem(ref object memData, byte[] bufIn, ref int taken,
                byte[] bufOut, ref int written, int seekBy) {
            if (!(memData is StringBuilder))
                return PackerResult.ErrorRead;
            sb = (StringBuilder)memData;

            written = 0;
            taken = (bufIn == null) ? 0 : bufIn.Length;
            if (taken == 0)
                return PackerResult.PackToMemDone;
            if (bufIn != null)
                bufIn.CopyTo(bufOut, 0);
            written = taken;
            sb.Append(EncodingDetector.GetString(bufIn));
            totalTaken += taken;
            totalWritten += written;
            return PackerResult.OK;
        }

        public override PackerResult DoneMemPack(object memData) {
            if (!(memData is StringBuilder))
                return PackerResult.ErrorRead;
            sb = (StringBuilder)memData;
            if (sb != null) {
                using (StreamWriter sw = new StreamWriter(archiveFile + "s")) {
                    sw.Write(sb.ToString());
                    sw.Flush();
                }
                sb.Length = 0;
            }
            sb = null;
            archiveFile = null;
            return PackerResult.OK;
        }

        public override bool CanYouHandleThisFile(string fileName) {
            string extension = Path.GetExtension(fileName);
            return extension != null && ExtensionsToOpen.Contains(extension.ToUpper());
        }

        #endregion IPackerPlugin Members

        #region Private Methods

        private static bool GetHeaderData(string fileName, ref HeaderData headerData) {
            bool result = true;
            headerData.ArchiveName = archiveFile;
            headerData.FileName = fileName;
            try {
                FileInfo fi = new FileInfo(Path.Combine(Path.GetDirectoryName(archiveFile), fileName));
                headerData.UnpackedSize = (ulong)fi.Length;
                headerData.PackedSize = (ulong)fi.Length;
                headerData.FileTime = fi.LastWriteTime;
                headerData.FileAttributes = fi.Attributes;
            } catch {
                headerData.UnpackedSize = unchecked(0xFFFFFFFFFFFFFFFF);
                headerData.FileTime = DateTime.MinValue;
                result = false;
            }
            return result;
        }

        private string GetFileHash(string fileName, HashAlgorithm hasher) {
            string input;
            using (StreamReader sr = new StreamReader(fileName)) {
                input = sr.ReadToEnd();
            }
            byte[] data = hasher.ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++) {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        #endregion Private Methods
    }
}
