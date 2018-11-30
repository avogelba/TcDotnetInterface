using System;
using System.Collections.Specialized;
using System.IO;

using OY.TotalCommander.TcPluginInterface.QuickSearch;

namespace OY.TotalCommander.TcPlugins.QSSample {
    public class SampleQS: QuickSearchPlugin {
        #region Constructors

        public SampleQS(StringDictionary pluginSettings)
                : base(pluginSettings) {
        }

        #endregion Constructors

        #region IQSPlugin Members

        public override bool MatchFile(string filter, string fileName) {
            if (String.IsNullOrEmpty(filter))
                return true;
            string fName = Path.GetFileNameWithoutExtension(fileName);
            if (fName == "..")
                return false;
            return fName != null && fName.StartsWith(filter, StringComparison.CurrentCultureIgnoreCase);
        }

        public override MatchOptions MatchGetSetOptions(ExactNameMatch status) {
            return MatchOptions.OverrideInternalSearch | MatchOptions.NoLeadingTrailingAsterisk;
        }

        #endregion IQSPlugin Members
    }
}
