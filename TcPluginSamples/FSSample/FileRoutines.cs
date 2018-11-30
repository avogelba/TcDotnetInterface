using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace OY.TotalCommander.TcPlugins.FSSample {
    // Functions defined in .NET don't allow to copy or move files with progress callback.
    // This file defines some wrappers around Windows file functions providing such callback.

    public enum CopyFileCallbackAction {
        Continue = 0,
        Cancel = 1,
        Stop = 2,
        Quiet = 3
    }

    [Flags]
    public enum CopyFileOptions {
        None = 0x0,
        FailIfDestinationExists = 0x1,
        Restartable = 0x2,
        AllowDecryptedDestination = 0x8,
        All = FailIfDestinationExists | Restartable | AllowDecryptedDestination
    }

    [Flags]
    public enum MoveFileOptions {
        None = 0x0,
        ReplaceExisting = 0x1,
        CopyAllowed = 0x2,
        DelayUntilReboot = 0x4,
        All = ReplaceExisting | CopyAllowed | DelayUntilReboot
    }


    internal class CopyProgressData {
        private FileInfo source;
        private FileInfo destination;
        private CopyFileCallback callback;
        private object state;

        public CopyProgressData(FileInfo source, FileInfo destination, CopyFileCallback callback, object state) {
            this.source = source;
            this.destination = destination;
            this.callback = callback;
            this.state = state;
        }

        public int CallbackHandler(long totalFileSize, long totalBytesTransferred,
                long streamSize, long streamBytesTransferred, int streamNumber,
                int callbackReason, IntPtr sourceFile, IntPtr destinationFile, IntPtr data) {
            return (int)callback(source, destination, state, totalFileSize, totalBytesTransferred);
        }
    }

    public delegate int CopyProgressRoutine(long totalFileSize, long totalBytesTransferred,
            long streamSize, long streamBytesTransferred, int streamNumber, int callbackReason,
            IntPtr sourceFile, IntPtr destinationFile, IntPtr data);

    public delegate CopyFileCallbackAction CopyFileCallback(FileInfo source, FileInfo destination,
            object state, long totalFileSize, long totalBytesTransferred);

    internal static class NativeMethods {
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool CopyFileEx(
                [MarshalAs(UnmanagedType.LPWStr)]string lpExistingFileName,
                [MarshalAs(UnmanagedType.LPWStr)]string lpNewFileName,
                CopyProgressRoutine lpProgressRoutine, IntPtr lpData,
                [MarshalAs(UnmanagedType.Bool)] ref bool pbCancel, int dwCopyFlags);

        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool MoveFileWithProgress(
                [MarshalAs(UnmanagedType.LPWStr)]string lpExistingFileName,
                [MarshalAs(UnmanagedType.LPWStr)]string lpNewFileName,
                CopyProgressRoutine lpProgressRoutine, IntPtr lpData, int dwFlags);

        //[SuppressUnmanagedCodeSecurity]
        //[DllImport("Kernel32.dll")]
        //private static extern int GetLastError();
    }

    public static class FileRoutines {
        public static void CopyFile(FileInfo source, FileInfo destination, CopyFileOptions options,
                CopyFileCallback callback) {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if ((options & ~CopyFileOptions.All) != 0)
                throw new ArgumentOutOfRangeException("options");

            new FileIOPermission(FileIOPermissionAccess.Read, source.FullName).Demand();
            new FileIOPermission(FileIOPermissionAccess.Write, destination.FullName).Demand();

            object state = null;
            CopyProgressRoutine cpr = (callback == null) ?
                null : new CopyProgressRoutine(new CopyProgressData(
                    source, destination, callback, state).CallbackHandler);

            bool cancel = false;
            if (!NativeMethods.CopyFileEx(source.FullName, destination.FullName, cpr, IntPtr.Zero, ref cancel, 
                    (int)options)) {
                throw new IOException(new Win32Exception().Message);
            }
        }

        public static void MoveFile(FileInfo source, FileInfo destination, MoveFileOptions options, 
                CopyFileCallback callback) {
            if (source == null)
                throw new ArgumentNullException("source");
            if (destination == null)
                throw new ArgumentNullException("destination");
            if ((options & ~MoveFileOptions.All) != 0)
                throw new ArgumentOutOfRangeException("options");

            new FileIOPermission(FileIOPermissionAccess.Write, source.FullName).Demand();
            new FileIOPermission(FileIOPermissionAccess.Write, destination.FullName).Demand();

            object state = null;
            CopyProgressRoutine cpr = (callback == null) ?
                null : new CopyProgressRoutine(new CopyProgressData(
                    source, destination, callback, state).CallbackHandler);

            if (!NativeMethods.MoveFileWithProgress(source.FullName, destination.FullName, cpr, IntPtr.Zero, 
                    (int)options)) {
                throw new IOException(new Win32Exception().Message);
            }
        }
    }
}
