using System.Security.Cryptography;
using System.Text;

public class SymmetricEncryptionDecryptionManager
{
    public static string Encrypt(string data, string key)
    {
        byte[] initializationVector = new byte[16];
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = initializationVector;
        var symmetricEncryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] encryptedBytes = symmetricEncryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

        return Convert.ToBase64String(encryptedBytes);
    }

    public static string Decrypt(string cipherText, string key)
    {
        byte[] initializationVector = new byte[16];
        byte[] buffer = Convert.FromBase64String(cipherText);
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = initializationVector;
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        byte[] decryptedBytes = decryptor.TransformFinalBlock(buffer, 0, buffer.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}