using System.Collections.Concurrent;

namespace Assistant.Modules.Interfaces {

	public interface IEmailClient : IModuleBase {

		/// <summary>
		/// The IEmailBot collection
		/// </summary>
		/// <value>String value represents the bot unique id, IEmailBot reporesents the bot instance</value>
		ConcurrentDictionary<string, IEmailBot> EmailClientCollection { get; set; }

		/// <summary>
		/// Invokes RegisterBot() method on all bots
		/// </summary>
		/// <returns>A boolean value indicating the startup status, A Dictionary with the unique id and IEmailBot instance of the specified index</returns>
		(bool, ConcurrentDictionary<string, IEmailBot>) InitEmailBots();

		/// <summary>
		/// Dispose the bot with the specified unique id
		/// </summary>
		/// <param name="botUniqueId">The unique id of the bot to remove</param>
		void DisposeEmailBot(string botUniqueId);

		/// <summary>
		/// Dispose all the IEmailBot instances which is currently running
		/// </summary>
		/// <returns>Boolean value indication status of the Dispose</returns>
		bool DisposeAllEmailBots();

		/// <summary>
		/// Adds the given instance of IEmailBot into the EmailClientCollection dictionary
		/// </summary>
		/// <param name="uniqueId">The unique id of the bot to add</param>
		/// <param name="bot">The bot instance</param>
		void AddBotToCollection(string uniqueId, IEmailBot bot);

		/// <summary>
		/// Removes the specified IEmailBot instance from EmailClientCollection dictionary using its unique id
		/// </summary>
		/// <param name="uniqueId">The unique id of the bot to remove</param>
		void RemoveBotFromCollection(string uniqueId);
	}
}
