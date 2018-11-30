using System;

namespace OY.TotalCommander.TcPluginInterface {
    // Class, used to store passwords in the TC secure password store, 
    // retrieve them back, or copy them to a new store.
    // It's a parent class for FsPassword and PackerPassword clases.
    [Serializable]
    public class PluginPassword {
        private TcPlugin plugin;
        private readonly int cryptoNumber;
        private readonly CryptFlags flags;

        public PluginPassword(TcPlugin plugin, int cryptoNumber, int flags) {
            this.plugin = plugin;
            this.cryptoNumber = cryptoNumber;
            this.flags = (CryptFlags)flags;
        }

        public bool TcMasterPasswordDefined {
            get {
                return (flags & CryptFlags.MasterPassSet) == CryptFlags.MasterPassSet;
            }
        }

        // Convert result returned by TC to CryptResult. Must be overidden in derived classes.
        protected virtual CryptResult GetCryptResult(int tcCryptResult) {
            return CryptResult.PasswordNotFound;
        }

        #region Public Methods

        // Save password to password store.
        public CryptResult Save(string store, string password) {
            return Crypt(CryptMode.SavePassword, store, ref password);
        }

        // Load password from password store.
        public CryptResult Load(string store, ref string password) {
            password = String.Empty;
            return Crypt(CryptMode.LoadPassword, store, ref password);
        }

        // Load password from password store only if master password has already been entered.
        public CryptResult LoadNoUI(string store, ref string password) {
            password = String.Empty;
            return Crypt(CryptMode.LoadPasswordNoUI, store, ref password);
        }

        // Copy password to new store.
        public CryptResult Copy(string sourceStore, string targetStore) {
            return Crypt(CryptMode.CopyPassword, sourceStore, ref targetStore);
        }

        // Copy password to new store and delete the source password.
        public CryptResult Move(string sourceStore, string targetStore) {
            return Crypt(CryptMode.MovePassword, sourceStore, ref targetStore);
        }

        // Delete the password of the given store.
        public CryptResult Delete(string store) {
            var password = string.Empty;
            return Crypt(CryptMode.DeletePassword, store, ref password);
        }

        public int GetCryptoNumber() {
            return cryptoNumber;
        }

        public int GetFlags() {
            return (int)flags;
        }

        #endregion Public Methods

        private CryptResult Crypt(CryptMode mode, string storeName, ref string password) {
            CryptEventArgs e =
                new CryptEventArgs(plugin.PluginNumber, cryptoNumber, (int)mode, storeName, password);
            CryptResult result = GetCryptResult(plugin.OnTcPluginEvent(e));
            if (result == CryptResult.OK)
                password = e.Password;
            return result;
        }

        #region Private Enumerations

        [Flags]
        private enum CryptFlags {
            None = 0,
            MasterPassSet = 1         // The user already has a master password defined.
        }

        private enum CryptMode {
            SavePassword = 1,
            LoadPassword,
            LoadPasswordNoUI,
            CopyPassword,
            MovePassword,
            DeletePassword
        }

        #endregion Private Enumerations

    }
}
