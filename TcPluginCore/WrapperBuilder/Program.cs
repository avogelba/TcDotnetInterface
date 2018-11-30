using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginTools;

namespace OY.TotalCommander.WrapperBuilder {
    sealed class Program {
        #region Constants

        private const string PluginInterfacePropertyName = "Plugin";
        private static NameValueCollection appSettings = ConfigurationManager.AppSettings;

        // Error Messages
        private const string ErrorMsg0 = "Use only one argument for wrapper assembly in parameters (/wcx | /wdx | /wfx | /wlx | /w=<wrapperDLL>).";
        private const string ErrorMsg1 = "Wrapper assembly '{0}' is empty or does not exist!";
        private const string ErrorMsg2 = "Cannot locate IL Assembler ilasm.exe!";
        private const string ErrorMsg3 = "Cannot locate IL Disassembler ildasm.exe!";
        private const string ErrorMsg4 = "Wrapper assembly '{0}' contains more than one TC plugin interface: ({1}, {2})!";
        private const string ErrorMsg5 = "Wrapper assembly '{0}' contains no TC plugin interface!";
        private const string ErrorMsg6 = "No methods to export, check your wrapper assembly!";
        private const string ErrorMsg7 = "{0} plugin is not implemented in '{1}'";
        private const string ErrorMsg8 = "ilasm.exe has failed assembling generated source!";
        private const string ErrorMsg9 = "ildasm.exe has failed disassembling {0}!";
        private const string ErrorMsg10 = "Use only one processor architecture flag in parameters (/x32 | /x64), or skip them all to create both 32- and 64-bit plugins.";
        private const string ErrorMsg11 = "Plugin assembly '{0}' is empty or does not exist!";
//        private const string ErrorMsg12 = "Cannot locate ZIP Archiver '{0}' - add path to settings!";
        private const string WarningMsg1 = "Cannot locate Resource Compiler rc.exe.";
        private const string WarningMsg2 = "File '{0}' - Resource Compiler ERROR.";
        private const string WarningMsg3 = "    WARNING!!! Type '{0}' - mandatory methods not implemented : {1}";

        private static Dictionary<PluginType, string> pluginExtensions =
            new Dictionary<PluginType, string> { 
                { PluginType.Content    , "wdx" },
                { PluginType.FileSystem , "wfx" },
                { PluginType.Lister     , "wlx" },
                { PluginType.Packer     , "wcx" },
                { PluginType.QuickSearch, "dll" }
            };

        private static Dictionary<PluginType, string> pluginMethodPrefixes =
            new Dictionary<PluginType, string> { 
                { PluginType.Content    , "Content" },
                { PluginType.FileSystem , "Fs"      },
                { PluginType.Lister     , "List"    },
                { PluginType.Packer     , null      },
                { PluginType.QuickSearch, null     }
            };

        // Mandatory plugin methods, parts of .NET plugin interface.
        // They must be implemented in plugin assembly.
        private static Dictionary<PluginType, string[]> pluginMandatoryMethods =
            new Dictionary<PluginType, string[]> { 
                { PluginType.Content, 
                    new[] {
                        "GetSupportedField", 
                        "GetValue"
                    }
                },
                { PluginType.FileSystem, 
                    new[] {
                        "FindFirst", 
                        "FindNext"  
                    }
                },
                { PluginType.Lister, 
                    new[] {
                        "Load" 
                    }
                },
                { PluginType.Packer, 
                    new[] {
                        "OpenArchive",
                        "ReadHeader",
                        "ProcessFile",
                        "CloseArchive"
                    }
                },
                { PluginType.QuickSearch, 
                    new[] {
                        "MatchFile", 
                        "MatchGetSetOptions"
                    }
                },
            };

        // Optional plugin methods - can be omitted in plugin assembly.
        // We have to exclude their calls from wrapper because of TC plugin requirements: 
        // "(must NOT be implemented if unsupported!)" - from TC plugin help.)
        private static Dictionary<PluginType, string[]> pluginOptionalMethods =
            new Dictionary<PluginType, string[]> { 
                { PluginType.Content, 
                    new[] {
                        "StopGetValue", 
                        "GetDefaultSortOrder", 
                        "PluginUnloading", 
                        "GetSupportedFieldFlags", 
                        "SetValue", 
                        "GetDefaultView",
                        "EditValue",
                        "SendStateInformation",
                        "CompareFiles"
                    }
                },
                { PluginType.FileSystem, 
                    new[] {
                        "GetFile", 
                        "PutFile", 
                        "RenMovFile", 
                        "DeleteFile", 
                        "RemoveDir", 
                        "MkDir", 
                        "ExecuteFile", 
                        "SetAttr", 
                        "SetTime", 
                        "Disconnect", 
                        "ExtractCustomIcon", 
                        "GetPreviewBitmap", 
                        "GetLocalName"
                    }
                },
                { PluginType.Lister, 
                    new[] {
                        "LoadNext",
                        "CloseWindow",
                        "SearchText",
                        "SendCommand",
                        "Print",
                        "NotificationReceived",
                        "GetPreviewBitmap",
                        "SearchDialog"
                    }
                },
                { PluginType.Packer, 
                    new[] {
                        "PackFiles",
                        "DeleteFiles",
                        "ConfigurePacker",
                        "StartMemPack",
                        "PackToMem",
                        "DoneMemPack",
                        "CanYouHandleThisFile"
                    }
                },
                { PluginType.QuickSearch, 
                    new string[] { }
                }
            };

