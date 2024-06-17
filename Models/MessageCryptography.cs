using System;
using System.Collections.Generic;
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
    }
}
