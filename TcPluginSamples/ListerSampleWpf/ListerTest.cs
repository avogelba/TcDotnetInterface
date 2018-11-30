using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Printing;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using WPFUserControl = System.Windows.Controls.UserControl;
using System.Windows;
using System.Diagnostics;

using OY.TotalCommander.TcPluginInterface;
using OY.TotalCommander.TcPluginInterface.Lister;

namespace OY.TotalCommander.TcPlugins.ListerSampleWpf {
    public class ListerTest: ListerPlugin {
        public const string AllowedExtensionsOnForceShow = ".DNLW,.TXT,.LOG";

        #region Constructors

        public ListerTest(StringDictionary pluginSettings)
                : base(pluginSettings) {
            if (String.IsNullOrEmpty(Title))
                Title = ".NET Lister Test";
            DetectString = "EXT=\"DNLW\"";
            BitmapBackgroundColor = Color.Khaki;
        }

        #endregion Constructors

        private ArrayList controls = new ArrayList();

        [DllImport("msvcr71.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int _controlfp(int n, int mask);

        #region IListerPlugin Members

        public override object Load(string fileToLoad, ShowFlags showFlags) {
            WpfListerControl lc = null;
//            _controlfp(0x9001F, 0xFFFFF);
            if (!String.IsNullOrEmpty(fileToLoad)) {
                if ((showFlags & ShowFlags.ForceShow).Equals(ShowFlags.ForceShow)) {
                    string ext = Path.GetExtension(fileToLoad);
                    if (AllowedExtensionsOnForceShow.IndexOf(ext, StringComparison.InvariantCultureIgnoreCase) < 0)
                        return null;
                }
                lc = new WpfListerControl {
                    WrapText = GetWrapping(showFlags),
                    AsciiCharset = (showFlags & ShowFlags.Ascii).Equals(ShowFlags.Ascii)
                };

                lc.FileLoad(fileToLoad);
                ScrollProc(0);
                controls.Add(lc);
            }
            return lc;
        }

        private TextWrapping GetWrapping(ShowFlags showFlags) {
            return (showFlags & ShowFlags.WrapText).Equals(ShowFlags.WrapText) ? TextWrapping.Wrap : TextWrapping.NoWrap;
        }

        public override ListerResult LoadNext(object control, string fileToLoad, ShowFlags showFlags) {
            WpfListerControl lc = (WpfListerControl)control;
            if (!String.IsNullOrEmpty(fileToLoad)) {
                if ((showFlags & ShowFlags.ForceShow).Equals(ShowFlags.ForceShow)) {
                    string ext = Path.GetExtension(fileToLoad);
                    if (AllowedExtensionsOnForceShow.IndexOf(ext, StringComparison.InvariantCultureIgnoreCase) < 0)
                        return ListerResult.Error;
                }
                lc.WrapText = GetWrapping(showFlags);
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

        public override ListerResult SearchText(object control, string searchString, SearchParameter searchParameter) {
            WpfListerControl lc = (WpfListerControl)control;
            bool matchCase = (searchParameter & SearchParameter.MatchCase).Equals(SearchParameter.MatchCase);
            bool wholeWords = (searchParameter & SearchParameter.WholeWords).Equals(SearchParameter.WholeWords);
            bool findFirst = (searchParameter & SearchParameter.FindFirst).Equals(SearchParameter.FindFirst);
            bool backwards = (searchParameter & SearchParameter.Backwards).Equals(SearchParameter.Backwards);
            int percent = lc.Search(searchString, matchCase, wholeWords, findFirst, backwards, 0);
            if (percent >= 0)
                ScrollProc(percent);
            return ListerResult.OK;
        }

        public override ListerResult SendCommand(object control, ListerCommand command, ShowFlags parameter) {
            WpfListerControl lc = (WpfListerControl)control;
            switch (command) {
                case ListerCommand.Copy:
                    lc.Copy();
                    break;
                case ListerCommand.SelectAll:
                    lc.SelectAll();
                    break;
                case ListerCommand.NewParams:
                    lc.WrapText = GetWrapping(parameter);
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
            WpfListerControl lc = (WpfListerControl)control;
            PrintDialog dialog = new PrintDialog();
            if (dialog.ShowDialog().GetValueOrDefault(false)) {
                MessageBox.Show("WPF Form print stub");
            }
            return ListerResult.OK;
        }

        public override int NotificationReceived(object control, int message, int wParam, int lParam) {
            // do nothing
            return 0;
        }

        private const int BitmapFontSize = 8;
        private const string BitmapHeader = ".NET Lister (WPF)";
        public override Bitmap GetPreviewBitmap(string fileToLoad, int width, int height,
            byte[] contentBuf) {
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

        #endregion IListerPlugin Members

        #region Private Methods

        public override void OnTcTrace(TraceLevel level, string text) {
            string msg = TcTrace.GetTraceTimeString() + " - " + level.ToString().Substring(0, 1) + " : " + text;
            foreach (object o in controls) {
                if (o is WpfListerControl) {
                    ((WpfListerControl)o).AddLogMessage(msg);
                }
            }
        }

        #endregion Private Methods
    }
}
