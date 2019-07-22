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
