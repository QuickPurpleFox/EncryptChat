using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EncryptChat.Models
{
    public class MessageCryptography
    {
        private static string _RsaPublicKey;
        private static string _RsaPrivateKey;
        private static string? _AesPrivateKey;

        public static Dictionary<string, string> ClientPublicKeys = new Dictionary<string, string>();

        static MessageCryptography()
        {
            // Initialize RSA keys
            var cryptoServiceProvider = new RSACryptoServiceProvider(2048); //2048 bit key size
            var privateKey = cryptoServiceProvider.ExportParameters(true); 
            var publicKey = cryptoServiceProvider.ExportParameters(false); 

            _RsaPublicKey = GetKeyString(publicKey);
            _RsaPrivateKey = GetKeyString(privateKey);
        }

        /// <summary>
        /// Converts RSA parameters to an XML string.
        /// </summary>
        /// <param name="key">The RSA parameters.</param>
        /// <returns>The XML string representation of the RSA key.</returns>
        private static string GetKeyString(RSAParameters key)
        {
            var stringWriter = new System.IO.StringWriter();
            var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            xmlSerializer.Serialize(stringWriter, key);
            return stringWriter.ToString();
        }

        /// <summary>
        /// Encrypts the given text using RSA encryption with the provided public key.
        /// </summary>
        /// <param name="textToEncrypt">The text to encrypt.</param>
        /// <param name="publicKeyString">The public key in XML string format.</param>
        /// <returns>The encrypted text in Base64 format.</returns>
        public static string RsaEncrypt(string textToEncrypt, string publicKeyString)
        {
            var bytesToEncrypt = Encoding.UTF8.GetBytes(textToEncrypt);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    rsa.FromXmlString(publicKeyString);
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

        /// <summary>
        /// Decrypts the given text using RSA decryption with the provided private key.
        /// </summary>
        /// <param name="textToDecrypt">The text to decrypt.</param>
        /// <param name="privateKeyString">The private key in XML string format.</param>
        /// <returns>The decrypted text.</returns>
        public static string RsaDecrypt(string textToDecrypt, string privateKeyString)
        {
            var bytesToDecrypt = Encoding.UTF8.GetBytes(textToDecrypt);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                try
                {
                    rsa.FromXmlString(privateKeyString);

                    var resultBytes = Convert.FromBase64String(textToDecrypt);
                    var decryptedBytes = rsa.Decrypt(resultBytes, true);
                    var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                    return decryptedData;
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        /// <summary>
        /// Gets the RSA public key.
        /// </summary>
        /// <returns>The RSA public key as a string.</returns>
        public static string GetPublicKeyStatic()
        {
            return _RsaPublicKey;
        }

        /// <summary>
        /// Gets the RSA public key.
        /// </summary>
        /// <returns>The RSA public key as a string.</returns>
        public string GetPublicKey()
        {
            return GetPublicKeyStatic();
        }
        
        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256 instance
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Compute the hash as a byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert the byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        
        static byte[] EncryptStringToBytes_Aes(string plainText, string key)
    {
        byte[] encrypted;

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.Mode = CipherMode.CBC;

            // Generate IV (Initialization Vector)
            aesAlg.GenerateIV();
            byte[] iv = aesAlg.IV;

            // Create an encryptor to perform the stream transform.
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, iv);

            // Create the streams used for encryption.
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                // Prepend IV to the encrypted bytes
                msEncrypt.Write(iv, 0, iv.Length);

                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        //Write all data to the stream.
                        swEncrypt.Write(plainText);
                    }
                }
                encrypted = msEncrypt.ToArray();
            }
        }

        // Return the encrypted bytes from the memory stream.
        return encrypted;
    }

    static string DecryptStringFromBytes_Aes(byte[] cipherText, string key)
    {
        string plaintext = null;

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.Mode = CipherMode.CBC;

            // Get IV from the beginning of the cipherText
            byte[] iv = new byte[aesAlg.BlockSize / 8];
            Array.Copy(cipherText, 0, iv, 0, iv.Length);
            aesAlg.IV = iv;

            // Create a decryptor to perform the stream transform.
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            // Create the streams used for decryption.
            using (MemoryStream msDecrypt = new MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        // Read the decrypted bytes from the decrypting stream and place them in a string.
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        return plaintext;
    }
    }
}
