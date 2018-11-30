using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace OY.TotalCommander.TcPluginInterface {
    [Serializable]
    [ComVisible(true)]
    public class MethodNotSupportedException: Exception {
        private const string Message0Fmt = "Mandatory method '{0}' is not supported";
        private const string Message1Fmt = "Method '{0}' is not supported";

        public bool Mandatory { get; set; }

        public MethodNotSupportedException() { }

        public MethodNotSupportedException(string message, Exception ex) 
                : base(message, ex) { }

        public MethodNotSupportedException(string methodName, bool mandatory) 
                : base(String.Format(mandatory ? Message0Fmt : Message1Fmt, methodName)) {
            Mandatory = mandatory;
        }

        public MethodNotSupportedException(string methodName) 
                : base(String.Format(Message1Fmt, methodName)) {
            Mandatory = false;
        }

        protected MethodNotSupportedException(SerializationInfo info, StreamingContext context) 
                : base(info, context) {
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info == null)
                throw new ArgumentNullException("info");
            info.AddValue("Mandatory", Mandatory.ToString());
            base.GetObjectData(info, context);
        }

    }
}