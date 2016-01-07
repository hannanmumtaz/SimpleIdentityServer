﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using SimpleIdentityServer.Core.Common.Extensions;
using SimpleIdentityServer.Core.Extensions;

namespace TestProj
{
    class Program
    {
        private static void Sign()
        {
            var toEncrypt = "coucou";
            byte[] b,
                bytes;
            string exported;
            string exportedPublicKey;
            using (var rsaCryptoServiceProvider = new RSACryptoServiceProvider())
            {
                var parameters = rsaCryptoServiceProvider.ExportParameters(false);
                exported = rsaCryptoServiceProvider.ToXmlString(true);
                exportedPublicKey = rsaCryptoServiceProvider.ToXmlString(false);
            }

            using (var provider = new RSACryptoServiceProvider())
            {
                bytes = ASCIIEncoding.ASCII.GetBytes(toEncrypt);
                provider.FromXmlString(exported);
                b = provider.SignData(bytes, "SHA256");
            }

            // We need "MODULE" + "Exponent" to check the signature.
            using (var provider2 = new RSACryptoServiceProvider())
            {
                provider2.FromXmlString(exportedPublicKey);
                var result = provider2.VerifyData(bytes, "SHA256", b);
                Console.WriteLine(result);
            }

            using (var ec = new ECDiffieHellmanCng())
            {
                ec.ToXmlString(ECKeyXmlFormat.Rfc4050);
            }
        }

        private static string Decrypt(string parameter, string parameters)
        {
            var dataToEncrypt = parameter.Base64DecodeBytes();
            var encryptedBytes = RsaDecrypt(dataToEncrypt, parameters, false);
            return ASCIIEncoding.ASCII.GetString(encryptedBytes);
        }

