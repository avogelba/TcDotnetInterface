using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using ICSharpCode.SharpZipLib.Zip;
using OY.TotalCommander.TcPluginInterface.Packer;

namespace OY.TotalCommander.TcPlugins.PackerSample.Zip {
    [Serializable]
    public class PackToMemData {
        public MemoryStream MemStream { get; private set; }
        public ZipOutputStream ZipStream { get; private set; }

        public PackToMemData(MemoryStream memStream, ZipOutputStream zipStream) {
            MemStream = memStream;
            ZipStream = zipStream;
        }
    }

    public class PackerZip: PackerPlugin {
        #region Constructors

        public PackerZip(StringDictionary pluginSettings)
                : base(pluginSettings) {
            Capabilities =
                PackerCapabilities.New | PackerCapabilities.Modify | PackerCapabilities.Multiple |
                PackerCapabilities.Delete | PackerCapabilities.Encrypt | PackerCapabilities.Mempack;
        }

        #endregion Constructors

        #region IPackerPlugin Members

        public override object OpenArchive(ref OpenArchiveData archiveData) {
            FileStream archive = File.Open(archiveData.ArchiveName, FileMode.Open);
            ZipFile zf = new ZipFile(archive);
            archiveData.Result = PackerResult.OK;
            return zf;
        }

        public override PackerResult ReadHeader(ref object arcData, out HeaderData headerData) {
            headerData = null;
            ZipFile zf = (ZipFile)arcData;
            IEnumerator arcEnum = (IEnumerator)zf;
            if (arcEnum.MoveNext()) {
                object current = arcEnum.Current;
                if (current is ZipEntry) {
                    headerData = new HeaderData();
                    GetHeaderData((ZipEntry)current, ref headerData);
                } else if (current != null)
                    throw new InvalidOperationException("Unknown type in FindNext: " + current.GetType().FullName);
            }

            return PackerResult.NotSupported;
        }

        private static void GetHeaderData(ZipEntry entry, ref HeaderData headerData) {
            headerData.FileName = entry.Name;
            headerData.PackedSize = (ulong)entry.CompressedSize;
            headerData.UnpackedSize = (ulong)entry.Size;
            headerData.FileCRC = (int)entry.Crc;
            headerData.FileTime = entry.DateTime;
            headerData.FileAttributes = 
                entry.IsDirectory ? FileAttributes.Directory : FileAttributes.Normal;
        }

        public override PackerResult ProcessFile(object arcData, ProcessFileOperation operation, string destFile) {
            return PackerResult.NotSupported;
        }

        public override PackerResult CloseArchive(object arcData) {
            return PackerResult.OK;
        }

        #region Optional Methods

        private const int ArcBufferSize = 31744;
        public override PackerResult PackFiles(string packedFile, string subPath, string srcPath,
                List<string> addList, PackFilesFlags flags) {
            PackerResult result;
            bool stopProcess = false;
            string password = null;
            try {
                if ((flags & PackFilesFlags.Encrypt).Equals(PackFilesFlags.Encrypt)) {
                    PackerPassword pswDialog = new PackerPassword(this, packedFile);
                    if (pswDialog.ShowDialog() == DialogResult.OK) {
                        password = pswDialog.Password;
                    }
                    if (String.IsNullOrEmpty(password)) {
                        return PackerResult.EAborted;
                    }
                }
                using (FileStream archive = File.Create(packedFile))
                using (ZipOutputStream zip = new ZipOutputStream(archive)) {
                    if (!String.IsNullOrEmpty(password))
                        zip.Password = password;
                    zip.SetLevel(6);
                    byte[] buff = new byte[ArcBufferSize];

                    foreach (string fileName in addList) {
                        if (stopProcess)
                            break;
                        bool skipFile = false;
                        string zipFile = Path.Combine(srcPath, fileName);
                        ZipEntry entry = new ZipEntry(fileName);
                        zip.PutNextEntry(entry);

                        if (Directory.Exists(zipFile)) {
                        } else if (File.Exists(zipFile)) {
                            using (FileStream source = File.OpenRead(zipFile)) {
                                int count;
                                while (!stopProcess && (count = source.Read(buff, 0, buff.Length)) > 0) {
                                    zip.Write(buff, 0, count);
                                    if (ProcessDataProc(zipFile, count) == 0) {
                                        stopProcess = true;
                                        break;
                                    }
                                }
                            }
                            if (stopProcess)
                                break;
                            entry.DateTime = File.GetLastWriteTime(zipFile);
                        } else
                            skipFile = true;
                        if (skipFile) {
                            // delete entry ???;
                        } else {
                            // !!! delete directory
                            if ((flags & PackFilesFlags.MoveFiles).Equals(PackFilesFlags.MoveFiles))
                                File.Delete(zipFile);
                        }
                    }
                    zip.Finish();
                    result = PackerResult.OK;
                }
                if (stopProcess && File.Exists(packedFile))
                    File.Delete(packedFile);
            } catch (Exception) {
                if (File.Exists(packedFile))
                    File.Delete(packedFile);
                throw;
            }
            return result;
        }

