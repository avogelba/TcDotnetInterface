using System;
using System.Collections.Generic;

namespace OY.TotalCommander.TcPluginInterface.Packer {
    [CLSCompliant(false)]
    public interface IPackerPlugin {
        #region Mandatory Methods

        object OpenArchive(ref OpenArchiveData archiveData);
        PackerResult ReadHeader(ref object arcData, out HeaderData headerData);
        PackerResult ProcessFile(object arcData, ProcessFileOperation operation, string destFile);
        PackerResult CloseArchive(object arcData);

        #endregion Mandatory Methods

        #region Optional Methods

        PackerResult PackFiles(string packedFile, string subPath, string srcPath, List<string> addList, 
                PackFilesFlags flags);
        PackerResult DeleteFiles(string packedFile, List<string> deleteList);
        void ConfigurePacker(TcWindow parentWin);
        object StartMemPack(MemPackOptions options, string fileName);
        PackerResult PackToMem(ref object memData, byte[] bufIn, ref int taken, byte[] bufOut, 
                ref int written, int seekBy);
        PackerResult DoneMemPack(object memData);
        bool CanYouHandleThisFile(string fileName);

        #endregion Optional Methods
    }
}