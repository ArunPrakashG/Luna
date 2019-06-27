# TESS ASSISTANT

A simple console application written in C# using .NET Core framework with a single goal, provide you with assistance in your home,
turning on lights, and turning them off, just with your voice!
that's not all, it can be more than your personal assistant, it can be your J.A.R.V.I.S.
Built and designed to run on raspberry pi 3 in headless and completely unattended mode.
able to handle errors smartly and do the needful on certain circumstances.

## Installation
- Install .NET Core on your raspberry pi first. see here for installation [.net core installation](https://www.hanselman.com/blog/InstallingTheNETCore2xSDKOnARaspberryPiAndBlinkingAnLEDWithSystemDeviceGpio.aspx)
- Download the latest release from here [Release](https://github.com/SynergYFTW/HomeAssistant/releases)
- Create a new directory called Tess and extract downloaded .zip file to that folder.

## Startup (assuming you followed the above installation steps...)
- Open a terminal on tess folder.
- Run HomeAssistant.dll using command `dotnet HomeAssistant.dll` on terminal.
- Wait for tess to initialise.

## Features
- Cross-platform - not just because its made on dotnet core, but because it is smart enough to know its platform and adapt accordingly.it can switch off various platform specific methods if it decteted that the current running os isn't what it is made to run on.
- Smart network switcher - during network issues or network connection loss, it can know about the change and switch to offline mode were all network related tasks will be stopped to prevent exceptions. when the network is back online, it can start the previously stopped tasks again.
- Modular - it can customised according to the user running it through thr JSON config system whenever he needed.
- File watcher - automatically update the core config and other config values if the config files inside the `Config` folder is changed/deleted/modified.
- JSON Config file - Customize every aspect of the assistant from the JSON config files present in the Config directory.
- Deep logging - it uses NLog under the hood, that means it has a logging system which is powerful as well as completly modular with the `NLog.config` configuration file.
- GPIO pin control - complete control over the raspberry pi Gpio pins, including all 1 to 31 pins and states input and output and their values high and low.
- Interactive console - console is a powerful tool in terms of controlling the app. many pre defined charecters are used to define various methods.
- Multi threaded - Tess utilizes multi threading to perform at its best. it can run many tasks concurrently without delay.
- Task queue - task based functions, such as setting remainders etc will be added to a concurrent queue with FIFO principal. they will started in the background thread concurrently with the main thread.
- TTS service - text to speech system for various important notifications and updates.
- Updater - automatically check for updates and update tess.
- Kestrel Http Server - Advanced REST Http server API with various endpoints to provide you with complete control over your assistant.
- Performence Counter - System to monitor CPU and RAM usage in real-time. [Currently only for Windows platforms]
- Swagger Docs - swagger integretion allows the user to view every available endpoints with their information on the route URL.
- On-the-fly config updater - Update config using the endpoints of Kestrel server!

### Modules
Modules are extensions, by which you can modify most of the core parts (excluding crucial core files which are required to run assistant ofcourse) of your assistant with your own definitions. The definitions can be anything, starting from, displaying an ASCII dog on console window to changing the general logging systems.
These modules are created a Class library project on the HomeAssistant solution, then by adding a reference to the HomeAssistant project to use the interfaces. once it is done, you have define the interface you want to use, such as IMailClient interface for implementing methods for custom mail client (IMAP notifications etc) to ILoggerBase interface to modify the general logging system.
The module will be compiled and placed inside the `HomeAssistant/Modules/` directory and will automatically load at runtime of the assistant.
This way, without modifying the Core project, you can customize your Assistant! more Interface's will be added soon!

### Currently available modules
- Email IMAP Notifier - get notified of new emails u receive by taking control of the IMAP server push notifications. simply login your account and enable the push notifications service on config file.
- Email System - Login with your email accounts to send, receive, delete or even search emails in your inbox. Supports 50+ accounts at a time.
- Steam bot - control your assistant and get chat notifications on steam, moreover, let your assistant take control of your account so that you can set custom away message and Hour boost as well as card farm games on steam.
- Discord bot - allows to control tess using discord bot commands from home assistant discord channel.
- Youtube - download video only or sound only or both from a youtube link.
- Misc methods - some extra methods such as converting date/time to days etc.

**To be edited later on**