        public override PackerResult DeleteFiles(string packedFile, List<string> deleteList) {
            return PackerResult.NotSupported;
        }

        private static MemoryStream GetBaseOutputStream(ZipOutputStream zip) {
            const BindingFlags bf = BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo fi = typeof(ZipOutputStream).GetField("baseOutputStream_", bf);
            if (fi != null) {
                object o = fi.GetValue(zip);
                return (o as MemoryStream);
            }
            return null;
        }

        private const int BlockSize = 10 * 8192;
        private const int OutOffset = 0;

        private byte[] outBuffer = new byte[ArcBufferSize * 10];
        private int outSize;
        private long totalWritten;
        private bool memPackFinished;
        private string archiveFile;
        
        public override object StartMemPack(MemPackOptions options, string fileName) {
            archiveFile = fileName;
            byte[] zipBuff = new byte[BlockSize];

            MemoryStream memStream = new MemoryStream();
            memStream.Write(zipBuff, 0, zipBuff.Length);

            ZipOutputStream zip = new ZipOutputStream(memStream);
            zip.SetLevel(6);
            fileName = Path.GetFileNameWithoutExtension(fileName);
            ZipEntry entry = new ZipEntry(fileName) { DateTime = DateTime.Now };
            zip.PutNextEntry(entry);
            totalWritten = zip.Position;
            return zip;
        }

        public override PackerResult PackToMem(ref object memData, byte[] bufIn, ref int taken,
                byte[] bufOut, ref int written, int seekBy) {
            taken = (bufIn == null) ? 0 : bufIn.Length;

            if (!(memData is ZipOutputStream))
                return PackerResult.ErrorRead;
            ZipOutputStream zip = (ZipOutputStream)memData;
            MemoryStream memStream = GetBaseOutputStream(zip);

            written = 0;
            if (taken == 0) {
                if (memPackFinished)
                    return PackerResult.PackToMemDone;
                zip.Finish();
                written = (int)memStream.Position;
                memStream.Seek(0, SeekOrigin.Begin);
                memStream.Read(bufOut, 0, written);
                bufOut.CopyTo(outBuffer, OutOffset);
                outSize = written;
                memPackFinished = true;
                return PackerResult.OK;
            }
            zip.Write(bufIn, 0, taken);
            written = (int)(zip.Position - totalWritten);
            if (written > 0) {
            }
            written = 0;
            return PackerResult.OK;
        }

        public override PackerResult DoneMemPack(object memData) {
            if (!(memData is ZipOutputStream))
                return PackerResult.ErrorRead;
            ZipOutputStream zip = (ZipOutputStream)memData;

            if (zip != null) {
                zip.Dispose();
                zip = null;
            }
            if (outSize > 0) {
                using (FileStream fs = new FileStream(archiveFile + "s", FileMode.Create, FileAccess.Write)) {
                    fs.Write(outBuffer, 0, outSize);
                }
            }
            archiveFile = null;
            memPackFinished = true;
            return PackerResult.OK;
        }

        #endregion Optional Methods

        #endregion IPackerPlugin Members

    }
}
