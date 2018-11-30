using System;
using System.Collections.Generic;
using System.Threading;

namespace OY.TotalCommander.TcPluginInterface {
    internal class RefCountObject {
        public object Obj { get; private set; }
        public int RefCount { get; private set; }

        public RefCountObject(object o) {
            Obj = o;
            RefCount = 1;
        }

        public void Update(object o) {
            Obj = o;
            RefCount++;
        }
    }

    // Some TC plugin methods return handles as pointers to internal plugin structures.
    // This class contains methods for TC plugin Handle management.
    public static class TcHandles {
        #region Handle Management

        private static Dictionary<IntPtr, RefCountObject> handleDictionary =
            new Dictionary<IntPtr, RefCountObject>();
        private static int lastHandle;
        private static object handleSyncObj = new Object();

        public static IntPtr AddHandle(object obj) {
            Monitor.Enter(handleSyncObj);
            try {
                lastHandle++;
                IntPtr handle = new IntPtr(lastHandle);
                handleDictionary.Add(handle, new RefCountObject(obj));
                return handle;
            } finally {
                Monitor.Exit(handleSyncObj);
            }
        }

        public static void AddHandle(IntPtr handle, object obj) {
            Monitor.Enter(handleSyncObj);
            try {
                handleDictionary.Add(handle, new RefCountObject(obj));
            } finally {
                Monitor.Exit(handleSyncObj);
            }
        }

        public static object GetObject(IntPtr handle) {
            Monitor.Enter(handleSyncObj);
            try {
                return handleDictionary.ContainsKey(handle) ? handleDictionary[handle].Obj : null;
            } finally {
                Monitor.Exit(handleSyncObj);
            }
        }

        public static int GetRefCount(IntPtr handle) {
            Monitor.Enter(handleSyncObj);
            try {
                if (handleDictionary.ContainsKey(handle))
                    return (handleDictionary[handle]).RefCount;
                else
                    return -1;
            } finally {
                Monitor.Exit(handleSyncObj);
            }
        }

        public static void UpdateHandle(IntPtr handle, object obj) {
            Monitor.Enter(handleSyncObj);
            try {
                (handleDictionary[handle]).Update(obj);
            } finally {
                Monitor.Exit(handleSyncObj);
            }
        }

        public static int RemoveHandle(IntPtr handle) {
            Monitor.Enter(handleSyncObj);
            try {
                if (handleDictionary.ContainsKey(handle)) {
                    int result = (handleDictionary[handle]).RefCount;
                    handleDictionary.Remove(handle);
                    return result;
                } else
                    return -1;
            } finally {
                Monitor.Exit(handleSyncObj);
            }
        }

        #endregion Handle Management
    }
}
