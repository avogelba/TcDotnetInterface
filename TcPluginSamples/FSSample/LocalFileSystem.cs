using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.FileSystem;

namespace OY.TotalCommander.TcPlugins.FSSample {
    public class LocalFileSystem: FsPlugin {
        #region Constants

        public const string ErrorFileNotFoundMsg = "The system cannot find the file specified";
        public const string ErrorRequestAbortedMsg = "The request was aborted";

        #endregion Constants

        #region Constructors

        public LocalFileSystem(StringDictionary pluginSettings)
                : base(pluginSettings) {
            BackgroundFlags = FsBackgroundFlags.Download;
            if (String.IsNullOrEmpty(Title))
                Title = "LFS Test";
        }

        ~LocalFileSystem() {
            TraceProc(TraceLevel.Warning, "LocalFileSystem's destructor is called.");
        }

        #endregion Constructors

        private bool canDisconnect;
        private string currentConnection;

        #region IFsPlugin Members

        public override object FindFirst(string path, out FindData findData) {
            findData = null;
            if (path == "\\") {
                //root, get drive names
                IEnumerator driveEnum = DriveInfo.GetDrives().GetEnumerator();
                if (driveEnum.MoveNext()) {
                    DriveInfo drive = (DriveInfo)driveEnum.Current;
                    if (drive != null)
                        GetFindData(drive.Name, out findData);
                    return driveEnum;
                }
                return null;
            }
            IEnumerator dirFileEnum = Directory.GetFileSystemEntries(PreparePath(path)).GetEnumerator();
            if (dirFileEnum.MoveNext()) {
                string fsEntry = (string)dirFileEnum.Current;
                GetFindData(fsEntry, out findData);
                return dirFileEnum;
            }
            return null;
        }

        public override bool FindNext(ref object o, out FindData findData) {
            findData = null;
            if (!(o is IEnumerator))
                return false;
            IEnumerator fsEnum = (IEnumerator)o;
            if (fsEnum.MoveNext()) {
                object current = fsEnum.Current;
                if (current != null) {
                    if (current is DriveInfo)
                        GetFindData(((DriveInfo)current).Name, out findData);
                    else if (current is string)
                        GetFindData((string)current, out findData);
                    else
                        throw new InvalidOperationException("Unknown type in FindNext: " + current.GetType().FullName);
                    return true;
                }
            }
            return false;
        }

