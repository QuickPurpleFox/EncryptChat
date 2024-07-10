using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using EncryptChat.ViewModels;

namespace EncryptChat.Models
{
    public class MessageCryptography
    {
        private ECDiffieHellmanCng _diffieHellman;
        private static byte[]? _publicKey;
        private byte[]? _aesKey;
        private Aes _aes;

        public MessageCryptography()
        {
            _diffieHellman = new ECDiffieHellmanCng
            {
                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
                HashAlgorithm = CngAlgorithm.Sha256
            };
            _publicKey = _diffieHellman.PublicKey.ToByteArray();
            _aes = Aes.Create();
            MainWindowViewModel.Messages.Add("Drop from cryptography constructor BASE64: " + Convert.ToBase64String (_publicKey));
        }

        public static byte[] PublicKey => _publicKey;
        
        public byte[] PublicKeyNonStatic => _publicKey;

        public byte[] ComputeDiffieHellmanSharedSecret(byte[] clientPublicKey)
        {
            CngKey clientKey = CngKey.Import(clientPublicKey, CngKeyBlobFormat.EccPublicBlob);
            _aesKey = _diffieHellman.DeriveKeyMaterial(clientKey);
            _aes.Key = _aesKey;
            return _aesKey;
        }

        public byte[] EncryptMessage(string plainText)
        {
            _aes.GenerateIV();
            ICryptoTransform encryptor = _aes.CreateEncryptor(_aes.Key, _aes.IV);
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                msEncrypt.Write(_aes.IV, 0, _aes.IV.Length);
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                }
                return msEncrypt.ToArray();
            }
        }

        public string DecryptMessage(byte[] cipherTextCombined, byte[] key)
        {
            using var aes = new AesManaged { Key = key };
            var iv = new byte[aes.BlockSize / 8];
            var cipherText = new byte[cipherTextCombined.Length - iv.Length];

            Array.Copy(cipherTextCombined, 0, iv, 0, iv.Length);
            Array.Copy(cipherTextCombined, iv.Length, cipherText, 0, cipherText.Length);

            using var decryptor = aes.CreateDecryptor(aes.Key, iv);
            var plainText = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

            return Encoding.UTF8.GetString(plainText);
        }

    }
}
