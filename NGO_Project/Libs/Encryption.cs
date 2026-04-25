using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NGO_Project.Libs
{
    public static class Encryption
    {
        private static readonly string Key = "X8g4Lr1mQz7vW2eTk9AsD3JpFiNbYc5u"; // 32 characters
        private static readonly string IV = "7hGtP2xQeWvZa9Lk";               // 16 characters
        public static string Encrypt(string plainText)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(Key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(IV);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        public static string Decrypt(string encryptedText)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(Key);
            byte[] ivBytes = Encoding.UTF8.GetBytes(IV);
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = ivBytes;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(encryptedBytes, 0, encryptedBytes.Length);
                        cs.FlushFinalBlock();
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
        }
    }
}
