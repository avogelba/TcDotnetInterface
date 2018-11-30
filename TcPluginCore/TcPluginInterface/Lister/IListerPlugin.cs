using System.Drawing;

namespace OY.TotalCommander.TcPluginInterface.Lister {
    public interface IListerPlugin {
        #region Mandatory Methods

        object Load(string fileToLoad, ShowFlags showFlags);

        #endregion Mandatory Methods

        #region Optional Methods

        ListerResult LoadNext(object control, string fileToLoad, ShowFlags showFlags);
        void CloseWindow(object control);
        ListerResult SearchText(object control, string searchString, SearchParameter searchParameter);
        ListerResult SendCommand(object control, ListerCommand command, ShowFlags parameter);
        ListerResult Print(object control, string fileToPrint, string defPrinter, PrintFlags printFlags,
                PrintMargins margins);
        int NotificationReceived(object control, int message, int wParam, int lParam);
        Bitmap GetPreviewBitmap(string fileToLoad, int width, int height, byte[] contentBuf);
        ListerResult SearchDialog(object control, bool findNext);

        #endregion Optional Methods
    }
}
