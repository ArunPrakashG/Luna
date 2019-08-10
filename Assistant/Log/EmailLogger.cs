
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
using System.Net;
using System.Net.Mail;
using Assistant.AssistantCore;
using static Assistant.AssistantCore.Enums;

namespace Assistant.Log {

	public class EmailLogger {
		private static readonly Logger Logger = new Logger("EMAIL-LOGGER");

		public static void SendEmail(string message) {
			if (string.IsNullOrEmpty(message)) {
				Logger.Log("Message is null.", Enums.LogLevels.Warn);
				return;
			}

			try {
				MailMessage mail = new MailMessage();
				SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
				mail.From = new MailAddress(Core.Config.AssistantEmailId);
				mail.To.Add(Core.Config.OwnerEmailAddress);
				mail.Subject = $"-{Core.AssistantName} Home Assistant Notification-";
				mail.Body = message;
				SmtpServer.Port = 587;
				SmtpServer.Credentials = new NetworkCredential(Core.Config.AssistantEmailId, Core.Config.AssistantEmailPassword);
				SmtpServer.EnableSsl = true;
				SmtpServer.Send(mail);
			}
			catch (ArgumentNullException) {
				Logger.Log("Send from email or send from password appears to be null.", Enums.LogLevels.Warn);
				return;
			}
			catch (InvalidOperationException) {
				Logger.Log("Invalid operation exception. please check the code.", Enums.LogLevels.Error);
				return;
			}
			catch (SmtpException) {
				Logger.Log("Either the username or the password is wrong.", Enums.LogLevels.Warn);
				return;
			}
		}
	}
}
