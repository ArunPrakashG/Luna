using Assistant.Extensions.Interfaces;
using Assistant.Logging;
using Assistant.Logging.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Assistant.Security {
	public class Security : IExternal {
		private static readonly ILogger Logger = new Logger(typeof(Security).Name);

		public static string Encrypt(string data) {
			if (string.IsNullOrEmpty(data) || string.IsNullOrWhiteSpace(data)) {
				return string.Empty;
			}

			try {
				using (RijndaelManaged myRijndael = new RijndaelManaged()) {
					myRijndael.GenerateKey();
					myRijndael.GenerateIV();
					return Convert.ToBase64String(EncryptStringToBytes(data, myRijndael.Key, myRijndael.IV));
				}
			}
			catch (Exception e) {
				Logger.Log(e);
				return string.Empty;
			}
		}

		public static string Decrypt(byte[] encryptedBytes) {
			if (encryptedBytes.Length <= 0) {
				return string.Empty;
			}

			try {
				using (RijndaelManaged myRijndael = new RijndaelManaged()) {
					string base64String = DecryptStringFromBytes(encryptedBytes, myRijndael.Key, myRijndael.IV);
					return Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
				}
			}
			catch (Exception e) {
				Logger.Log(e);
				return string.Empty;
			}
		}

		private static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV) {
			if (plainText == null || plainText.Length <= 0) {
				throw new ArgumentNullException("plainText");
			}

			if (Key == null || Key.Length <= 0) {
				throw new ArgumentNullException("Key");
			}

			if (IV == null || IV.Length <= 0) {
				throw new ArgumentNullException("IV");
			}

			byte[] encrypted;
			using (RijndaelManaged rijAlg = new RijndaelManaged()) {
				rijAlg.Key = Key;
				rijAlg.IV = IV;
				ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);
				using (MemoryStream msEncrypt = new MemoryStream()) {
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
						using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {
							swEncrypt.Write(plainText);
						}
						encrypted = msEncrypt.ToArray();
					}
				}
			}
			return encrypted;
		}

		private static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV) {
			if (cipherText == null || cipherText.Length <= 0) {
				throw new ArgumentNullException("cipherText");
			}

			if (Key == null || Key.Length <= 0) {
				throw new ArgumentNullException("Key");
			}

			if (IV == null || IV.Length <= 0) {
				throw new ArgumentNullException("IV");
			}

			string plaintext = string.Empty;
			using (RijndaelManaged rijAlg = new RijndaelManaged()) {
				rijAlg.Key = Key;
				rijAlg.IV = IV;
				ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);
				using (MemoryStream msDecrypt = new MemoryStream(cipherText)) {
					using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
						using (StreamReader srDecrypt = new StreamReader(csDecrypt)) {
							plaintext = srDecrypt.ReadToEnd();
						}
					}
				}
			}
			return plaintext;
		}

		public void RegisterLoggerEvent(object eventHandler) => LoggerExtensions.RegisterLoggerEvent(eventHandler);
	}
}
