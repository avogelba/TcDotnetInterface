﻿namespace OY.TotalCommander.TcPluginInterface {
    public enum CryptResult {
        OK = 0,              // Success.
        PasswordNotFound,    // Password not found in password store.
        NoMasterPassword,    // No master password entered yet.
        Failed,              // Encrypt/Decrypt failed.
        WriteError           // Could not write password to password store.
    }
}