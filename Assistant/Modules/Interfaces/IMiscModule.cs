using System;

namespace HomeAssistant.Modules.Interfaces {

	public interface IMiscModule : IModuleBase {

		/// <summary>
		/// Converts the specified DateTime source to 24 hour formate
		/// </summary>
		/// <param name="source">The source</param>
		/// <returns>24 Hour formate DateTime</returns>
		DateTime ConvertTo24Hours(DateTime source);

		/// <summary>
		/// Converts 24 hour DateTime source to 12 hour formate
		/// </summary>
		/// <param name="source">The source</param>
		/// <returns>12 Hour formate DateTime</returns>
		DateTime ConvertTo12Hours(DateTime source);
	}
}