        // Other plugin methods:
        //   1. TC plugin methods implemented in plugin wrapper only and are not parts of .NET plugin interface, or
        //   2. Methods implemented in parent plugin class and can be omitted in plugin assembly.
        // We DON'T have to exclude them from plugin wrapper.
        private static Dictionary<PluginType, string[]> pluginOtherMethods =
            new Dictionary<PluginType, string[]> {
                { PluginType.Content,
                    new[] {
                        "GetDetectString" 
                        //"SetDefaultParams" - commented to prevent excluding "FsSetDefaultParams" method for FS plugin without Content features
                    }
                },
                { PluginType.FileSystem, 
                    new[] {
                        "Init",
                        "FindClose", 
                        "SetCryptCallback",
                        "GetDefRootName", 
                        "SetDefaultParams",
                        "GetBackgroundFlags",
                        "LinksToLocalFiles", 
                        "StatusInfo"
                    }
                },
                { PluginType.Lister, 
                    new[] {
                        "GetDetectString",
                        "SetDefaultParams"
                    }
                },
                { PluginType.Packer, 
                    new[] {
                        "SetChangeVolProc",
                        "SetProcessDataProc",
                        "GetPackerCaps",
                        "PackSetDefaultParams",
                        "PkSetCryptCallback",
                        "GetBackgroundFlags"
                    }
                },
                { PluginType.QuickSearch, 
                    new string[] { }
                }
            };

        private static string[] resourceTemplate = {
            "1 VERSIONINFO",
            "FILEVERSION {FileVersion}",
            "PRODUCTVERSION {FileVersion}",
            "FILEOS 0x4",
            "FILETYPE 0x2",
            "{",
            "BLOCK \"StringFileInfo\"",
            "{",
            "  BLOCK \"000004b0\"",
            "  {",
            "{Values}",
            "  }",
            "}",
            "BLOCK \"VarFileInfo\"",
            "{",
            "  VALUE \"Translation\", 0x0000 0x04B0",
            "}",
            "}",
            "",
            "ICON_1 ICON \"{IconFile}\""
        };

        private static string[] configTemplate = {
            "<?xml version=\"1.0\" encoding=\"utf-8\" ?>",
            "<configuration>",
            "  <appSettings>",
            "    <add key=\"pluginAssembly\" value=\"{PluginAssembly}\"/>",
            "    <!-- add key=\"pluginTitle\" value=\"???\"/> -->",
            "    <add key=\"writeStatusInfo\" value=\"true\"/>",
            "    <add key=\"writeTrace\" value=\"true\"/>",
            "",
            "    <!-- write your plugin settings here -->",
            "  </appSettings>",
            "</configuration>"
        };

        private const string DefaultDescription = "...Put your description here...";
        private static string[] pluginstTemplate = {
            "[plugininstall]",
            "description={Description}",
            "type={PluginType}",
            "file={PluginFile}",
            "defaultdir={DefaultDir}",
            "defaultextension=???"
        };

        private static string[] usageInfo = {
            "Builds wrapper DLL(s) for Total Commander plugin written in .NET",
            "",
            "Usage: ",
            "  WrapperBuilder (/wcx | /wdx | /wfx | /wlx | /w=<wrapperDLL>) ",
            "                 /p=<pluginDLL> [/c=<contentDLL>] [/o=<outputName>] ",
            "                 [/i=<iconFile> | /ipa] [/v] [/release] ([/x32] | [/x64])",
            "                 [/a=<assemblerPath>] [/d=<disassemblerPath>] [/r=<rcPath>]",
            "",
            "  /wcx /wdx /wfx /wlx /qs  Use one of standard plugin wrapper templates",
            "                           located in the program folder: ",
            "                             /wcx - WcxWrapper.dll for Packer plugin, or",
            "                             /wdx - WdxWrapper.dll for Content plugin, or",
            "                             /wfx - WfxWrapper.dll for File System plugin, or",
            "                             /wlx - WlxWrapper.dll for Lister plugin, or",
            "                             /qs  - QSWrapper.dll  for QuickSearch plugin",
            "  /w=<wrapperDLL>        Use your own wrapper template assembly.",
            "  /p=<pluginDLL>         Assembly implementing TC plugin interface.",
            "                         If some plugin interface function is not implemented", 
            "                         here, it is excluded from wrapper.",
            "  /c=<contentDLL>        Assembly implementing TC Content interface.",
            "                         Used with File System wrapper only, if FS and Content", 
            "                         interfaces are implemented in separate DLLs.",
            "  /o=<outputName>        Output wrapper file name (no path, no extension).", 
            "                         If value is empty, plugin assembly file name is used.", 
            "  /i=<iconFile>          Adds icon to wrapper assembly from <icon File>.",
            "  /ipa                   Adds icon to wrapper assembly extracting it from the",
            "                         plugin assembly.",
            "                         /i or /ipa flags are used for FS wrapper only.",
            "  /release               Adds optimizaton to output assembly.",
            "                         (Equals to ilasm.exe '/optimize' key)",
            "  /x32                   Creates only 32-bit (PE32) plugin wrapper.",
            "  /x64                   Creates only 64-bit (PE32+) plugin wrapper",
            "                         for 64-bit AMD processor as the target processor.",
            "                         (Equals to ilasm.exe '/pe64 /x64' key set)",
            "         If both /x32 and /x64 flags are skipped, ",
            "         both 32- and 64-bit plugin wrappers will be created.",
            "  /v                     Verbose mode; outputs log to console.",
            "  /a=<assemblerPath>     Specifies path to IL Assembler (ilasm.exe).",
            "                         If not set, path loaded from configuration file",
            "                         WrapperBuilder.exe.config, key='assemblerPath'.", 
            "  /d=<disassemblerPath>  Specifies path to IL Disassembler (ildasm.exe).",
            "                         If not set, path loaded from configuration file",
            "                         WrapperBuilder.exe.config, key='disassemblerPath'.", 
            "  /r=<rcPath>            Specifies path to Resource Compiler (rc.exe).",
            "                         If not set, path loaded from configuration file",
            "                         WrapperBuilder.exe.config, key='rcPath'.",
            "  /z=<zipPath>           Specifies path to ZIP archiver (usually zip.exe).",
            "                         If not set, path loaded from configuration file",
            "                         WrapperBuilder.exe.config, key='zipArchiver'.",
            "                         Is used to create Installation ZIP archive for plugin.",
            "                         If finally not set, Inst. Archive will not be created."
        };