        public override bool MkDir(string dir) {
            if (dir.Equals("\\"))
                return false;
            try {
                Directory.CreateDirectory(PreparePath(dir));
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public override bool RemoveDir(string dirName) {
            if (dirName.Equals("\\"))
                return false;
            try {
                Directory.Delete(PreparePath(dirName));
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public override bool DeleteFile(string fileName) {
            try {
                File.Delete(fileName.Substring(1));
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public override FileSystemExitCode RenMovFile(string oldName, string newName, bool move, bool overwrite, 
                RemoteInfo remoteInfo) {
            oldName = oldName.Substring(1);
            if (!File.Exists(oldName))
                return FileSystemExitCode.FileNotFound;
            newName = newName.Substring(1);
            if (File.Exists(newName) & !overwrite)
                return FileSystemExitCode.FileExists;
            try {
                if (move) {
                    MoveFileOptions options = 
                        (overwrite) ? MoveFileOptions.ReplaceExisting : MoveFileOptions.None;
                    FileRoutines.MoveFile(new FileInfo(oldName), new FileInfo(newName),
                        options, OnCopyFile);
                } else {
                    CopyFileOptions options = 
                        (overwrite) ? CopyFileOptions.None : CopyFileOptions.FailIfDestinationExists;
                    FileRoutines.CopyFile(new FileInfo(oldName), new FileInfo(newName),
                        options, OnCopyFile);
                }
                return FileSystemExitCode.OK;
            } catch (IOException ex) {
                if (ex.Message.Equals(ErrorRequestAbortedMsg))
                    return FileSystemExitCode.UserAbort;
                if (ex.Message.Equals(ErrorFileNotFoundMsg))
                    return FileSystemExitCode.FileNotFound;
                MessageBox.Show(
                    "File operation error:" + Environment.NewLine + ex.Message,
                    "LFS Plugin (RenMovFile)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return FileSystemExitCode.ReadError;
            }
        }

        public override FileSystemExitCode GetFile(string remoteName, ref string localName, CopyFlags copyFlags,
                RemoteInfo remoteInfo) {
            bool overWrite = (CopyFlags.Overwrite & copyFlags) != 0;
            remoteName = remoteName.Substring(1);

            if (File.Exists(localName) & !overWrite)
                return FileSystemExitCode.FileExists;
            try {
                if ((CopyFlags.Move & copyFlags) != 0) {
                    MoveFileOptions options = 
                        (overWrite) ? MoveFileOptions.ReplaceExisting : MoveFileOptions.None;
                    FileRoutines.MoveFile(new FileInfo(remoteName), new FileInfo(localName),
                        options, OnCopyFile);
                } else {
                    CopyFileOptions options = 
                        (overWrite) ? CopyFileOptions.None : CopyFileOptions.FailIfDestinationExists;
                    FileRoutines.CopyFile(new FileInfo(remoteName), new FileInfo(localName),
                        options, OnCopyFile);
                }
                TraceProc(TraceLevel.Warning, "Plugin file '" + remoteName + "' transferred!");
                return FileSystemExitCode.OK;
            } catch (Exception ex) {
                MessageBox.Show(
                    "File operation error:" + Environment.NewLine + ex.Message,
                    "LFS Plugin (GetFile)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return FileSystemExitCode.ReadError;
            }
        }

        public override FileSystemExitCode PutFile(string localName, ref string remoteName, CopyFlags copyFlags) {
            bool overWrite = (CopyFlags.Overwrite & copyFlags) != 0;
            string rmtName = remoteName.Substring(1);
            if (File.Exists(rmtName) & !overWrite)
                return FileSystemExitCode.FileExists;
            try {
                if ((CopyFlags.Move & copyFlags) != 0) {
                    MoveFileOptions options = 
                        (overWrite) ? MoveFileOptions.ReplaceExisting : MoveFileOptions.None;
                    FileRoutines.MoveFile(new FileInfo(localName), new FileInfo(rmtName), options, OnCopyFile);
                } else {
                    CopyFileOptions options = 
                        (overWrite) ? CopyFileOptions.None : CopyFileOptions.FailIfDestinationExists;
                    FileRoutines.CopyFile(new FileInfo(localName), new FileInfo(rmtName), options, OnCopyFile);
                }
                TraceProc(TraceLevel.Warning, "Local file '" + localName + "' transferred!");
                return FileSystemExitCode.OK;
            } catch (Exception ex) {
                MessageBox.Show(
                    "File operation error:" + Environment.NewLine + ex.Message,
                    "LFS Plugin (PutFile)", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return FileSystemExitCode.WriteError;
            }
        }

        List<string> ftpConnections = new List<string>();
        public override ExecResult ExecuteCommand(TcWindow mainWin, ref string remoteName, string command) {
            ExecResult result = ExecResult.Yourself;
            if (String.IsNullOrEmpty(command))
                return result;
            string[] cmdPars = command.Split(new[] { ' ', '\\' });
            if (cmdPars[0].Equals("log", StringComparison.InvariantCultureIgnoreCase)) {
                string logString = ((cmdPars.Length < 3 || String.IsNullOrEmpty(cmdPars[2])) ? null : cmdPars[2]);
                if (cmdPars.Length > 1) {
                    canDisconnect = true;
                    if (cmdPars[1].Equals("connect", StringComparison.InvariantCultureIgnoreCase)) {
                        //msgType = LogMsgType.Connect;
                        currentConnection = logString == null ? Title : logString;
                        LogProc(LogMsgType.Connect, "CONNECT \\" + currentConnection);
                        ftpConnections.Add(currentConnection);
                        LogProc(LogMsgType.Details, String.Format("Connection to {0} established.", currentConnection));
                    } else if (cmdPars[1].Equals("disconnect", StringComparison.InvariantCultureIgnoreCase)) {
                        LogProc(LogMsgType.Disconnect, "Disconnect from: " + logString);
                    } else if (cmdPars[1].Equals("details", StringComparison.InvariantCultureIgnoreCase)) {
                        LogProc(LogMsgType.Details, logString);
                    } else if (cmdPars[1].Equals("trComplete", StringComparison.InvariantCultureIgnoreCase)) {
                        LogProc(LogMsgType.TransferComplete, "Transfer complete: \\" + remoteName + " -> " + logString);
                    } else if (cmdPars[1].Equals("error", StringComparison.InvariantCultureIgnoreCase)) {
                        LogProc(LogMsgType.ImportantError, logString);
                    } else if (cmdPars[1].Equals("opComplete", StringComparison.InvariantCultureIgnoreCase)) {
                        LogProc(LogMsgType.OperationComplete, logString);
                    }
                    result = ExecResult.OK;
                }
                
                //currentConnection = "\\" + ((cmdPars.Length < 2 || String.IsNullOrEmpty(cmdPars[1])) ? Title : cmdPars[1]);
                //LogProc(LogMsgType.Connect, "CONNECT " + currentConnection);
                //if (cmdPars.Length > 2)
                //    LogProc(LogMsgType.Details,
                //        String.Format("Connection to {0} established with {1}.", currentConnection, cmdPars[2]));
                //result = ExecResult.OK;
            } else if (cmdPars[0].Equals("req", StringComparison.InvariantCultureIgnoreCase)) {
                RequestType requestType = RequestType.Other;
                if (cmdPars.Length > 1) {
                    if (cmdPars[1].Equals("UserName", StringComparison.InvariantCultureIgnoreCase))
                        requestType = RequestType.UserName;
                    else if (cmdPars[1].Equals("Password", StringComparison.InvariantCultureIgnoreCase))
                        requestType = RequestType.Password;
                    else if (cmdPars[1].Equals("Account", StringComparison.InvariantCultureIgnoreCase))
                        requestType = RequestType.Account;
                    else if (cmdPars[1].Equals("TargetDir", StringComparison.InvariantCultureIgnoreCase))
                        requestType = RequestType.TargetDir;
                    else if (cmdPars[1].Equals("url", StringComparison.InvariantCultureIgnoreCase))
                        requestType = RequestType.Url;
                    else if (cmdPars[1].Equals("DomainInfo", StringComparison.InvariantCultureIgnoreCase))
                        requestType = RequestType.DomainInfo;
                }
                string customText = (requestType == RequestType.Other) ? "Input value:" : null;
                string testValue = (cmdPars.Length > 2) ? cmdPars[2] : null;
                if (RequestProc(requestType, "Request Callback Test", customText, ref testValue, 2048)) {
                    MessageBox.Show(testValue,
                        String.Format("Request for '{0}' returned:", requestType.ToString()),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    result = ExecResult.OK;
                }
            } else if (cmdPars[0].Equals("crypt", StringComparison.InvariantCultureIgnoreCase)) {
                string connectionName = "LFS Test Connection";
                if (cmdPars.Length > 2)
                    connectionName = cmdPars[2];
                string password = "qwerty";
                if (cmdPars.Length > 3)
                    password = cmdPars[3];
                CryptResult cryptRes = CryptResult.PasswordNotFound;
                if (cmdPars.Length > 1) {
                    if (cmdPars[1].Equals("Save", StringComparison.InvariantCultureIgnoreCase))
                        cryptRes = Password.Save(connectionName, password);
                    else if (cmdPars[1].Equals("Load", StringComparison.InvariantCultureIgnoreCase))
                        cryptRes = Password.Load(connectionName, ref password);
                    else if (cmdPars[1].Equals("LoadNoUi", StringComparison.InvariantCultureIgnoreCase))
                        cryptRes = Password.LoadNoUI(connectionName, ref password);
                    else if (cmdPars[1].Equals("Copy", StringComparison.InvariantCultureIgnoreCase))
                        cryptRes = Password.Copy(connectionName, password);
                    else if (cmdPars[1].Equals("Move", StringComparison.InvariantCultureIgnoreCase))
                        cryptRes = Password.Move(connectionName, password);
                    else if (cmdPars[1].Equals("Delete", StringComparison.InvariantCultureIgnoreCase))
                        cryptRes = Password.Delete(connectionName);
                }
                string s = String.Format("Crypt for '{0}' returned '{1}'", cmdPars[1], cryptRes.ToString());
                if (cryptRes == CryptResult.OK)
                    s += " (" + password + ")";
                string testValue = null;
                RequestProc(RequestType.MsgYesNo, null, s, ref testValue, 0);
                result = ExecResult.OK;
            }
            return result;
        }

        public override bool SetAttr(string remoteName, FileAttributes attr) {
            try {
                File.SetAttributes(remoteName.Substring(1), attr);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public override bool SetTime(string remoteName, DateTime? creationTime, DateTime? lastAccessTime,
                DateTime? lastWriteTime) {
            try {
                remoteName = remoteName.Substring(1);
                if (creationTime.HasValue) {
                    if (Directory.Exists(remoteName))
                        Directory.SetCreationTime(remoteName, creationTime.Value);
                    else if (File.Exists(remoteName))
                        File.SetCreationTime(remoteName, creationTime.Value);
                }
                if (lastAccessTime.HasValue) {
                    if (Directory.Exists(remoteName))
                        Directory.SetLastAccessTime(remoteName, lastAccessTime.Value);
                    else if (File.Exists(remoteName))
                        File.SetLastAccessTime(remoteName, lastAccessTime.Value);
                }
                if (lastWriteTime.HasValue) {
                    if (Directory.Exists(remoteName))
                        Directory.SetLastWriteTime(remoteName, lastWriteTime.Value);
                    else if (File.Exists(remoteName))
                        File.SetLastWriteTime(remoteName, lastWriteTime.Value);
                }
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public override bool Disconnect(string disconnectRoot) {
            if (canDisconnect) {
                string msg = String.Format("Do you really want to disconnect from \"{0}\"", disconnectRoot);
                string s = "Yes";
                if (RequestProc(RequestType.MsgYesNo, "Disconnect?", msg, ref s, 45)) {
                    canDisconnect = false;
                    LogProc(LogMsgType.Details, String.Format("Trying to disconnect {0} ...", disconnectRoot));
//                    LogProc(LogMsgType.Disconnect, null);
                    return true;
                }
                MessageBox.Show("Sorry, we are not able to disconnect " + disconnectRoot,
                                "Can not disconnect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return false;
        }

        public override ExtractIconResult ExtractCustomIcon(ref string remoteName, ExtractIconFlags extractFlags, 
                out Icon icon) {
            icon = null;
            ExtractIconResult result = ExtractIconResult.UseDefault;
            string iconFile = null;
            remoteName = remoteName.Substring(1);
            if (remoteName.EndsWith(@"..\"))
                return ExtractIconResult.UseDefault;
            if (Directory.Exists(remoteName)) {
                iconFile = "DATABASE.ico";
                result = ExtractIconResult.LoadFromFile;
            } else if (File.Exists(remoteName)) {
                string extension = Path.GetExtension(remoteName);
                if (extension != null) {
                    if (extension.ToUpper().Equals(".TXT")) {
                        icon = Icon.ExtractAssociatedIcon(@"C:\WINDOWS\system32\notepad.exe");
                        result = ExtractIconResult.Extracted;
                        iconFile = "Notepad";
                    } else if (extension.ToUpper().Equals(".EXE")) {
                        iconFile = "Table.ico";
                        result = ExtractIconResult.LoadFromFile;
                    }
                }
            }
            if (!result.Equals(ExtractIconResult.UseDefault)) {
                remoteName = Path.Combine(Settings["iconFolder"], iconFile);
            }
            return result;
        }

        //public override bool LinksToLocalFiles()
        //{
        //    return false;  
        //}

        //public override FsBackgroundFlags GetBackgroundFlags()
        //{
        //    return FsBackgroundFlags.Download;   // | FsBackgroundFlags.AskUser;
        //}

        private static Random rnd = new Random(unchecked((int)(DateTime.Now.Ticks)));

        public override PreviewBitmapResult GetPreviewBitmap(ref string remoteName, int width, int height,
                out Bitmap returnedBitmap) {
            string[] images = new[] { "Blue hills.jpg", "Sunset.jpg", "Water lilies.jpg", "Winter.jpg" };
            string imageFolder = Settings["iconFolder"];

            returnedBitmap = null;
            PreviewBitmapResult result = PreviewBitmapResult.None;
            string bitmapFile;
            remoteName = remoteName.Substring(1);
            if (remoteName.EndsWith(@"..\"))
                return PreviewBitmapResult.None;
            if (Directory.Exists(remoteName)) {
                int imgIndex = rnd.Next(4);
                bitmapFile = Path.Combine(imageFolder, images[imgIndex]);
                Image img = Image.FromFile(bitmapFile);
                returnedBitmap = new Bitmap(img, width / rnd.Next(1, 3), height / rnd.Next(1, 3));
                result = PreviewBitmapResult.Extracted;
            } else if (File.Exists(remoteName)) {
                string extension = Path.GetExtension(remoteName);
                if (extension != null) {
                    if (extension.ToUpper().Equals(".TXT")) {
                        bitmapFile = "logo_large.gif";
                        remoteName = Path.Combine(imageFolder, bitmapFile);
                        result = PreviewBitmapResult.ExtractYourself;
                    } else if (extension.ToUpper().Equals(".EXE")) {
                        bitmapFile = "Exe_Bmp.png";
                        string tmpFile = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), bitmapFile);
                        try {
                            File.Copy(Path.Combine(imageFolder, bitmapFile), tmpFile, false);
                        } catch (Exception) {
                        }
                        remoteName = tmpFile;
                        result = PreviewBitmapResult.ExtractYourselfAndDelete;
                    }
                }
                result = result | PreviewBitmapResult.Cache;
            }
            return result;
        }

        public override bool GetLocalName(ref string remoteName, int maxLen) {
            if (remoteName.Length > 1)
                remoteName = remoteName.Substring(1);
            return true;
        }

        #endregion IFsPlugin Members

        public override int OnTcPluginEvent(PluginEventArgs e) {
            return base.OnTcPluginEvent(e);
        }

        #region Private Methods

        private CopyFileCallbackAction OnCopyFile(FileInfo source, FileInfo destination, object state,
                long totalFileSize, long totalBytesTransferred) {
            if (totalFileSize == 0)
                return CopyFileCallbackAction.Continue;
            int percDone = Decimal.ToInt32((totalBytesTransferred * 100) / totalFileSize);

            int result = ProgressProc(source.FullName, destination.FullName, percDone);
            if (result == 0)
                return CopyFileCallbackAction.Continue;
            return CopyFileCallbackAction.Cancel;
        }

        private static void GetFindData(string path, out FindData findData) {
            if (path.Length == 3 && path.EndsWith(":\\")) {
                DriveInfo dInfo = new DriveInfo(path.Substring(0, 1));
                if (Directory.Exists(path))
                    findData = new FindData(path.Substring(0, 2), (ulong)dInfo.TotalSize, FileAttributes.Directory);
                else
                    findData = new FindData(path.Substring(0, 2), FileAttributes.Directory);
            } else if (Directory.Exists(path)) {
                DirectoryInfo info = new DirectoryInfo(path);
                findData = new FindData(Path.GetFileName(path), 0, info.Attributes,
                    info.LastWriteTime, info.CreationTime, info.LastAccessTime);
            } else if (File.Exists(path)) {
                FileInfo info = new FileInfo(path);
                findData = new FindData(Path.GetFileName(path), (ulong)info.Length, info.Attributes,
                    info.LastWriteTime, info.CreationTime, info.LastAccessTime);
            } else if (path.StartsWith("\\\\") && path.IndexOf('\\', 2) > 2) {
                findData = new FindData(Path.GetFileName(path), FileAttributes.Directory);
            } else
                throw new FileNotFoundException("File not found", path);
        }

        private static string PreparePath(string path) {
            return path.Substring(1) + "\\";
        }

        #endregion Private Methods
    }
}
