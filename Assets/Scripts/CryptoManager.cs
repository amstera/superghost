using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class CryptoManager : MonoBehaviour
{
    private static readonly byte[] key = Encoding.UTF8.GetBytes("2ZeSQawc0yUj6SI9");
    private static readonly byte[] iv = Encoding.UTF8.GetBytes("1ASrbtbfguiMtcIc");

    public static string EncryptString(string plainText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using (var msEncrypt = new MemoryStream())
            {
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    public static string DecryptString(string cipherText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using (var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (var srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }
}