        private const string ZipArchiverDefault = @"C:\TotalCmd\Arc\zip\zip.exe";

        #endregion Constants

        #region Variables

        static readonly string AppFolder =
            Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        static string dllExportAttributeTypeName = typeof(DllExportAttribute).FullName;
        static List<string> ilasmArgs = new List<string>();
        static string workDir = GetWorkingDirectory();

        static bool clearWorkDir = false;
        static bool createConfiguration = true;
        //static bool createPluginst = false;
        static bool createInstZip = true;

        static string assemblerPath;
        static string contentAssemblyFile;
        static string disassemblerPath;
        static string rcPath;
        static string iconFileName;
        static bool iconFromPluginAssembly;
        static string outputWrapperFolder;
        static string outputWrapperName;
        static string pluginAssemblyFile;
        static PluginType pluginType = PluginType.Unknown;
        static bool verbose;
        static string wrapperAssemblyFile;
        static string wrapperAssemblyVersion;
        static bool x32Flag = true;
        static bool x64Flag = true;
        static bool pause;

        #endregion Variables

        #region Main

        static void Main(string[] args) {
            try {
                ParseArgs(args);
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
                WriteLine(String.Format("Wrapper Assembly '{0}'", wrapperAssemblyFile));
                List<string> exportedMethods = LoadExportedList(wrapperAssemblyFile);
                WriteLine(String.Format("Plugin type : {0}", TcUtils.PluginNames[pluginType]));
                if (exportedMethods.Count <= 0)
                    throw new Exception(ErrorMsg6);
                WriteLine(String.Format("- {0} Methods to export", exportedMethods.Count));
                SetOutputNames();
                List<string> excludedMethods = new List<string>();
                if (!String.IsNullOrEmpty(pluginAssemblyFile)) {
                    excludedMethods = LoadExcludedList();
                    if (pluginType.Equals(PluginType.FileSystem)) {
                        if (!String.IsNullOrEmpty(iconFileName) && File.Exists(iconFileName)) {
                            WriteLine("Plugin Icon added: " + iconFileName);
                            iconFileName = iconFileName.Replace("\\", "\\\\");
                        } else if (iconFromPluginAssembly)
                            iconFileName = GetTmpIconFileName(pluginAssemblyFile);
                    }
                }
                string sourcePath = Disassemble();
                WriteLine(String.Format("Disassembled to '{0}'", sourcePath));
                string sourceOutPath = workDir + @"\output.il";
                ProcessSource(sourcePath, sourceOutPath, exportedMethods, excludedMethods);
                WriteLine(String.Format("Processed to '{0}'", sourceOutPath));

                if (x32Flag)
                    Assemble(false);
                if (x64Flag)
                    Assemble(true);

                if (!outputWrapperFolder.Equals(Path.GetDirectoryName(pluginAssemblyFile))) {
                    File.Copy(pluginAssemblyFile, 
                        Path.Combine(outputWrapperFolder, Path.GetFileName(pluginAssemblyFile)), true);
                }

                if (createInstZip)
                    CreateInstallationZip();
                if (clearWorkDir)
                    Directory.Delete(workDir, true);
                if (pause)
                    Console.Read();
            } catch (Exception ex) {
                WriteLine("ERROR: " + ex.Message, false);
                if (pause)
                    Console.Read();
                Environment.Exit(1);
            }
        }

        #endregion Main

        #region Private Methods

        private static string Assemble(bool x64) {
            string outputFileName = outputWrapperName;
            string outputWrapperExt = "." + pluginExtensions[pluginType];
            string outPath = String.Format(@"{0}\{1}", workDir, Path.GetFileName(wrapperAssemblyFile));
            string resourcePath = String.Format(@"{0}\{1}", workDir, "input.res");
            StringBuilder args = new StringBuilder();
            args.AppendFormat(@"""{0}\output.il"" /out:""{1}""", workDir, outPath);
            if (Path.GetExtension(wrapperAssemblyFile) == ".dll")
                args.Append(" /dll");
            if (File.Exists(resourcePath))
                args.AppendFormat(@" /res:""{0}""", resourcePath);
            if (x64) {
                ilasmArgs.Add("/x64");
                ilasmArgs.Add("/PE64");
                if (pluginType == PluginType.QuickSearch)
                    outputFileName += "64";
                else
                    outputWrapperExt += "64";
                WriteLine("\n64-bit plugin wrapper\n=====================");
            } else {
                WriteLine("\n32-bit plugin wrapper\n=====================");
            }
            if (ilasmArgs.Count > 0)
                args.Append(" ").Append(String.Join(" ", ilasmArgs.ToArray()));

            ProcessStartInfo startInfo = 
                new ProcessStartInfo(assemblerPath, args.ToString()) { 
                    WindowStyle = ProcessWindowStyle.Hidden 
                };
            Process process = Process.Start(startInfo);
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new Exception(ErrorMsg8);

            WriteLine(String.Format("  Assembled to '{0}'", outPath));
            string newPath = //Path.Combine(outputWrapperFolder, outputWrapperName + outputWrapperExt);
                outputFileName + outputWrapperExt;
            File.Delete(newPath);
            File.Copy(outPath, newPath, true);
            WriteLine(String.Format("  Wrapper assembly moved to '{0}'", newPath));
            if (createConfiguration)
                CreateConfigFile(newPath);
            
            return outPath;
        }

