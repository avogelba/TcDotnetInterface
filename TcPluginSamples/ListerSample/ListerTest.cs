using System;
using System.Collections;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;

using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.Lister;
using System.Diagnostics;

namespace OY.TotalCommander.TcPlugins.ListerSample {
    public class ListerTest: ListerPlugin {
        public const string AllowedExtensionsOnForceShow = ".DNL,.TXT,.LOG";

        #region Constructors

        public ListerTest(StringDictionary pluginSettings)
                : base(pluginSettings) {
            if (String.IsNullOrEmpty(Title))
                Title = ".NET Lister Test";
            DetectString = "EXT=\"DNL\"";
            BitmapBackgroundColor = Color.Khaki;
        }

        #endregion Constructors

        private ArrayList controls = new ArrayList();

        #region IListerPlugin Members

        public override object Load(string fileToLoad, ShowFlags showFlags) {
            ListerControl lc = null;
            if (!String.IsNullOrEmpty(fileToLoad)) {
                if ((showFlags & ShowFlags.ForceShow).Equals(ShowFlags.ForceShow)) {
                    string ext = Path.GetExtension(fileToLoad);
                    if (AllowedExtensionsOnForceShow.IndexOf(ext, StringComparison.InvariantCultureIgnoreCase) < 0)
                        return null;
                }
                lc = new ListerControl {
                    WrapText = (showFlags & ShowFlags.WrapText).Equals(ShowFlags.WrapText),
                    AsciiCharset = (showFlags & ShowFlags.Ascii).Equals(ShowFlags.Ascii)
                };

                lc.FileLoad(fileToLoad);
                FocusedControl = lc.tabControl;
                ScrollProc(0);

                controls.Add(lc);
            }
            return lc;
        }

        public override ListerResult LoadNext(object control, string fileToLoad, ShowFlags showFlags) {
            ListerControl lc = (ListerControl)control;
            if (!String.IsNullOrEmpty(fileToLoad)) {
                if ((showFlags & ShowFlags.ForceShow).Equals(ShowFlags.ForceShow)) {
                    string ext = Path.GetExtension(fileToLoad);
                    if (AllowedExtensionsOnForceShow.IndexOf(ext, StringComparison.InvariantCultureIgnoreCase) < 0)
                        return ListerResult.Error;
                }
                lc.WrapText = (showFlags & ShowFlags.WrapText).Equals(ShowFlags.WrapText);
                lc.AsciiCharset = (showFlags & ShowFlags.Ascii).Equals(ShowFlags.Ascii);
                lc.FileLoad(fileToLoad);
                ScrollProc(0);
                return ListerResult.OK;
            }
            return ListerResult.Error;
        }

        public override void CloseWindow(object control) {
            controls.Remove(control);
        }

        private int searchTabIndex;
        private Random rnd = new Random();
        public override ListerResult SearchText(object control, string searchString, SearchParameter searchParameter) {
            ListerControl lc = (ListerControl)control;
            bool matchCase = (searchParameter & SearchParameter.MatchCase).Equals(SearchParameter.MatchCase);
            bool wholeWords = (searchParameter & SearchParameter.WholeWords).Equals(SearchParameter.WholeWords);
            bool findFirst = (searchParameter & SearchParameter.FindFirst).Equals(SearchParameter.FindFirst);
            bool backwards = (searchParameter & SearchParameter.Backwards).Equals(SearchParameter.Backwards);
            int tabIndex = searchTabIndex;
            int percent = lc.Search(searchString, matchCase, wholeWords, findFirst, backwards, tabIndex);
            if (percent >= 0)
                ScrollProc(percent);
            return ListerResult.OK;
        }

        public override ListerResult SendCommand(object control, ListerCommand command, ShowFlags parameter) {
            ListerControl lc = (ListerControl)control;
            switch (command) {
                case ListerCommand.Copy:
                    lc.Copy();
                    break;
                case ListerCommand.SelectAll:
                    lc.SelectAll();
                    break;
                case ListerCommand.NewParams:
                    lc.WrapText = (parameter & ShowFlags.WrapText).Equals(ShowFlags.WrapText);
                    lc.AsciiCharset = (parameter & ShowFlags.Ascii).Equals(ShowFlags.Ascii);
                    break;
                case ListerCommand.SetPercent:
                    lc.SetPercent((int)parameter);
                    ScrollProc((int)parameter);
                    break;
            }
            return ListerResult.OK;
        }

        public override ListerResult Print(object control, string fileToPrint, string defPrinter,
                PrintFlags printFlags, PrintMargins pMargins) {
            ListerControl lc = (ListerControl)control;
            PrintDialog dialog = new PrintDialog { PrinterSettings = { PrinterName = defPrinter } };
            if (dialog.ShowDialog().Equals(DialogResult.OK)) {
                PrinterSettings printerSettings = dialog.PrinterSettings;

                //Margins margins = new Margins(pMargins.left, pMargins.right, pMargins.top, pMargins.bottom);
                //printerSettings.DefaultPageSettings.Margins = margins;

                lc.Print(printerSettings);
            }
            return ListerResult.OK;
        }

        public override int NotificationReceived(object control, int message, int wParam, int lParam) {
            // do nothing
            return 0;
        }

        private const int BitmapFontSize = 8;
        private const string BitmapHeader = ".NET Lister";
        public override Bitmap GetPreviewBitmap(string fileToLoad, int width, int height, byte[] contentBuf) {
            Bitmap bitmap = new Bitmap(width, height);
            Color headerColor = Color.Blue;
            Color textColor = Color.Black;
            string text = EncodingDetector.GetString(contentBuf);
            if (String.IsNullOrEmpty(text)) {
                using (FileStream fs = new FileStream(fileToLoad, FileMode.Open, FileAccess.Read)) {
                    byte[] bytes = new byte[1024];
                    fs.Read(bytes, 0, 1024);
                    text = EncodingDetector.GetString(bytes);
                }
            }
            Graphics gr = Graphics.FromImage(bitmap);
            Font font = new Font("Arial", BitmapFontSize);
            gr.Clear(Color.White);
            gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            gr.DrawString(BitmapHeader, font, new SolidBrush(headerColor), 0, 0);
            gr.DrawLine(new Pen(headerColor), 3, BitmapFontSize + 4, 60, BitmapFontSize + 4);
            gr.DrawString(text, font, new SolidBrush(textColor), 0, BitmapFontSize + 6);
            gr.Flush();
            return bitmap;
        }

        public override ListerResult SearchDialog(object control, bool findNext) {
            ListerResult result = ListerResult.Error;
            if (!findNext) {
                searchTabIndex = (rnd.NextDouble() < 0.7) ? 0 : 1;
                if (MessageBox.Show("Do you really want to search on " + ((searchTabIndex == 0) ? "File" : "Log") + " Tab?",
                    "Search",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question).Equals(DialogResult.No))
                    result = ListerResult.OK;
            }
            return result;
        }

        #endregion IListerPlugin Members

        #region Private Methods

        public override void OnTcTrace(TraceLevel level, string text) {
            string msg = TcTrace.GetTraceTimeString() + " - " + level.ToString().Substring(0, 1) + " : " + text;
            foreach (object o in controls) {
                if (o is ListerControl) {
                    ((ListerControl)o).AddLogMessage(msg);
                }
            }
        }

        #endregion Private Methods
    }
}
