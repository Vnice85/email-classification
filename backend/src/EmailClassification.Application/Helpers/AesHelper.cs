using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EmailClassification.Application.Helpers
{
    public static class AesHelper
    {
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(key))
                throw new ArgumentException("Plain text and key must not be null or empty.");

            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.GenerateIV();
            var iv = aes.IV;

            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);

            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(aes.Key, iv), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(key))
                throw new ArgumentException("Cipher text and key must not be null or empty.");
            var fullCipher = Convert.FromBase64String(cipherText);
            using var aes = System.Security.Cryptography.Aes.Create();

            aes.Key = Encoding.UTF8.GetBytes(key);
            var iv = new byte[aes.BlockSize / 8];
            Array.Copy(fullCipher, iv, iv.Length);
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new System.IO.MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var cs = new System.Security.Cryptography.CryptoStream(ms, decryptor, System.Security.Cryptography.CryptoStreamMode.Read);
            using var sr = new System.IO.StreamReader(cs);
            return sr.ReadToEnd();


        }
    }
}
