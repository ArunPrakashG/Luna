
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
using Assistant.AssistantCore;

namespace Assistant.Modules.Interfaces {

	public interface IModuleBase {

		///<summary>
		/// Identifier for the Module.
		///</summary>
		(Enums.ModuleType, string) ModuleIdentifier { get; set; }

		///<summary>
		/// Specifies if the module requires a stable constant internet connection to function.
		///</summary>
		bool RequiresInternetConnection { get; }

		///<summary>
		/// Author of the Module.
		///</summary>
		string ModuleAuthor { get; }

		///<summary>
		/// Version of the Module.
		///</summary>
		Version ModuleVersion { get; }

		///<summary>
		/// Invoked during module startup.
		///</summary>
		bool InitModuleService();

		///<summary>
		/// Invoked to shutdown module.
		///</summary>
		bool InitModuleShutdown();
	}
}
