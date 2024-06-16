using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EncryptChat.Models;

public class MessageCryptography
{
    private string _RsaPublicKey;
    private string _RsaPrivateKey;

    private string? _AesPrivateKey;
    
    public static Dictionary<string, string> ClientPublicKeys = new Dictionary<string, string>();
    
    MessageCryptography()
    {
        //RSA
        var cryptoServiceProvider = new RSACryptoServiceProvider(2048); //2048 
        var privateKey = cryptoServiceProvider.ExportParameters(true); 
        var publicKey = cryptoServiceProvider.ExportParameters(false); 
        
        _RsaPublicKey = GetKeyString(publicKey);
        _RsaPrivateKey = GetKeyString(privateKey);
    }
    
    private string GetKeyString(RSAParameters publicKey)
    {

        var stringWriter = new System.IO.StringWriter();
        var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
        xmlSerializer.Serialize(stringWriter, publicKey);
        return stringWriter.ToString();
    }
    
    public static string RsaEncrypt(string textToEncrypt, string publicKeyString)
    {
        var bytesToEncrypt = Encoding.UTF8.GetBytes(textToEncrypt);

        using (var rsa = new RSACryptoServiceProvider(2048))
        {
            try
            {               
                rsa.FromXmlString(publicKeyString.ToString());
                var encryptedData = rsa.Encrypt(bytesToEncrypt, true);
                var base64Encrypted = Convert.ToBase64String(encryptedData);
                return base64Encrypted;
            }
            finally
            {
                rsa.PersistKeyInCsp = false;
            }
        }
    }

    public static string RsaDecrypt(string textToDecrypt, string privateKeyString)
    {
        var bytesToDescrypt = Encoding.UTF8.GetBytes(textToDecrypt);

        using (var rsa = new RSACryptoServiceProvider(2048))
        {
            try
            {
                // server decrypting data with private key                    
                rsa.FromXmlString(privateKeyString);

                var resultBytes = Convert.FromBase64String(textToDecrypt);
                var decryptedBytes = rsa.Decrypt(resultBytes, true);
                var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                return decryptedData.ToString();
            }
            finally
            {
                rsa.PersistKeyInCsp = false;
            }
        }
    }

    public string GetPublicKey()
    {
        return _RsaPublicKey;
    }
}