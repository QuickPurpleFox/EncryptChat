using System.Security.Cryptography;

namespace EncryptChat.Models;

public class MessageCryptography
{
    private string _RsaPublicKey;
    private string _RsaPrivateKey;

    private string _AesPrivateKey;
    
    MessageCryptography()
    {
        
    }
}