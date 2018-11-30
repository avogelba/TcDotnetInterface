
namespace OY.TotalCommander.TcPluginInterface.QuickSearch {
    public interface IQuickSearchPlugin {
        #region Mandatory Methods

        bool MatchFile(string filter, string fileName);
        MatchOptions MatchGetSetOptions(ExactNameMatch status);

        #endregion Mandatory Methods
    }
}
