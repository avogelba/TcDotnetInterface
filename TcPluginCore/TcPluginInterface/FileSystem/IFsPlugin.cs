using System;
using System.Drawing;
using System.IO;

namespace OY.TotalCommander.TcPluginInterface.FileSystem {
    [CLSCompliant(false)]
    public interface IFsPlugin {
        #region Mandatory Methods

        object FindFirst(string path, out FindData findData);
        bool FindNext(ref object o, out FindData findData);
        int FindClose(object o);

        #endregion Mandatory Methods

        #region Optional Methods

        FileSystemExitCode GetFile(string remoteName, ref string localName, CopyFlags copyFlags, RemoteInfo remoteInfo);
        FileSystemExitCode PutFile(string localName, ref string remoteName, CopyFlags copyFlags);
        FileSystemExitCode RenMovFile(string oldName, string newName, bool move, bool overwrite, RemoteInfo remoteInfo);
        bool DeleteFile(string fileName);
        bool RemoveDir(string dirName);
        bool MkDir(string dir);
        ExecResult ExecuteOpen(TcWindow mainWin, ref string remoteName);
        ExecResult ExecuteProperties(TcWindow mainWin, string remoteName);
        ExecResult ExecuteCommand(TcWindow mainWin, ref string remoteName, string command);
        bool SetAttr(string remoteName, FileAttributes attr);
        bool SetTime(string remoteName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime);
        bool Disconnect(string disconnectRoot);
        void StatusInfo(string remoteDir, InfoStartEnd startEnd, InfoOperation infoOperation);
        ExtractIconResult ExtractCustomIcon(ref string remoteName, ExtractIconFlags extractFlags, out Icon icon);
        PreviewBitmapResult GetPreviewBitmap(ref string remoteName, int width, int height, out Bitmap returnedBitmap);
        bool GetLocalName(ref string remoteName, int maxLen);

        // FsContent... methods - are determined in IContentPlugin interface

        #endregion Optional Methods
    }
}
