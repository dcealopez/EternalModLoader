using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EternalModLoader.Blang
{
    /// <summary>
    /// BlangCrypt class
    /// </summary>
    internal class BlangCrypt
    {
        /// <summary>
        /// Encrypts or decrypts a blang file
        /// </summary>
        /// <param name="fileData">byte array contaning a blang file bytes</param>
        /// <param name="internalPath">blang file's internal path</param>
        /// <param name="decrypt">bool indicating if we wanna encrypt or decrypt</param>
        /// <returns>int indicating success or failure</returns>
        public static int IdCrypt(ref byte[] fileData, string internalPath, bool decrypt)
        {
            string keyDeriveStatic = "swapTeam\n";
            byte[] fileSalt = new byte[0xC];

            // Get fileSalt from file, or create a new one
            if (decrypt)
            {
                Array.Copy(fileData, fileSalt, 0xC);
            }
            else
            {
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(fileSalt);
                }
            }

            byte[] keyDeriveStaticBytes = new byte[0xA];
            Array.Copy(Encoding.ASCII.GetBytes(keyDeriveStatic), keyDeriveStaticBytes, 0xA - 1);
            keyDeriveStaticBytes[0xA - 1] = (byte)'\0';

            // Generate the encryption key for AES using SHA256
            byte[] encKey = null;

            try
            {
                encKey = HashData(fileSalt, keyDeriveStaticBytes, Encoding.ASCII.GetBytes(internalPath), null);
            }
            catch
            {
                return 1;
            }

            // Get IV for AES from the file, or create a new one
            byte[] fileIV = new byte[0x10];

            if (decrypt)
            {
                Array.Copy(fileData, 0xC, fileIV, 0, 0x10);
            }
            else
            {
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(fileIV);
                }
            }

            // Get plain text for AES
            byte[] fileText;
            byte[] hmac = new byte[0x20];

            if (decrypt)
            {
                fileText = new byte[fileData.Length - 0x1C - 0x20];
                Array.Copy(fileData, 0x1C, fileText, 0, fileText.Length);

                byte[] fileHmac = new byte[0x20];
                Array.Copy(fileData, fileData.Length - 0x20, fileHmac, 0, 0x20);

                // Get HMAC from file data
                try
                {
                    hmac = HashData(fileSalt, fileIV, fileText, encKey);
                }
                catch
                {
                    return 1;
                }

                // Make sure the file HMAC and the new HMAC are the same
                if (!hmac.SequenceEqual(fileHmac))
                {
                    return 1;
                }
            }
            else
            {
                fileText = fileData;
            }

            // Encrypt or decrypt the data using AES
            byte[] cryptedText = null;

            try
            {
                cryptedText = CryptData(decrypt, fileText, encKey.Take(0x10).ToArray(), fileIV);
            }
            catch
            {
                return 1;
            }

            // Write the new file into a memory stream
            var cryptMemoryStream = new MemoryStream();

            if (decrypt)
            {
                cryptMemoryStream.Write(cryptedText, 0, cryptedText.Length);
            }
            else
            {
                cryptMemoryStream.Write(fileSalt, 0, fileSalt.Length);
                cryptMemoryStream.Write(fileIV, 0, fileIV.Length);
                cryptMemoryStream.Write(cryptedText, 0, cryptedText.Length);

                try
                {
                    hmac = HashData(fileSalt, fileIV, cryptedText, encKey);
                }
                catch
                {
                    return 1;
                }

                cryptMemoryStream.Write(hmac, 0, hmac.Length);
            }

            fileData = cryptMemoryStream.ToArray();
            return 0;
        }

        /// <summary>
        /// Hashes or gets hmac of the given byte array
        /// </summary>
        /// <param name="pbBuf1">first byte array to hash</param>
        /// <param name="pbBuf2">second byte array to hash</param>
        /// <param name="pbBuf3">third byte array to hash</param>
        /// <param name="pbSecret">key for hmac generation, can be null</param>
        /// <returns>hash or hmac in bytes</returns>
        private static byte[] HashData(byte[] pbBuf1, byte[] pbBuf2, byte[] pbBuf3, byte[] pbSecret)
        {
            if (pbSecret == null)
            {
                using(SHA256 sha256 = SHA256.Create())
                {
                    sha256.TransformBlock(pbBuf1, 0, pbBuf1.Length, null, 0);
                    sha256.TransformBlock(pbBuf2, 0, pbBuf2.Length, null, 0);
                    sha256.TransformBlock(pbBuf3, 0, pbBuf3.Length, null, 0);

                    sha256.TransformFinalBlock(new byte[0], 0, 0);

                    return sha256.Hash;
                }
            }
            else
            {
                using (HMACSHA256 hmac = new HMACSHA256(pbSecret))
                {
                    hmac.TransformBlock(pbBuf1, 0, pbBuf1.Length, null, 0);
                    hmac.TransformBlock(pbBuf2, 0, pbBuf2.Length, null, 0);
                    hmac.TransformBlock(pbBuf3, 0, pbBuf3.Length, null, 0);

                    hmac.TransformFinalBlock(new byte[0], 0, 0);

                    return hmac.Hash;
                }
            }
        }

        /// <summary>
        /// Encrypts or decrypts data using AES
        /// </summary>
        /// <param name="decrypt">bool indicating if we wanna encrypt or decrypt</param>
        /// <param name="pbInput">data to encrypt/decrypt</param>
        /// <param name="pbEncKey">AES key</param>
        /// <param name="pbIV">AES IV</param>
        /// <returns>encrypted/decrypted data bytes</returns>
        private static byte[] CryptData(bool decrypt, byte[] pbInput, byte[] pbEncKey, byte[] pbIV)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = pbEncKey;
                aesAlg.IV = pbIV;

                if (decrypt)
                {
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(pbInput))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            byte [] decryptedData = new byte[pbInput.Length];
                            int bytesRead = csDecrypt.Read(decryptedData, 0, pbInput.Length);
                            return decryptedData.Take(bytesRead).ToArray();
                        }
                    }
                }
                else
                {
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            csEncrypt.Write(pbInput, 0, pbInput.Length);
                            csEncrypt.FlushFinalBlock();

                            return msEncrypt.ToArray();
                        }
                    }
                }
            }
        }
    }
}
