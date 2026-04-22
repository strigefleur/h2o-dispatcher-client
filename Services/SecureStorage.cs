using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace Felweed.Services;

public static class SecureStorage
{
    // Optional 'entropy' adds an extra layer of complexity to the encryption
    private static readonly byte[] SAdditionalEntropy = [9, 8, 3, 1, 5];

    public static bool SaveApiKey(string apiKey)
    {
        try
        {
            // Convert the string key to bytes
            var dataToEncrypt = Encoding.UTF8.GetBytes(apiKey);

            // Encrypt the data using DPAPI
            // DataProtectionScope.CurrentUser ensures only this Windows user can decrypt it
            var encryptedData =
                ProtectedData.Protect(dataToEncrypt, SAdditionalEntropy, DataProtectionScope.CurrentUser);

            var config = ConfigurationService.LoadConfig();
            config.EncryptedGitlabKey = Convert.ToBase64String(encryptedData);

            ConfigurationService.SaveConfig();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save API Key");
            return false;
        }
    }

    public static string? LoadApiKey()
    {
        var config = ConfigurationService.LoadConfig();
        if (config.EncryptedGitlabKey == null)
            return null;
        
        try
        {
            var encryptedData = Convert.FromBase64String(config.EncryptedGitlabKey);
            
            // Decrypt the data
            var decryptedData =
                ProtectedData.Unprotect(encryptedData, SAdditionalEntropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        }
        catch (CryptographicException ex)
        {
            // Occurs if the user changes or data is corrupted
            Log.Error(ex, "Failed to decrypt API Key");
            return null;
        }
    }
}