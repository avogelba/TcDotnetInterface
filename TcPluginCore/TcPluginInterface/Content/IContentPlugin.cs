namespace OY.TotalCommander.TcPluginInterface.Content {
    public interface IContentPlugin {
        #region Mandatory Methods

        ContentFieldType GetSupportedField(int fieldIndex, out string fieldName,
                out string units, int maxLen);
        GetValueResult GetValue(string fileName, int fieldIndex, int unitIndex,
                int maxLen, GetValueFlags flags, out string fieldValue, out ContentFieldType fieldType);

        #endregion Mandatory Methods

        #region Optional Methods

        // functions used in both WDX and WFX plugins
        void StopGetValue(string fileName);
        DefaultSortOrder GetDefaultSortOrder(int fieldIndex);
        void PluginUnloading();
        SupportedFieldOptions GetSupportedFieldFlags(int fieldIndex);
        SetValueResult SetValue(string fileName, int fieldIndex, int unitIndex,
                ContentFieldType fieldType, string fieldValue, SetValueFlags flags);

        // functions used in WDX plugins only
        SetValueResult EditValue(TcWindow parentWin, int fieldIndex, int unitIndex, ContentFieldType fieldType,
                ref string fieldValue, int maxLen, EditValueFlags flags, string langIdentifier);
        void SendStateInformation(StateChangeInfo state, string path);
        ContentCompareResult CompareFiles(int compareIndex, string fileName1, string fileName2,
                ContentFileDetails contentFileDetails, out int iconResourceId);

        // function used in WFX plugins only
        bool GetDefaultView(out string viewContents, out string viewHeaders, out string viewWidths,
                out string viewOptions, int maxLen);

        #endregion Optional Methods
    }
}