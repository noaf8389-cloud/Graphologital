using Microsoft.AspNetCore.DataProtection;

namespace Data_Extraction;

public class EncryptionService
{
    private readonly IDataProtector _protector;

    public EncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Graphologital.SensitiveData");
    }

    public string Encrypt(string value)
    {
        return _protector.Protect(value);
    }

    public string Decrypt(string value)
    {
        return _protector.Unprotect(value);
    }

    public string SafeDecrypt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
    
        try
        {
            return _protector.Unprotect(value);
        }
        catch
        {
            return value;
        }
    }
}