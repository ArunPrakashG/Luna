
//    _  _  ___  __  __ ___     _   ___ ___ ___ ___ _____ _   _  _ _____
//   | || |/ _ \|  \/  | __|   /_\ / __/ __|_ _/ __|_   _/_\ | \| |_   _|
//   | __ | (_) | |\/| | _|   / _ \\__ \__ \| |\__ \ | |/ _ \| .` | | |
//   |_||_|\___/|_|  |_|___| /_/ \_\___/___/___|___/ |_/_/ \_\_|\_| |_|
//

//MIT License

//Copyright(c) 2019 Arun Prakash
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Assistant.Log;

namespace Assistant.Security {

	public static class CryptoManager {
		private static readonly Logger Logger = new Logger("SECURITY");

		public static string Encrypt(string data) {
			if (string.IsNullOrEmpty(data) || string.IsNullOrWhiteSpace(data)) {
				return null;
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
				return null;
			}
		}

		public static string Decrypt(byte[] encryptedBytes) {
			if (encryptedBytes.Length <= 0) {
				return null;
			}

			try {
				using (RijndaelManaged myRijndael = new RijndaelManaged()) {
					string base64String = DecryptStringFromBytes(encryptedBytes, myRijndael.Key, myRijndael.IV);
					return Encoding.UTF8.GetString(Convert.FromBase64String(base64String));
				}
			}
			catch (Exception e) {
				Logger.Log(e);
				return null;
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

			string plaintext = null;
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
	}
}
