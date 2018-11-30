using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace OY.TotalCommander.TcPlugins.ListerSampleWpf {
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class WpfListerControl: UserControl {
        private string fileName;

        public TextWrapping WrapText {
            set {
                txtFile.TextWrapping = value;
                txtLog.TextWrapping = value;
            }
        }

        private bool fileLoaded;
        private Encoding encoding = Encoding.Default;
        public bool AsciiCharset {
            set {
                bool reload = encoding.Equals(Encoding.Default) ^ !value;
                encoding = value ? Encoding.ASCII : Encoding.Default;
                if (fileLoaded && reload)
                    FileLoad();
            }
        }

        public WpfListerControl() {
            InitializeComponent();
            WrapText = TextWrapping.NoWrap;
        }

        private void FileLoad() {
            FileLoad(this.fileName);
        }

        public void FileLoad(string fileName) {
            using (StreamReader sr = new StreamReader(fileName, encoding)) {
                txtFile.Text = sr.ReadToEnd();
                fileLoaded = true;
                this.fileName = fileName;
            }
            SetPercent(0);
        }

        public void Print(PrinterSettings printerSettings) {
            stringToPrint = txtFile.Text;   
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += PrintPage;
            pd.PrinterSettings = printerSettings;
            pd.DocumentName = Path.GetFileName(fileName);
            pd.Print();
        }

        private string stringToPrint;
        private void PrintPage(object sender, PrintPageEventArgs e) {
            int charactersOnPage;
            int linesPerPage;
            // Sets the value of charactersOnPage to the number of characters 
            // of stringToPrint that will fit within the bounds of the page.
            Font font = new Font(txtFile.FontFamily.Source, (float)txtFile.FontSize);
            e.Graphics.MeasureString(stringToPrint, font,
                e.MarginBounds.Size, StringFormat.GenericTypographic,
                out charactersOnPage, out linesPerPage);
            // Draws the string within the bounds of the page
            e.Graphics.DrawString(stringToPrint, font, Brushes.Black,
                e.MarginBounds, StringFormat.GenericTypographic);
            // Remove the portion of the string that has been printed.
            stringToPrint = stringToPrint.Substring(charactersOnPage);
            // Check to see if more pages are to be printed.
            e.HasMorePages = (stringToPrint.Length > 0);
        }

        public void Copy() {
            if (tabControl.TabIndex == 0)        // File tab
                txtFile.Copy();
            else if (tabControl.TabIndex == 1)   // Log tab
                txtLog.Copy();
        }

        public void SelectAll() {
            if (tabControl.TabIndex == 0)        // File tab
                txtFile.SelectAll();
            else if (tabControl.TabIndex == 1)   // Log tab
                txtLog.SelectAll();
        }

        public void SetPercent(int percent) {
            int pos = (int)Math.Round((double)(txtFile.Text.Length * percent / 100));
            ScrollToPosition(pos, 0, txtFile);
        }

        private void ScrollToPosition(int pos, int len, TextBox textBox) {
            if (pos < 0)
                pos = 0;
            if (pos >= textBox.Text.Length)
                pos = textBox.Text.Length - 1;
            if (len < 0)
                len = 0;
            textBox.SelectionStart = pos;
            textBox.SelectionLength = len;
            textBox.Focus();
        }

        private int logSearchPos = -1;
        private int logSearchLen;
        public int Search(string searchString, bool matchCase, bool wholeWords,
            bool findFirst, bool backwards, int tabIndex) {
            int result = -1;
            TextBox textBox = null;
            if (tabIndex == 0)             // File tab
                textBox = txtFile;
            else if (tabIndex == 1)        // Log tab
                textBox = txtLog;
            if (textBox != null) {
                int pos = findFirst ? 0 : textBox.SelectionStart;
                StringComparison sc = 
                    matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                int newPos;
                if (backwards) {
                    if (pos > 0)
                        pos--;
                    newPos = textBox.Text.LastIndexOf(searchString, pos, sc);
                } else {
                    if (pos > 0 && pos < txtFile.Text.Length - 1)
                        pos++;
                    newPos = textBox.Text.IndexOf(searchString, pos, sc);
                }
                if (newPos >= 0) {
                    ScrollToPosition(newPos, searchString.Length, txtFile);
                    if (tabIndex == 1) {
                        logSearchPos = newPos;
                        logSearchLen = searchString.Length;
                    } else
                        logSearchPos = -1;
                    result = (int)Math.Round((double)(newPos * 100 / txtFile.Text.Length));
                } else
                    Console.Beep();
            }
            return result;
        }

        public void AddLogMessage(string msg) {
            txtLog.AppendText(msg + Environment.NewLine);
            if (logSearchPos >= 0)
                ScrollToPosition(logSearchPos, logSearchLen, txtLog);
        }
    }
}
