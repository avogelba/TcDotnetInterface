using System;
using System.IO;
using System.Windows.Forms;

using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginTools;

namespace OY.TotalCommander.TcPlugins.PackerSample.Zip {
    internal partial class PackerPassword: Form {
        private PasswordControl passwordControl;

        internal PackerPassword() {
            InitializeComponent();
        }

        internal PackerPassword(TcPlugin plugin, string archiveName) {
            InitializeComponent();
            string storeName = Path.GetFileName(archiveName);
            passwordControl = new PasswordControl(plugin, storeName) {
                WarningText = "Please enter the password for packing archive " + archiveName
            };
            panel.Controls.Add(passwordControl);
        }

        private void form_Load(object sender, EventArgs e) {
        }

        private void btnCancel_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
        }

        public string Password { get; set; }

        private void btnOK_Click(object sender, EventArgs e) {
            if (passwordControl != null) {
                passwordControl.SavePassword();
                Password = passwordControl.Password;
            }
            DialogResult = DialogResult.OK;
        }
    }
}