        private static string Disassemble() {
            string sourcePath = String.Format(@"{0}\input.il", workDir);
            string args = String.Format(@"""{0}"" /out:""{1}""", wrapperAssemblyFile, sourcePath);
            ProcessStartInfo startInfo = 
                new ProcessStartInfo(disassemblerPath, args) { 
                    WindowStyle = ProcessWindowStyle.Hidden 
                };
            Process process = Process.Start(startInfo);
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new Exception(String.Format(ErrorMsg9, wrapperAssemblyFile));
            try {
                string rcFile = rcPath ?? Path.Combine(Path.GetDirectoryName(disassemblerPath), "rc.exe");
                if (String.IsNullOrEmpty(rcFile) || !File.Exists(rcFile))
                    throw new Exception(WarningMsg1);
                string resFileName = workDir + "\\input.rc";
                FillResourceFile(resFileName);
                args = "/v " + resFileName;
                startInfo =
                    new ProcessStartInfo(rcFile, args) {
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                process = Process.Start(startInfo);
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new Exception(String.Format(WarningMsg2, resFileName));
            } catch (Exception ex) {
                WriteLine("WARNING: " + ex.Message, false);
            }
            return sourcePath;
        }

        private static void FillResourceFile(string resFileName) {
            string fileVersion = null;
            string productVersion = null;
            FileVersionInfo version = FileVersionInfo.GetVersionInfo(pluginAssemblyFile);  // wrapperAssemblyFile);
            StringBuilder versionInfo = new StringBuilder(500);
            if (!String.IsNullOrEmpty(version.Comments))
                versionInfo.AppendFormat("    VALUE \"Comments\", \"{0}\"\n", version.Comments);
            if (!String.IsNullOrEmpty(version.CompanyName))
                versionInfo.AppendFormat("    VALUE \"CompanyName\", \"{0}\"\n", version.CompanyName);
            if (!String.IsNullOrEmpty(version.FileDescription))
                versionInfo.AppendFormat("    VALUE \"FileDescription\", \"{0}\"\n", version.FileDescription);
            //if (!String.IsNullOrEmpty(version.FileName))
            //    versionInfo.AppendFormat("    VALUE \"FileName\", \"{0}\"\n", version.FileName);
            if (!String.IsNullOrEmpty(version.FileVersion)) {
                versionInfo.AppendFormat("    VALUE \"FileVersion\", \"{0}\"\n", version.FileVersion);
                fileVersion = version.FileVersion.Replace('.', ',');
            }
            if (!String.IsNullOrEmpty(version.InternalName))
                versionInfo.AppendFormat("    VALUE \"InternalName\", \"{0}\"\n", version.InternalName);
            if (!String.IsNullOrEmpty(version.LegalCopyright))
                versionInfo.AppendFormat("    VALUE \"LegalCopyright\", \"{0}\"\n", version.LegalCopyright);
            if (!String.IsNullOrEmpty(version.LegalTrademarks))
                versionInfo.AppendFormat("    VALUE \"LegalTrademarks\", \"{0}\"\n", version.LegalTrademarks);
            if (!String.IsNullOrEmpty(version.OriginalFilename))
                versionInfo.AppendFormat("    VALUE \"OriginalFilename\", \"{0}\"\n", version.OriginalFilename);
            if (!String.IsNullOrEmpty(version.ProductName))
                versionInfo.AppendFormat("    VALUE \"ProductName\", \"{0}\"\n", version.ProductName);
            if (!String.IsNullOrEmpty(version.ProductVersion)) {
                versionInfo.AppendFormat("    VALUE \"ProductVersion\", \"{0}\"\n", version.ProductVersion);
                productVersion = version.ProductVersion.Replace('.', ',');
            }
            versionInfo.AppendFormat("    VALUE \"Assembly Version\", \"{0}\"\n", wrapperAssemblyVersion);
            using (StreamWriter output = new StreamWriter(resFileName, false, Encoding.Default)) {
                foreach (string str in resourceTemplate) {
                    string outputStr = str
                        .Replace("{FileVersion}", fileVersion)
                        .Replace("{ProductVersion}", productVersion)
                        .Replace("{Values}", versionInfo.ToString());
                    if (str.Contains("{IconFile}")) {
                        if (String.IsNullOrEmpty(iconFileName))
                            continue;
                        else {
                            // Add icon to resource file for wrapper
                            outputStr = outputStr.Replace("{IconFile}", iconFileName);
                        }
                    }
                    output.WriteLine(outputStr);

                }
            }
        }

        private static void CreateConfigFile(string wrapperFileName) {
            string configFileName = wrapperFileName + ".config";
            if (File.Exists(configFileName))
                return;
            using (StreamWriter output = new StreamWriter(configFileName, false, Encoding.Default)) {
                foreach (string str in configTemplate) {
                    if (str.Contains("\"writeStatusInfo\"") && !pluginType.Equals(PluginType.FileSystem))
                        continue;
                    output.WriteLine(str
                        .Replace("{PluginAssembly}", Path.GetFileName(pluginAssemblyFile)));
                }
            }
        }

        private static void CreatePluginstFile(string iniFileName) {
            if (File.Exists(iniFileName))
                return;
            FileVersionInfo version = FileVersionInfo.GetVersionInfo(pluginAssemblyFile);
            string description = version.Comments ?? DefaultDescription;
            string wrapperName = Path.GetFileNameWithoutExtension(outputWrapperName);
            using (StreamWriter output = new StreamWriter(iniFileName, false, Encoding.Default)) {
                foreach (string str in pluginstTemplate) {
                    if (str.StartsWith("defaultextension") && !pluginType.Equals(PluginType.Packer))
                        continue;
                    output.WriteLine(str
                        .Replace("{PluginType}", pluginExtensions[pluginType])
                        .Replace("{PluginFile}", wrapperName + "." + pluginExtensions[pluginType])
                        .Replace("{Description}", description)
                        .Replace("{DefaultDir}", "dotNet_" + wrapperName));
                }
            }
        }

        private static void CreateInstallationZip() {
            if (pluginType == PluginType.QuickSearch)
                return;
            WriteLine("\nInstallation archive\n====================");

            string zipArchiver = null;
            if (appSettings != null)
                zipArchiver = appSettings["zipArchiver"];
            if (String.IsNullOrEmpty(zipArchiver)) {
                zipArchiver = ZipArchiverDefault;
            }
            if (!File.Exists(zipArchiver)) {
                WriteLine("ZIP Archiver is not found - Installation Archive is not created.");
                return;
//                throw new Exception(String.Format(ErrorMsg12, zipArchiver));
            }
            string iniFile = Path.Combine(outputWrapperFolder, "pluginst.inf");
            CreatePluginstFile(iniFile);  //   ???
            if (File.Exists(iniFile)) {
                string outFile = outputWrapperName;
                string wrapperFile = outFile + "." + pluginExtensions[pluginType];
                string args = "-j -q " + outFile + ".zip";
                if (x32Flag) {
                    args += " " + wrapperFile;
                    if (File.Exists(wrapperFile + ".config"))
                        args += " " + wrapperFile + ".config";
                }
                if (x64Flag) {
                    wrapperFile += "64";
                    args += " " + wrapperFile;
                    if (File.Exists(wrapperFile + ".config"))
                        args += " " + wrapperFile + ".config";
                }
                args += " " + pluginAssemblyFile;
                args += " " + iniFile;

                ProcessStartInfo startInfo = new ProcessStartInfo(zipArchiver, args) {
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process process = Process.Start(startInfo);
                process.WaitForExit();
                if (process.ExitCode != 0)
                    WriteLine("  ERROR archive creating !!!");
                else {
                    WriteLine(String.Format("  Archive created: '{0}.zip'", outFile));
                    File.Delete(iniFile);
                }
            }
        }

        private static List<string> GetExportedMethods(Assembly assembly) {
            List<string> methods = new List<string>();
            foreach (Type type in assembly.GetTypes()) {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)) {
                    string methodName = method.Name;
                    foreach (CustomAttributeData attr in CustomAttributeData.GetCustomAttributes(method)) {
                        if (attr.Constructor.DeclaringType != null 
                                && attr.Constructor.DeclaringType.FullName.Equals(dllExportAttributeTypeName)) {
                            if (attr.NamedArguments != null) {
                                foreach (CustomAttributeNamedArgument arg in attr.NamedArguments) {
                                    if (arg.MemberInfo.Name.Equals("EntryPoint"))
                                        methodName = (string)arg.TypedValue.Value;
                                }
                            }
                            methods.Add(methodName);
                        }
                    }
                }
            }
            return methods;
        }