        private static byte[] RsaDecrypt(byte[] dataToEncrypt, string rsaKeyInfo, bool DoOAEPPadding)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(rsaKeyInfo);
                return rsa.Decrypt(dataToEncrypt, DoOAEPPadding);
            }
        }

        private static byte[] GenerateRandomBytes(int size)
        {
            var data = new byte[size/8];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(data);
                return data;
            }
        }

        private static byte[][] SplitByteArrayInHalf(byte[] arr)
        {
            var halfIndex = arr.Length/2;
            var firstHalf = new byte[halfIndex];
            var secondHalf = new byte[halfIndex];
            Buffer.BlockCopy(arr, 0, firstHalf, 0, halfIndex);
            Buffer.BlockCopy(arr, halfIndex, secondHalf, 0, halfIndex);
            return new[]
            {
                firstHalf,
                secondHalf
            };
        }

        public static byte[] Concat(params byte[][] arrays)
        {
            byte[] result = new byte[arrays.Sum(a => (a == null) ? 0 : a.Length)];
            int offset = 0;

            foreach (byte[] array in arrays)
            {
                if (array == null) continue;

                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }

            return result;
        }

        public static byte[] LongToBytes(long value)
        {
            ulong _value = (ulong)value;

            return BitConverter.IsLittleEndian
                ? new[] { (byte)((_value >> 56) & 0xFF), (byte)((_value >> 48) & 0xFF), (byte)((_value >> 40) & 0xFF), (byte)((_value >> 32) & 0xFF), (byte)((_value >> 24) & 0xFF), (byte)((_value >> 16) & 0xFF), (byte)((_value >> 8) & 0xFF), (byte)(_value & 0xFF) }
                : new[] { (byte)(_value & 0xFF), (byte)((_value >> 8) & 0xFF), (byte)((_value >> 16) & 0xFF), (byte)((_value >> 24) & 0xFF), (byte)((_value >> 32) & 0xFF), (byte)((_value >> 40) & 0xFF), (byte)((_value >> 48) & 0xFF), (byte)((_value >> 56) & 0xFF) };
        }

        public static bool ConstantTimeEquals(byte[] expected, byte[] actual)
        {
            if (expected == actual)
                return true;

            if (expected == null || actual == null)
                return false;

            if (expected.Length != actual.Length)
                return false;

            bool equals = true;

            for (int i = 0; i < expected.Length; i++)
                if (expected[i] != actual[i])
                    equals = false;

            return equals;
        }

        #region Encryption

        private static string GenerateJwe(
            string toEncrypt,
            out string rsaXml,
            Func<byte[][], byte[]> hmacKeyParser = null)
        {
            var cekSize = 256;
            string parameters;
            
            // Generate a random Content encryption key with 256 bit.
            var contentEncryptionKey = GenerateRandomBytes(cekSize);

            // Encrypt the Content Encryption Key with RSA algorithm : RSA1_5
            // Encrypt the "Content encryption key" with the public key of the client.
            byte[] encryptedEncryptionKey;
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsaXml = rsa.ToXmlString(true);
                encryptedEncryptionKey = rsa.Encrypt(contentEncryptionKey, false);
            }
            
            // Fetch the MAC_KEY
            // In some case the HASH_MAC KEY is the client_secret : shared key
            var contentEncryptionKeySplitted = SplitByteArrayInHalf(contentEncryptionKey);
            byte[] hmacKey;
            if (hmacKeyParser == null)
            {
                hmacKey = contentEncryptionKeySplitted[0];
            }
            else
            {
                hmacKey = hmacKeyParser(contentEncryptionKeySplitted);
            }

            var aesCbcKey = contentEncryptionKeySplitted[1];

            // Initialize the vector value.
            // IV size equal : key size / 2
            var iv = GenerateRandomBytes(128);

            byte[] ciptherText;

            // Encrypt the plain text to create ciphertext.
            using (var aes = new AesManaged())
            {
                aes.Key = aesCbcKey;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (var swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(toEncrypt);
                            }

                            ciptherText = msEncrypt.ToArray();
                        }
                    }
                }
            }

            // Concatenate the AAD + IV + ciphertext + AL

            var protectedHeader = new Dictionary<string, string>
            {
                {
                    "alg", "RSA1_5"
                },
                {
                    "enc", "A128CBC_HS256"
                },
                {
                    "kid", "1"
                }
            };

            // base64 encoded protected header
            var jweProtectedHeaderSerialized = protectedHeader.SerializeWithJavascript();

            var base64EncodedJweProtectedHeader = jweProtectedHeaderSerialized.Base64Encode();

            // jwe encrypted key
            var base64EncodedJweEncryptedKey = Convert.ToBase64String(encryptedEncryptionKey);

            // iv
            var base64EncodedIv = Convert.ToBase64String(iv);

            // Cipther text
            var base64EncodedCipherText = Convert.ToBase64String(ciptherText);

            // Calculate the additional authenticated data
            var aad = Encoding.UTF8.GetBytes(jweProtectedHeaderSerialized);
            var base64EncodedAdd = Convert.ToBase64String(aad);

            // Calculate the authentication tag
            var al = LongToBytes(aad.Length * 8);
            var hmacInput = Concat(aad, iv, ciptherText, al);

            byte[] hmacValue;
            // Sign the HMAC input with HMAC KEY
            using (var crypt = new HMACSHA256(hmacKey))
            {
                hmacValue = crypt.ComputeHash(hmacInput);
            }

            var authenticationTag = SplitByteArrayInHalf(hmacValue)[0];
            var base64AuthenticationTag = Convert.ToBase64String(authenticationTag);

            var result = string.Format("{0}.{1}.{2}.{3}.{4}",
                base64EncodedJweProtectedHeader,
                base64EncodedJweEncryptedKey,
                base64EncodedIv,
                base64EncodedCipherText,
                base64AuthenticationTag);

            Console.WriteLine(result);

            return result;
        }


        private static string GenerateJweWithClientSecretAsHmacKey(
            string toEncrypt,
            string clientSecret,
            out string rsaXml)
        {
            var callback = new Func<byte[][], byte[]>((arg) => Encoding.UTF8.GetBytes(clientSecret));
            return GenerateJwe(toEncrypt, out rsaXml, callback);
        }

        #endregion

        #region Decryption

        private static string DecodeJwe(
            string jwe,
            string rsaXml,
            Func<byte[]> getHmacKeyCallback = null)
        {
            var jweSplitted = jwe.Split('.');

            var jweEncryptedKeyBytes = Convert.FromBase64String(jweSplitted[1]);
            var ivBytes = Convert.FromBase64String(jweSplitted[2]);
            var cipherTextBytes = Convert.FromBase64String(jweSplitted[3]);
            var authenticationTagBytes = Convert.FromBase64String(jweSplitted[4]);

            var jweProtectedHeaderSerialized = jweSplitted[0].Base64Decode();
            var jweEncryptedKey = jweSplitted[1].Base64Decode();
            var iv = jweSplitted[2].Base64Decode();
            var cipherText = jweSplitted[3].Base64Decode();
            var authenticationTag = jweSplitted[4].Base64Decode();
            
            // Decrypt the encryption key with the private key of the client.
            byte[] contentEncryptionKey;
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(rsaXml);
                contentEncryptionKey = rsa.Decrypt(jweEncryptedKeyBytes, false);
            }

            // Calculate the AAD
            var aad = Encoding.UTF8.GetBytes(jweProtectedHeaderSerialized);
            
            var contentEncryptionKeySplitted = SplitByteArrayInHalf(contentEncryptionKey);
            byte[] hmacKey;
            if (getHmacKeyCallback == null)
            {
                hmacKey = contentEncryptionKeySplitted[0];
            }
            else
            {
                hmacKey = getHmacKeyCallback();
            }

            var aesCbcKey = contentEncryptionKeySplitted[1];
            
            // Calculate the authentication tag
            var al = LongToBytes(aad.Length * 8);
            var hmacInput = Concat(aad, ivBytes, cipherTextBytes, al);

            byte[] hmacValue;
            // Sign the HMAC input with HMAC KEY
            using (var crypt = new HMACSHA256(hmacKey))
            {
                hmacValue = crypt.ComputeHash(hmacInput);
            }

            var authenticationTagCalculated = SplitByteArrayInHalf(hmacValue)[0];
            var macAreEquals = ConstantTimeEquals(authenticationTagCalculated, authenticationTagBytes);

            string result;
            // Encrypt the plain text to create ciphertext.
            using (var aes = new AesManaged())
            {
                aes.Key = aesCbcKey;
                aes.IV = ivBytes;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    using (var msDecrypt = new MemoryStream(cipherTextBytes))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                result = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }

            return result;
        }
        
        private static string DecodeJweWithClientSecret(
            string jwe,
            string rsaXml,
            string clientSecret)
        {
            var callback = new Func<byte[]>(() => Encoding.UTF8.GetBytes(clientSecret));
            return DecodeJwe(jwe, rsaXml, callback);
        }

        #endregion

        private static void UnpackEccPublicBlob(byte[] blob, out BigInteger x, out BigInteger y)
        {
            var count = BitConverter.ToInt32(blob, 4);
            x = new BigInteger(ReverseBytes(blob, 8, count, true));
            y = new BigInteger(ReverseBytes(blob, 8 + count, count, true));
        }

        private static byte[] ReverseBytes(byte[] buffer, int offset, int count, bool padWithZeroByte)
        {
            var numArray = !padWithZeroByte ? new byte[count] : new byte[count + 1];
            var num = offset + count - 1;
            for (var index = 0; index < count; ++index)
            {
                numArray[index] = buffer[num - index];
            }

            var size = numArray.Length;
            return numArray;
        }

        public static byte[][] Slice(byte[] array, int count)
        {
            var sliceCount = array.Length / count;
            var result = new byte[sliceCount][];
            for (int i = 0; i < sliceCount; i++)
            {
                var slice = new byte[count];
                Buffer.BlockCopy(array, i * count, slice, 0, count);
                result[i] = slice;
            }

            return result;
        }

        public static byte[] RightmostBits(byte[] data, int lengthBits)
        {
            var byteCount = lengthBits / 8;
            var result = new byte[byteCount];
            Buffer.BlockCopy(data, data.Length - byteCount, result, 0, byteCount);
            return result;

        }

        private static CngKey GenerateRandomCngKey(CngAlgorithm cngAlgorithm)
        {
            // Generate the parameters : x, y, & d
            var keyCreationParameter = new CngKeyCreationParameters
            {
                ExportPolicy = CngExportPolicies.AllowPlaintextExport
            };

            var cnk = CngKey.Create(cngAlgorithm, null, keyCreationParameter);
            var privateBlob = cnk.Export(CngKeyBlobFormat.EccPrivateBlob);
            // Contains both the public & private portions of an ECC key
            var magicNumber = new[]
            {
                privateBlob[0],
                privateBlob[1],
                privateBlob[2],
                privateBlob[3]
            };
            
            var m = BitConverter.ToInt32(magicNumber, 0);

            var length = new[]
            {
                privateBlob[4],
                privateBlob[5],
                privateBlob[6],
                privateBlob[7]
            };

            // Part size in octet
            var partSize = BitConverter.ToInt32(length, 0);
            // Total size in bits equals to : part size * 8 (to convert into octet) * 3 (number of blocks)
            var keyParts = Slice(RightmostBits(privateBlob, partSize * 8 * 3), partSize);
            var x = keyParts[0];
            var y = keyParts[1];
            var d = keyParts[2];

            // IMPORT THE PARAMETERS : x, y, d
            var partLength = BitConverter.GetBytes(partSize);

            // 256 => 844317509 ==> 32
            // 384 => 877871941 ==> 48
            // 521 => 911426373 ==> size 66

            var magic = BitConverter.GetBytes(911426373);
            var blob = Concat(magic, partLength, x, y, d);
            return CngKey.Import(blob, CngKeyBlobFormat.EccPrivateBlob);
        }

        private static string SignWithEllipticCurve(string message,
            CngKey cngKey)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(message);
            string result;
            using (var ecsdaSignature = new ECDsaCng(cngKey))
            {
                result = ecsdaSignature.SignData(plainTextBytes).Base64EncodeBytes();
            }

            return result;
        }

        private static bool CheckSignatureWithEllipticCurve(string message,
            string signature,
            CngKey cngKey)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var signatureBytes = signature.Base64DecodeBytes();
            bool result;
            using (var ecsdaSignature = new ECDsaCng(cngKey))
            {
                result = ecsdaSignature.VerifyData(messageBytes, 
                    signatureBytes);
            }

            return result;
        }

        static void Main(string[] args)
        {
            var m =
                "eyJhbGciOiJub25lIn0.eyJub25jZSI6ICJNQktmV3g4U0d0MmoiLCAic3RhdGUiOiAiVzhoRGRRT3FkV0c2dDllQSIsICJyZWRpcmVjdF91cmkiOiAiaHR0cHM6Ly9vcC5jZXJ0aWZpY2F0aW9uLm9wZW5pZC5uZXQ6NjAxODYvYXV0aHpfY2IiLCAicmVzcG9uc2VfdHlwZSI6ICJpZF90b2tlbiIsICJjbGllbnRfaWQiOiAiTXlCbG9nIiwgInNjb3BlIjogIm9wZW5pZCJ9.";

            var p = m.Split('.')[1];
            var payload = p.Base64Decode();

            var idToken =
                "eyJhbGciOiJSUzI1NiIsImtpZCI6ImEzck1VZ01Gdjl0UGNsTGE2eUYzekFrZnF1RSIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwOi8vbG9jYWxob3N0OjUwNDkvIiwiYXVkIjpbIk15QmxvZyIsIk15QmxvZ0NsaWVudFNlY3JldFBvc3QiLCJodHRwOi8vbG9jYWxob3N0OjUwNDkvIl0sImV4cCI6MTQ1NTEwMTgxNCwiaWF0IjoxNDUyMTAxODE0LCJub25jZSI6InNocE1lbExSeVBBaiIsImFjciI6Im9wZW5pZC5wYXBlLmF1dGhfbGV2ZWwubnMucGFzc3dvcmQ9MSIsImFtciI6InBhc3N3b3JkIiwiYXpwIjoiTXlCbG9nIiwic3ViIjoiYWRtaW5pc3RyYXRvckBob3RtYWlsLmJlIiwiYXRfaGFzaCI6InJ6eDJYR1pDNzJseVpocE1LS242aXcifQ.n1mfeJ1r3qVP96AalTQR2EW5tjRo4hOEbzGlS3hnV2PhXI-soaXDf6fcIo7XQYWj5rAkjCl0QmKgBhXbzh_2_EqsSebRopBtAvPrDR-UL4v6A-4EC8cwETuoOI6UL05SE81aHNVpBpHmrZ_3VboyeWeHt_SLOMQ_ifK6c2kQYvk";
            var i = idToken.Split('.')[1].Base64Decode();
            var header = idToken.Split('.')[0].Base64Decode();


            const string message = "message";
            var alg = CngAlgorithm.ECDsaP384;
            var cngKey = GenerateRandomCngKey(alg);

            // Sign the message
            var signedMessage = SignWithEllipticCurve(message,
                cngKey);
            var isSignatureCorrect = CheckSignatureWithEllipticCurve(message,
                signedMessage,
                cngKey);

            Console.WriteLine(signedMessage);
            Console.WriteLine(isSignatureCorrect);
            Console.ReadLine();

            // UnpackEccPublicBlob(publicBlob, out x, out y);

            /*

            // Extract the X & Y & D values
            var xValue = x.ToString("R", CultureInfo.InvariantCulture);
            var yValue = y.ToString("R", CultureInfo.InvariantCulture);
            var dValue = privateBlob.Base64EncodeBytes();

            var privateKeyBytes = dValue.Base64DecodeBytes();

            // Sign the data with private key
            var cngKey = CngKey.Import(privateKeyBytes, CngKeyBlobFormat.EccPrivateBlob);
            using (var ec = new ECDsaCng(cngKey))
            {
                ec.HashAlgorithm = alg;
                var messageBytes = Encoding.UTF8.GetBytes(message);
                encryptedBytes = ec.SignData(messageBytes);
            }
            */

            // Verify the data with public key
            // var concat = Concat(x, y);
            // var publicKeyCng = CngKey.Import()

            /*
            var m =
                "eyJhbGciOiJSUzI1NiIsImtpZCI6IjEiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwOi8vbG9jYWxob3N0L2lkZW50aXR5IiwiYXVkIjpbIk15QmxvZyJdLCJleHAiOjE0NTEzMTAxOTEsImlhdCI6MTQ0ODMxMDE5MSwic3ViIjoiYWRtaW5pc3RyYXRvckBob3RtYWlsLmJlIiwiYWNyIjoib3BlbmlkLnBhcGUuYXV0aF9sZXZlbC5ucy5wYXNzd29yZD0xIiwiYW1yIjoicGFzc3dvcmQiLCJhenAiOiJNeUJsb2cifQ.Ai+zreXQmsRIIosGOeuM2k8iBdBtnKa+b9m7isX6cg/1p5i4N2OK7Ul2679mdp2fcjj1f7panW0yOsJMTU1Ydo0khKiiH11bP/cShS5cDfW0haCqJuMNXN5j/X4wP4Vd7fDenqYG9wNcvQWpNn/Yqlm92lnHiGFdXF8pfKMagt8=";
            var r = m.Split('.');
            var s = Convert.FromBase64String("Ai+zreXQmsRIIosGOeuM2k8iBdBtnKa+b9m7isX6cg/1p5i4N2OK7Ul2679mdp2fcjj1f7panW0yOsJMTU1Ydo0khKiiH11bP/cShS5cDfW0haCqJuMNXN5j/X4wP4Vd7fDenqYG9wNcvQWpNn/Yqlm92lnHiGFdXF8pfKMagt8=");
            // "Ai+zreXQmsRIIosGOeuM2k8iBdBtnKa+b9m7isX6cg/1p5i4N2OK7Ul2679mdp2fcjj1f7panW0yOsJMTU1Ydo0khKiiH11bP/cShS5cDfW0haCqJuMNXN5j/X4wP4Vd7fDenqYG9wNcvQWpNn/Yqlm92lnHiGFdXF8pfKMagt8="
            var serializedProtectedHeader = r[0].Base64Decode();
            var encryptedContentEncryptionKeyBytes = r[1].Base64DecodeBytes();
            var ivBytes = r[2].Base64DecodeBytes();
            var cipherText = r[3].Base64DecodeBytes();
            var authenticationTag = r[4].Base64DecodeBytes();

            var clientSecret = "ClientSecret";
            string rsaXml;
            var jwe = GenerateJweWithClientSecretAsHmacKey("hello", clientSecret, out rsaXml);

            var decrypted = DecodeJweWithClientSecret(jwe, rsaXml, clientSecret);
            Console.WriteLine(decrypted);
            Console.ReadLine();
            /*
            string parameters;
            var encryptedData = Encrypt("encrypt data", out parameters);
            Console.WriteLine(encryptedData);

            var decryptedData = Decrypt(encryptedData, parameters);
            Console.WriteLine(decryptedData);

            Console.ReadLine();
             */
            /*
            var today = DateTime.UtcNow.AddDays(2).ToString();
            var encoded =
                "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIiwia2lkIjpudWxsfQ==.eyJpc3MiOiJodHRwOi8vbG9jYWxob3N0L2lkZW50aXR5IiwiYXVkIjpbIk15QmxvZyJdLCJleHAiOjE0NTA2ODYzMDcsImlhdCI6MTQ0NzY4NjMwNywic3ViIjoiYWRtaW5pc3RyYXRvckBob3RtYWlsLmJlIiwiYWNyIjoib3BlbmlkLnBhcGUuYXV0aF9sZXZlbC5ucy5wYXNzd29yZD0xIiwiYW1yIjoicGFzc3dvcmQiLCJhenAiOiJNeUJsb2cifQ==.";
            var arr = encoded.Split('.');
            var header = arr[0].Base64Decode();
            var payload = arr[1].Base64Decode();
            Console.WriteLine(header);
            Console.WriteLine(payload);
            Console.ReadLine();*/
        }
    }
}
