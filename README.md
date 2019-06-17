# TESS home assistant & home automation system

A simple console application written in C# using .NET Core framework.
built and designed to run on raspberry pi 3 kin headless and completely unattended mode.
able to handle errors smartly and do the needful on certain circumstances.

## Installation and execution
- Install .NET Core on your raspberry pi first. see here for installation [.net core installation](https://www.hanselman.com/blog/InstallingTheNETCore2xSDKOnARaspberryPiAndBlinkingAnLEDWithSystemDeviceGpio.aspx)
- Download the latest release from here [Release](https://github.com/SynergYFTW/HomeAssistant/releases)
- Create a new directory called Tess and extract downloaded .zip file to that folder.
- Open a terminal on tess folder and run HomeAssistant.dll using command `dotnet HomeAssistant.dll` on terminal.

## Features
- Cross-platform - not just because its made on dotnet core, but because it is smart enough to know its platform and adapt accordingly.
it can switch off various platform specific methods if it decteted that the current running os isn't what it is supposed to be.
- Automatically switch between Offline and Online mode - during network issues or network connection loss, it can dectet the change and switch to offline mode were all network related tasks will be stopped to prevent exceptions.
when the network is back online, it can start the previously stopped tasks again.
- Modular - it can customised according to the user running it through thr JSON config system whenever he needed.
- File watcher - automatically update the core config and other config values if the config files inside the `Config` folder is changed/deleted/modified.
- JSON Config file - Customize every aspect of the assistant from the JSON config files present in the Config directory.
- Deep logging - it uses NLog under the hood, that means it has a logging system which is powerful as well as completly modular with the `NLog.config` configuration file.
- GPIO pin control - complete control over the raspberry pi Gpio pins, including all 1 to 31 pins and states input and output and their values high and low.
- Interactive console - console is a powerful tool in terms of controlling the app. many pre defined charecters are used to define various methods.
- Multi threaded - Tess utilizes multi threading to perform at its best. it can run many tasks concurrently without delay.
- Task queue - task based functions, such as setting remainders etc will be added to a concurrentqueue with FIFO principal. they will started in the background thread concurrently with the main thread.
- Discord bot - allows to control tess using discord bot commands from home assistant discord chann
- TTS service - text to speech system for various important notifications and updates.
- Updater - automatically check for updates and update tess.
- Kestrel Http Server - Advanced REST Http server API with various endpoints to provide you with complete control over your assistant.
- Performence Counter - System to monitor CPU and RAM usage in real-time. [Currently only for Windows platforms]
- Swagger Docs - swagger integretion allows the user to view every available endpoints with their information on the route URL.

**To be edited later on**