        private static string GetTmpIconFileName(string pluginAssembly) {
            // Try to extract icon from plugin assembly to temporary file
            string iconFile = "icon_tmp.ico";
            try {
                Icon icon = Icon.ExtractAssociatedIcon(pluginAssembly);
                if (icon != null) {
                    using (Stream s = new FileStream(Path.Combine(workDir, iconFile), FileMode.Create)) {
                        icon.Save(s);
                    }
                    WriteLine("Plugin Icon extracted from Plugin Assembly.");
                }
            } catch (Exception ex) {
                WriteLine("ICON EXTRACT ERROR: " + ex.Message, false);
                iconFile = null;
            }
            return iconFile;
        }

        private static string GetWorkingDirectory() {
            string path = Environment.ExpandEnvironmentVariables(@"%TEMP%\WrapperBuilder");
            DirectoryInfo directory = new DirectoryInfo(path);
            if (!directory.Exists) {
                directory.Create();
            }
            return directory.FullName;
        }

        private static List<string> LoadExcludedList() {
            List<string> list = new List<string>();
            AppDomain domain = AppDomain.CreateDomain(Guid.NewGuid().ToString());
            domain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
            domain.SetData("pluginDll", pluginAssemblyFile);
            domain.SetData("pluginType", pluginType);
            WriteLine("Excluded methods:");
            WriteLine(String.Format("  Plugin Assembly '{0}'", pluginAssemblyFile));
            domain.DoCallBack(LoadExcludedMethods);

            List<string> pList = (List<string>)domain.GetData("methods");
            foreach (string method in pList) {
                WriteLine("   - " + method);
            }
            list.AddRange(pList);
            if (pluginType.Equals(PluginType.FileSystem)) {
                pList.Clear();
                WriteLine(String.Format(
                    (String.IsNullOrEmpty(contentAssemblyFile) ? "  No Content Assembly, try " : "  Content Assembly ") + "'{0}'",
                    contentAssemblyFile ?? pluginAssemblyFile));
                // System plugins can contain some methods of content plugin
                domain.SetData("pluginDll", contentAssemblyFile ?? pluginAssemblyFile);
                domain.SetData("pluginType", PluginType.Content);
                try {
                    domain.DoCallBack(LoadExcludedMethods);
                    pList = (List<string>)domain.GetData("methods");
                    foreach (string method in pList) {
                        WriteLine("   - " + method);
                    }
                    list.AddRange(pList);
                } catch (PluginNotImplementedException) {
                    WriteLine("  -- Content interface is NOT implemented, exclude all Content methods:");
                    pList.AddRange(pluginMandatoryMethods[PluginType.Content]);
                    pList.AddRange(pluginOptionalMethods[PluginType.Content]);
                    pList.AddRange(pluginOtherMethods[PluginType.Content]);
                    foreach (string method in pList) {
                        WriteLine("   - " + method);
                    }
                    list.AddRange(pList);
                }
            }
            AppDomain.Unload(domain);
            return list;
        }

