using System;
using System.Drawing;
using System.Windows.Forms;

namespace OY.TotalCommander.TcPluginTools {
    public partial class ErrorDialog: Form {
        private ErrorDialog() {
            components = null;
            InitializeComponent();
        }

        public static void Show(Exception ex) {
            Show(null, ex);
        }

        public static void Show(string callSignature, Exception ex) {
            using (ErrorDialog errDlg = new ErrorDialog()) {
                if (!String.IsNullOrEmpty(callSignature))
                    errDlg.lblException.Text = callSignature;
                errDlg.FormatException(ex, 0);
                errDlg.btnClose.Focus();
                if (errDlg.ShowDialog() == DialogResult.Abort) {
                    throw new OperationCanceledException("Execution terminated by user.", ex);
                }
            }
        }

        private void FormatException(Exception ex, int ident) {
            if (ident == 0) {
                rtbCallStack.Clear();
                if (lblException.Text.Equals("ERROR"))
                    lblException.Text = ex.GetType().Name + "  (detail)";
                //txtException.Text = ex.Message;
            } else {
                rtbCallStack.SelectionFont = new Font("Courier", 10f);
                rtbCallStack.SelectedText = Environment.NewLine;
            }
            rtbCallStack.SelectionIndent = ident * 20;
            rtbCallStack.SelectionFont = new Font("Courier", 8f, FontStyle.Bold);
            rtbCallStack.SelectedText = ex.GetType().Name + ": " + ex.Message + Environment.NewLine;
            rtbCallStack.SelectionIndent = ident * 20;
            rtbCallStack.SelectionFont = new Font("Courier", 8f);
            rtbCallStack.SelectedText = ex.StackTrace + Environment.NewLine;
            ident++;
            if (ex.InnerException != null) {
                FormatException(ex.InnerException, ident);
            }
        }

        private void butClose_Click(object sender, EventArgs e) {
            Close();
        }

    }
}
