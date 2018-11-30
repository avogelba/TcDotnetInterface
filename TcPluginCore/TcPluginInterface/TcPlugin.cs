using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Lifetime;
using System.Security.Permissions;
using System.Threading;
#if TRACE
using System.Reflection;
using System.Text;
#endif

namespace OY.TotalCommander.TcPluginInterface {
    [Serializable]
    public class TcPlugin: MarshalByRefObject {
        #region Variables

        private static readonly string TcFolder =
            Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private int mainThreadId;

        #endregion Variables

        #region Properties

        public string DataBufferName { get; private set; }

        public AppDomain MainDomain { get; set; }

        public PluginDefaultParams DefaultParams { get; set; }

        public string DomainInfo {
            get {
#if TRACE
                StringBuilder sb = new StringBuilder();
                AppDomain domain = AppDomain.CurrentDomain;
                sb.Append("-----------");
                sb.AppendFormat("\nPlugin: {0}", Title);
                if (!domain.Equals(MainDomain)) {
                    sb.AppendFormat("\nApp Domain: {0}.{1}", domain.Id, domain.FriendlyName);
                    sb.Append("\n  Assemblies loaded:");
                    foreach (Assembly a in domain.GetAssemblies()) {
                        string aName = a.GetName().Name;
                        if (aName.Equals("<unknown>"))
                            sb.AppendFormat("\n\t{0}", aName);
                        else {
                            if (a.GlobalAssemblyCache)
                                sb.AppendFormat("\n\t{{ GAC }} {0}", Path.GetFileName(a.Location));
                            else if (String.IsNullOrEmpty(a.Location))
                                sb.AppendFormat("\n\t[{0}]", a.FullName);
                            else
                                sb.AppendFormat("\n\t{0} [ver. {1}]", a.Location, a.GetName().Version);
                        }
                    }
                }
                return sb.ToString();
#else
                return String.Empty;
#endif
            }
        }

        protected bool IsBackgroundThread {
            get { return Thread.CurrentThread.ManagedThreadId != mainThreadId; }
        }

        public TcPlugin MasterPlugin { get; set; }

        public PluginPassword Password { get; protected set; }

        protected string PluginFolder { get; private set; }

        public string PluginId { get; private set; }

        public int PluginNumber { get; set; }

        public StringDictionary Settings { get; private set; }

        public bool ShowErrorDialog { get; set; }

        public string Title { get; set; }

        public virtual string TraceTitle {
            get {
                return (MasterPlugin == null) ? Title : MasterPlugin.TraceTitle;
            }
        }

        public string WrapperFileName { get; set; }

        public bool WriteTrace { get; private set; }

        #endregion Properties

        #region Constructors

        public TcPlugin() {
            PluginInit(null);
        }

        public TcPlugin(StringDictionary pluginSettings) {
            PluginInit(pluginSettings);
        }

        private void PluginInit(StringDictionary pluginSettings) {
            PluginId = Guid.NewGuid().ToString();
            DataBufferName = Guid.NewGuid().ToString();
            PluginNumber = -1;
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            if (pluginSettings != null) {
                this.Settings = pluginSettings;
                PluginFolder = pluginSettings["pluginFolder"];
                Title = pluginSettings["pluginTitle"];
                ShowErrorDialog = !Convert.ToBoolean(pluginSettings["hideErrorDialog"]);
                WriteTrace = Convert.ToBoolean(pluginSettings["writeTrace"]);
            }
        }

        #endregion Constructors

        #region MarshalByRefObject - Lifetime initialization

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override Object InitializeLifetimeService() {
            ILease lease = (ILease)base.InitializeLifetimeService();
            if (lease != null && lease.CurrentState == LeaseState.Initial) {
                // By default we set infinite lifetime for each created plugin (initialLifeTime = 0)
                lease.InitialLeaseTime = TimeSpan.Zero;
            }
            return lease;
        }

        #endregion

        #region Plugin Event Handler

        public event EventHandler<PluginEventArgs> TcPluginEventHandler;

        public virtual int OnTcPluginEvent(PluginEventArgs e) {
            EventHandler<PluginEventArgs> handler = TcPluginEventHandler;
            if (handler != null) {
                handler(this, e);
                return e.Result;
            }
            return 0;
        }

        #endregion Plugin Event Handler

        #region Trace Handler

        protected void TraceProc(TraceLevel level, string text) {
#if TRACE
            PluginDomainTraceHandler(this, new TraceEventArgs(level, text));
#endif
        }

#if TRACE
        const string TraceCallbackPluginId = "TraceCallbackPluginId";
        const string TraceCallbackEventArg = "TraceCallbackEventArg";

        protected static void PluginDomainTraceHandler(object sender, TraceEventArgs e) {
            TcPlugin tp = sender as TcPlugin;
            if (tp == null)
                return;

            AppDomain mainDomain = tp.MainDomain;
            mainDomain.SetData(TraceCallbackPluginId, tp.TraceTitle);
            mainDomain.SetData(TraceCallbackEventArg, e);
            try {
                mainDomain.DoCallBack(MainDomainTraceHandler);
            } finally {
                mainDomain.SetData(TraceCallbackEventArg, null);
                mainDomain.SetData(TraceCallbackPluginId, null);
            }
        }

        public static void MainDomainTraceHandler() {
            string category = (string)AppDomain.CurrentDomain.GetData(TraceCallbackPluginId);
            TraceEventArgs e = (TraceEventArgs)AppDomain.CurrentDomain.GetData(TraceCallbackEventArg);
            TcTrace.TraceOut(e.Level, e.Text, category);
        }
#endif

        #endregion Trace Handler

        #region Other Methods

        public virtual void OnTcTrace(TraceLevel level, string text) {
        }

        public virtual void CreatePassword(int cryptoNumber, int flags) {
            Password = null;
        }

        protected void SetPluginFolder(string folderKey, string defaultFolder) {
            string folderName = Settings.ContainsKey(folderKey) ? Settings[folderKey] : defaultFolder;
            if (!String.IsNullOrEmpty(folderName)) {
                folderName = folderName
                    .Replace("%TC%", TcFolder)
                    .Replace("%PLUGIN%", PluginFolder);
                Settings[folderKey] = folderName;
            }
        }

        #endregion Other Methods
    }
}