        private static void LoadExcludedMethods() {
            string assemblyPath = (string)AppDomain.CurrentDomain.GetData("pluginDll");
            pluginType = (PluginType)AppDomain.CurrentDomain.GetData("pluginType");
            List<string> methods = GetExcludedMethods(assemblyPath, pluginType);
            AppDomain.CurrentDomain.SetData("methods", methods);
        }

        private static List<string> GetExcludedMethods(string assemblyPath, PluginType pType) {
            if (String.IsNullOrEmpty(assemblyPath) 
                    || !File.Exists(assemblyPath) 
                    || pType.Equals(PluginType.Unknown))
                return new List<string>();
            bool assemblyOk = false;

            List<string> exclMethods = new List<string>(pluginOptionalMethods[pType]);
            Assembly assembly = TcPluginLoader.AssemblyReflectionOnlyLoadFrom(assemblyPath);
            foreach (Type type in assembly.GetExportedTypes()) {
                if (type.GetInterface(TcUtils.PluginInterfaces[pType]) != null) {
                    string methodsMissed = String.Empty;
                    List<string> typeMethods = new List<string>();
                    BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                    foreach (MethodInfo method in type.GetMethods(bindingFlags)) {
                        typeMethods.Add(method.Name);
                    }
                    // Check if all mandatory methods are implemented in the type
                    foreach (string method in pluginMandatoryMethods[pType]) {
                        if (!typeMethods.Contains(method))
                            methodsMissed += method + ",";
                    }
                    if (methodsMissed.Length == 0) {
                        // all mandatory methods are implemented
                        assemblyOk = true;
                        foreach (string method in pluginOptionalMethods[pType]) {
                            if (typeMethods.Contains(method))
                                exclMethods.Remove(method);
                            else {
                                foreach (string substMethod in SubstituteMethods(method, pType)) {
                                    if (typeMethods.Contains(substMethod)) {
                                        exclMethods.Remove(method);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (!assemblyOk)
                throw new PluginNotImplementedException(
                    String.Format(ErrorMsg7, TcUtils.PluginNames[pType], assemblyPath));

            return exclMethods;
        }

        private static IEnumerable<string> SubstituteMethods(string method, PluginType pType) {
            if (pType.Equals(PluginType.FileSystem) && method.Equals("ExecuteFile")) {
                return new[] { "ExecuteOpen", "ExecuteProperties", "ExecuteCommand" };
            }
            return new string[0];
        }

        private static List<string> LoadExportedList(string assemblyPath) {
            AppDomain domain = AppDomain.CreateDomain("Exported");
            domain.ReflectionOnlyAssemblyResolve += ReflectionOnlyAssemblyResolve;
            domain.SetData("assemblyPath", assemblyPath);
            domain.DoCallBack(LoadExportedMethods);
            List<string> list = (List<string>)domain.GetData("methods");
            wrapperAssemblyVersion = (string)domain.GetData("assemblyVersion");
            pluginType = (PluginType)domain.GetData("pluginType");
            AppDomain.Unload(domain);
            return list;
        }

        private static void LoadExportedMethods() {
            PluginType plugType = PluginType.Unknown;
            string assemblyFile = (string)AppDomain.CurrentDomain.GetData("assemblyPath");
            Assembly assembly = TcPluginLoader.AssemblyReflectionOnlyLoadFrom(assemblyFile);
            string assemblyVersion = assembly.GetName().Version.ToString();
            foreach (Type type in assembly.GetTypes()) {
                PropertyInfo pi = type.GetProperty(PluginInterfacePropertyName,
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (pi != null) {
                    PluginType pType = GetPluginType(pi.PropertyType);
                    if (!pType.Equals(PluginType.Unknown)) {
                        if (plugType.Equals(PluginType.Unknown))
                            plugType = pType;
                        else {
                            throw new Exception(
                                String.Format(ErrorMsg4, assemblyFile, pluginExtensions[pType], pluginExtensions[plugType]));
                        }
                    }
                }
            }
            if (plugType.Equals(PluginType.Unknown)) {
                throw new Exception(String.Format(ErrorMsg5, assemblyFile));
            }
            List<string> methods = GetExportedMethods(assembly);
            AppDomain.CurrentDomain.SetData("methods", methods);
            AppDomain.CurrentDomain.SetData("assemblyVersion", assemblyVersion);
            AppDomain.CurrentDomain.SetData("pluginType", plugType);
        }

        private static PluginType GetPluginType(Type type) {
            foreach (PluginType pType in TcUtils.PluginInterfaces.Keys) {
                if (type.GetInterface(TcUtils.PluginInterfaces[pType]) != null)
                    return pType;
            }
            return PluginType.Unknown;
        }

        private static void ParseArgs(string[] args) {
            //TODO : parameter to create simple config file
            bool showHelp = (args.Length == 0);
            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
                if (arg.Equals("/?")) {
                    showHelp = true;
                    break;
                }
                if (arg.Equals("/wcx")) {
                    if (String.IsNullOrEmpty(wrapperAssemblyFile))
                        wrapperAssemblyFile = Path.Combine(AppFolder, "WcxWrapper.dll");
                    else
                        throw new Exception(ErrorMsg0);
                } else if (arg.Equals("/wdx")) {
                    if (String.IsNullOrEmpty(wrapperAssemblyFile))
                        wrapperAssemblyFile = Path.Combine(AppFolder, "WdxWrapper.dll");
                    else
                        throw new Exception(ErrorMsg0);
                } else if (arg.Equals("/wfx")) {
                    if (String.IsNullOrEmpty(wrapperAssemblyFile))
                        wrapperAssemblyFile = Path.Combine(AppFolder, "WfxWrapper.dll");
                    else
                        throw new Exception(ErrorMsg0);
                } else if (arg.Equals("/wlx")) {
                    if (String.IsNullOrEmpty(wrapperAssemblyFile))
                        wrapperAssemblyFile = Path.Combine(AppFolder, "WlxWrapper.dll");
                    else
                        throw new Exception(ErrorMsg0);
                } else if (arg.Equals("/qs")) {
                    if (String.IsNullOrEmpty(wrapperAssemblyFile))
                        wrapperAssemblyFile = Path.Combine(AppFolder, "QSWrapper.dll");
                    else
                        throw new Exception(ErrorMsg0);
                } else if (arg.StartsWith("/w=")) {
                    if (String.IsNullOrEmpty(wrapperAssemblyFile))
                        wrapperAssemblyFile = arg.Substring(3);
                    else
                        throw new Exception(ErrorMsg0);
                } else if (arg.StartsWith("/p=")) {
                    pluginAssemblyFile = arg.Substring(3);
                } else if (arg.StartsWith("/c=")) {
                    contentAssemblyFile = arg.Substring(3);
                } else if (arg.StartsWith("/o=")) {
                    outputWrapperName = arg.Substring(3);
                } else if (arg.StartsWith("/a=")) {
                    assemblerPath = arg.Substring(3);
                } else if (arg.StartsWith("/d=")) {
                    disassemblerPath = arg.Substring(3);
                } else if (arg.StartsWith("/r=")) {
                    rcPath = arg.Substring(3);
                } else if (arg.StartsWith("/i=")) {
                    iconFileName = arg.Substring(3);
                } else if (arg.Equals("/ipa")) {
                    iconFromPluginAssembly = true;
                } else if (arg.Equals("/v")) {
                    verbose = true;
                } else if (arg.Equals("/release")) {
                    ilasmArgs.Add("/optimize");
                } else if (arg.Equals("/x32")) {
                    x64Flag = false;
                } else if (arg.Equals("/x64")) {
                    x32Flag = false;
                } else if (arg.Equals("/pause")) {
                    pause = true;
                }
            }
            if (showHelp) {
                foreach (string str in usageInfo) {
                    Console.WriteLine(str);
                }
                Environment.Exit(1);
            }
            if (String.IsNullOrEmpty(wrapperAssemblyFile) || !File.Exists(wrapperAssemblyFile)) {
                throw new Exception(String.Format(ErrorMsg1, wrapperAssemblyFile));
            }
            if (String.IsNullOrEmpty(pluginAssemblyFile) || !File.Exists(pluginAssemblyFile)) {
                throw new Exception(String.Format(ErrorMsg11, pluginAssemblyFile));
            }
            if (String.IsNullOrEmpty(assemblerPath)) {
                if (appSettings != null)
                    assemblerPath = appSettings["assemblerPath"];
            }

            if (String.IsNullOrEmpty(disassemblerPath)) {
                if (appSettings != null)
                    disassemblerPath = appSettings["disassemblerPath"];
            }
            if (String.IsNullOrEmpty(rcPath)) {
                if (appSettings != null)
                    rcPath = appSettings["rcPath"];
            }
            if (String.IsNullOrEmpty(assemblerPath) || !File.Exists(assemblerPath)) {
                throw new Exception(ErrorMsg2);
            }
            if (String.IsNullOrEmpty(disassemblerPath) || !File.Exists(disassemblerPath)) {
                throw new Exception(ErrorMsg3);
            }
            if (!x32Flag && !x64Flag ) {
                throw new Exception(ErrorMsg10);
            }

            WriteLine(String.Format("IL Disassembler: '{0}'", disassemblerPath));
            WriteLine(String.Format("IL Assembler   : '{0}'", assemblerPath));
        }

        private static void SetOutputNames() {
            if (String.IsNullOrEmpty(outputWrapperName)) {
                outputWrapperFolder = Path.GetDirectoryName(pluginAssemblyFile);
                if (pluginType == PluginType.QuickSearch)
                    outputWrapperName = "tcmatch";
                else
                    outputWrapperName = Path.GetFileNameWithoutExtension(pluginAssemblyFile);
            } else {
                outputWrapperFolder = Path.GetDirectoryName(outputWrapperName);
                if (!Path.IsPathRooted(outputWrapperFolder))
                    outputWrapperFolder = Path.Combine(
                        Path.GetDirectoryName(pluginAssemblyFile), outputWrapperFolder);
                outputWrapperName = Path.GetFileNameWithoutExtension(outputWrapperName);
            }
            if (!Directory.Exists(outputWrapperFolder)) {
                Directory.CreateDirectory(outputWrapperFolder);
            }
            outputWrapperName = Path.Combine(outputWrapperFolder, outputWrapperName);
        }

        const string DllExportAttributeStr =
            ".custom instance void [TcPluginInterface]OY.TotalCommander.TcPluginInterface.DllExportAttribute";

        private static void ProcessSource(string sourcePath, string outPath,
                List<string> exportedMethods, List<string> excludedMethods) {
            string prefix = pluginMethodPrefixes[pluginType] ?? String.Empty;
            string cntPrefix = ((pluginType.Equals(PluginType.FileSystem)) ?
                pluginMethodPrefixes[PluginType.Content] : null) 
                ?? String.Empty;
            foreach (string method in excludedMethods) {
                string exclMethod = prefix + method;
                string cntExclMethod = String.IsNullOrEmpty(cntPrefix) ? null : prefix + cntPrefix + method;
                if (exportedMethods.Contains(exclMethod)) {
                    exportedMethods.Remove(exclMethod);
                    // Check if Unicode method exists 
                    if (exportedMethods.Contains(exclMethod + "W")) {
                        exportedMethods.Remove(exclMethod + "W");
                    }
                } else if (!String.IsNullOrEmpty(cntExclMethod) && exportedMethods.Contains(cntExclMethod)) {
                    exportedMethods.Remove(cntExclMethod);
                    // Check if Unicode method exists 
                    if (exportedMethods.Contains(cntExclMethod + "W")) {
                        exportedMethods.Remove(cntExclMethod + "W");
                    }
                } else
                    WriteLine(String.Format("  Excluded method '{0}' is not in exported list.", method));
            }
            using (StreamWriter output = new StreamWriter(outPath, false, Encoding.Default)) {
                int methodIndex = 0;
                int skipLines = 0;
                int openBraces = 0;
                bool isMethodStatic = false;
                bool isMethodExcluded = false;
                string methodName = "<NONE>";
                List<string> methodHeaders = new List<string>();
                foreach (string srcLine in File.ReadAllLines(sourcePath, Encoding.Default)) {
                    if (skipLines > 0) {
                        skipLines--;
                        continue;
                    }
                    string line = srcLine.TrimStart(' ');
                    if (line.StartsWith(".method")) {
                        isMethodStatic = line.Contains(" static ");
                        methodName = "<UNKNOWN>";
                        methodHeaders.Clear();
                    }
                    if (methodName.Equals("<UNKNOWN>")) {
                        int pos = srcLine.IndexOf('(');
                        if (pos > 0) {
                            int pos1 = srcLine.LastIndexOf(' ', pos);
                            if (pos1 < 0)
                                pos1 = 0;
                            string mName = srcLine.Substring(pos1 + 1, pos - pos1 - 1).Trim();
                            if (!mName.Equals("marshal"))
                                methodName = mName;
                        }
                        if (methodName.Equals("<UNKNOWN>"))
                            methodHeaders.Add(srcLine);
                        else {
                            isMethodExcluded = excludedMethods.Contains(methodName);
                            if (!isMethodExcluded && methodName.EndsWith("W")) {
                                isMethodExcluded =
                                    excludedMethods.Contains(methodName.Substring(0, methodName.Length - 1));
                            }
                            if (!isMethodExcluded && methodHeaders.Count > 0) {
                                foreach (string s in methodHeaders) {
                                    output.WriteLine(s);
                                }
                                methodHeaders.Clear();
                            }
                        }
                    }
                    if (!isMethodExcluded && line.StartsWith(DllExportAttributeStr)) {
                        foreach (char ch in line) {
                            if (ch == '(')
                                openBraces++;
                            if (ch == ')')
                                openBraces--;
                        }
                        if (isMethodStatic) {
                            output.WriteLine(".export [{0}] as {1}", methodIndex + 1, exportedMethods[methodIndex]);
                            methodIndex++;
                        }
                        continue;
                    }
                    if (!isMethodExcluded && openBraces > 0) {
                        foreach (char ch in line) {
                            if (ch == '(')
                                openBraces++;
                            if (ch == ')')
                                openBraces--;
                        }
                        continue;
                    }
                    if (isMethodExcluded && line.StartsWith(@"} // end of method")) {
                        isMethodExcluded = false;
                        methodName = "<NONE>";
                        continue;
                    }
                    if (!isMethodExcluded && !methodName.Equals("<UNKNOWN>"))
                        output.WriteLine(srcLine);
                }
            }
        }

        private static Assembly ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args) {
            try {
                return Assembly.ReflectionOnlyLoad(args.Name);
            } catch (FileNotFoundException) {
                string mainAssemblyPath = (string)AppDomain.CurrentDomain.GetData("pluginDll");
                string assemblyName = args.Name;
                if (assemblyName.IndexOf(',') > 0) {
                    assemblyName = assemblyName.Substring(0, assemblyName.IndexOf(','));
                }
                if (String.IsNullOrEmpty(Path.GetExtension(assemblyName))) {
                    assemblyName += ".dll";
                }
                return Assembly.ReflectionOnlyLoadFrom(
                    Path.Combine(Path.GetDirectoryName(mainAssemblyPath), assemblyName));
            }
        }

        private static void WriteLine(string text) {
            WriteLine(text, true);
        }

        private static void WriteLine(string text, bool detailed) {
            if (verbose || !detailed)
                Console.WriteLine(text);
        }

        #endregion Private Methods
    }
}
