using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using OY.TotalCommander.TcPluginInterface.Content;
using OY.TotalCommander.TcPluginInterface.FileSystem;
using OY.TotalCommander.TcPluginInterface.Lister;
using OY.TotalCommander.TcPluginInterface.Packer;
using OY.TotalCommander.TcPluginInterface.QuickSearch;
using FileTime = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace OY.TotalCommander.TcPluginInterface {
    public static class TcUtils {
        const uint EmptyDateTimeHi = 0xFFFFFFFF;
        const uint EmptyDateTimeLo = 0xFFFFFFFE;

        #region Common Dictionaries

        public static Dictionary<PluginType, string> PluginInterfaces = 
            new Dictionary<PluginType, string> { 
                { PluginType.Content    , typeof(IContentPlugin).FullName },
                { PluginType.FileSystem , typeof(IFsPlugin).FullName      },
                { PluginType.Lister     , typeof(IListerPlugin).FullName  },
                { PluginType.Packer     , typeof(IPackerPlugin).FullName  },
                { PluginType.QuickSearch, typeof(IQuickSearchPlugin).FullName  }
            };

        public static Dictionary<PluginType, string> PluginNames = 
            new Dictionary<PluginType, string> { 
                { PluginType.Content    , "Content "     },
                { PluginType.FileSystem , "File System " },
                { PluginType.Lister     , "Lister "      },
                { PluginType.Packer     , "Packer "      },
                { PluginType.QuickSearch, "QuickSearch "}
            };

        #endregion Common Dictionaries

		#region Class Loading Methods

        public static object CreateInstance(Type classType, string assemblyPath, Type interfaceType, string className) {
            if (String.IsNullOrEmpty(assemblyPath) ||
                    (String.IsNullOrEmpty(className) && interfaceType == null))
                return null;
            if (classType == null) {
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveEventHandler;
                try {
                    classType = FindClass(assemblyPath, interfaceType, className);
                    if (classType == null)
                        return null;
                } finally {
                    AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolveEventHandler;
                }
            }
            try {
                object result = classType.InvokeMember(null,
                    BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.CreateInstance,
                    null, null, null);
                return result;
            } catch (Exception ex) {
                throw new Exception(className + " - Class loading error.", ex);
            }
        }

        private static Assembly AssemblyResolveEventHandler(Object sender, ResolveEventArgs args) {
			return Assembly.Load(args.Name);
		}

		private static Type FindClass(string assemblyPath, Type interfaceType, string className) {
			string interfaceTypeName = interfaceType == null ? null : interfaceType.FullName;
			if (!Path.IsPathRooted(assemblyPath))
				assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyPath);
			Assembly assembly = Assembly.LoadFrom(assemblyPath);
			foreach (Type type in assembly.GetExportedTypes()) {
				if (!String.IsNullOrEmpty(interfaceTypeName) && type.GetInterface(interfaceTypeName) == null)
					continue;
				if (!String.IsNullOrEmpty(className) && !type.FullName.EndsWith(className))
					continue;
				return type;
			}
			return null;
		}

		#endregion Class Loading Methods

		#region Long Conversion Methods

		public static int GetHigh(long value) {
            return (int)(value >> 32);
        }

        public static int GetLow(long value) {
            return (int)(value & uint.MaxValue);
        }

        public static long GetLong(int high, int low) {
            return ((long)high << 32) + low;
        }

        [CLSCompliant(false)]
        public static uint GetUHigh(ulong value) {
            return (uint)(value >> 32);
        }

        [CLSCompliant(false)]
        public static uint GetULow(ulong value) {
            return (uint)(value & uint.MaxValue);
        }

        [CLSCompliant(false)]
        public static ulong GetULong(uint high, uint low) {
            return ((ulong)high << 32) + low;
        }

        #endregion Long Conversion Methods

        #region DateTime Conversion Methods

        public static FileTime GetFileTime(DateTime? dateTime) {
            var longTime = 
                (dateTime.HasValue && dateTime.Value != DateTime.MinValue)
                    ? dateTime.Value.ToFileTime() : long.MaxValue << 1;
            return new FileTime {
                dwHighDateTime = GetHigh(longTime),
                dwLowDateTime = GetLow(longTime),
            };
        }

        [CLSCompliant(false)]
        public static ulong GetULong(DateTime? dateTime) {
            if (dateTime.HasValue && dateTime.Value != DateTime.MinValue) {
                ulong ulongTime = Convert.ToUInt64(dateTime.Value.ToFileTime());
                return ulongTime;
            }
            return GetULong(EmptyDateTimeHi, EmptyDateTimeLo);
        }

        public static DateTime? FromFileTime(FileTime fileTime) {
            try {
                long longTime = Convert.ToInt64(fileTime);
                return DateTime.FromFileTime(longTime);
            } catch (Exception) {
                return null;
            }
        }

        [CLSCompliant(false)]
        public static DateTime? FromULong(ulong fileTime) {
            long longTime = Convert.ToInt64(fileTime);
            return longTime != 0 
                ? DateTime.FromFileTime(longTime) : (DateTime?)null;
        }

        public static DateTime? ReadDateTime(IntPtr addr) {
            return addr == IntPtr.Zero 
                ? (DateTime?)null : DateTime.FromFileTime(Marshal.ReadInt64(addr));
        }

        public static int GetArchiveHeaderTime(DateTime dt) {
            if (dt.Year < 1980 || dt.Year > 2100)
                return 0;
            return 
                (dt.Year - 1980) << 25 
                    | dt.Month << 21 
                    | dt.Day << 16 
                    | dt.Hour << 11 
                    | dt.Minute << 5 
                    | dt.Second / 2;
        }

        #endregion DateTime Conversion Methods

        #region Unmanaged String Methods

        public static string ReadStringAnsi(IntPtr addr) {
            return (addr == IntPtr.Zero) 
                ? string.Empty : Marshal.PtrToStringAnsi(addr);
        }

        public static List<string> ReadStringListAnsi(IntPtr addr) {
            List<string> result = new List<string>();
            if (addr != IntPtr.Zero) {
                while (true) {
                    string s = ReadStringAnsi(addr);
                    if (String.IsNullOrEmpty(s))
                        break;
                    result.Add(s);
                    addr = new IntPtr(addr.ToInt64() + s.Length + 1);
                }
            }
            return result;
        }

        public static string ReadStringUni(IntPtr addr) {
            return (addr == IntPtr.Zero) 
                ? string.Empty : Marshal.PtrToStringUni(addr);
        }

        public static List<string> ReadStringListUni(IntPtr addr) {
            List<string> result = new List<string>();
            if (addr != IntPtr.Zero) {
                while (true) {
                    string s = ReadStringUni(addr);
                    if (String.IsNullOrEmpty(s))
                        break;
                    result.Add(s);
                    addr = new IntPtr(addr.ToInt64() + (s.Length + 1) * 2);
                }
            }
            return result;
        }

        public static void WriteStringAnsi(string str, IntPtr addr, int length) {
            if (String.IsNullOrEmpty(str))
                Marshal.WriteIntPtr(addr, IntPtr.Zero);
            else {
                int strLen = str.Length;
                if (length > 0 && strLen >= length)
                    strLen = length - 1;
                int i = 0;
                Byte[] bytes = new Byte[strLen + 1];
                foreach (char ch in str.Substring(0, strLen)) {
                    bytes[i++] = Convert.ToByte(ch);
                }
                bytes[strLen] = 0;
                Marshal.Copy(bytes, 0, addr, strLen + 1);
            }
        }

        public static void WriteStringUni(string str, IntPtr addr, int length) {
            if (String.IsNullOrEmpty(str))
                Marshal.WriteIntPtr(addr, IntPtr.Zero);
            else {
                int strLen = str.Length;
                if (length > 0 && strLen >= length)
                    strLen = length - 1;
                Marshal.Copy((str + (Char)0).ToCharArray(0, strLen + 1), 0, addr, strLen + 1);
            }
        }

        #endregion Unmanaged String Methods
    }
}