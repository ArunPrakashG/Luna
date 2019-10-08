# HOME ASSISTANT

A simple console application written in C# using .NET Core framework with a single goal, provide you with assistance in your home!
turning on lights, and turning them off, getting weather update, email notifications and more!
Built and designed to run on raspberry pi 3 in headless and completely unattended mode.
able to handle errors smartly and do the needful on certain circumstances.

## Features
- Cross-platform - not just because its made on dotnet core, but because it is smart enough to know its platform and adapt accordingly.it can switch off various platform specific methods if it decteted that the current running os isn't what it is made to run on.
- Smart network switcher - during network issues or network connection loss, it can know about the change and switch to offline mode were all network related tasks will be stopped to prevent exceptions. when the network is back online, it can start the previously stopped tasks again.
- Modular - it can customised according to the user running it through the JSON config system whenever the user require. Further, program level customization require referenced projects or Module system. (discussed below)
- Config watcher - automatically update the core config and other config values if the config files inside the `Config` folder is changed/deleted/modified.
- JSON Config file - Customize every aspect of the assistant from the JSON config files present in the Config directory.
- Deep logging - it uses NLog under the hood, that means it has a logging system which is powerful as well as completly modular with the `NLog.config` configuration file.
- GPIO pin control - complete control over the raspberry pi Gpio pins, including all 1 to 40 pins and states input and output and their values high and low.
- Interactive console - console is a powerful tool in terms of controlling the assistant. many pre defined charecters are used to define various methods.
- Multi threaded - Assistant utilizes multi threading to perform at its best. it can run many tasks concurrently without delay.
- Task queue - task based functions, such as setting remainders etc will be added to a task queue, with their execution time and many such variables, providing an easy way to schedule the tasks.
- TTS service - text to speech system for various important notifications and updates.
- Updater - automatically check for updates and update assistant.
- Kestrel Http Server - Advanced REST Http server API with various endpoints to provide you with complete control over your assistant. also provides an API to develop scrips or 3rd party application to interact with assistant.
- Performence Counter - System to monitor CPU and RAM usage in real-time. [Currently only for Windows platforms]
- Swagger Documentation - swagger integretion allows the user to view every available endpoints with their information on the route URL.
- On-the-fly config updater - Update config using the endpoints of Kestrel server.
- Dynamic assembly loader - Load modules and integrate with core assistant process automatically from `Modules` directory during the run-time as well as during a pre-configured check on the directory on assistant startup.
- Polling system - generate events based on GPIO pin value change by using polling technique on GPIO pin value.
- Weather API - Get weather info on a specific location based on Zip/Pin code and the country code.
- Zip code Data - Fetch Post offices/taluks of a region based on its Zip/Pin code. 
- Morse code Generator - Generate morse code from the specified text.
- Morse code translator - Translate the generated morse code to Sound using Console beeps (Windows only) or Gpio pin values (Raspberry only)

### Modules
Modules are extensions, by which you can modify most of the core parts (excluding crucial core files which are required to run assistant ofcourse) of your assistant with your own definitions. The definitions can be anything, starting from, displaying an ASCII doggo on console window to changing the general logging systems.
These modules are created by adding reference to the Assistant project to use the required interfaces. once it is done, you have to define the interface you want to use, such as IMailClient interface for implementing methods for custom mail client (IMAP notifications etc) to Custom interfaces to modify the various parts of the assistant.
The module will be compiled and placed inside the `Assistant/Modules/` directory and will automatically load at runtime of the assistant.
This way, without modifying the Core project, you can customize your Assistant! more Interface's will be added soon!

### Currently available modules
NOTE - The module projects have been seperated from main project [this] and they will be linked in this readme file shortly.
- Email System - Login with your email accounts to send, receive, delete or even search emails in your inbox. Supports 50+ accounts at a time also, get notified of new emails you receive by taking control of the IMAP server push notifications. Enable the push notifications service on config file.
- [WIP] Steam bot - control your assistant and get chat notifications on steam, moreover, let your assistant take control of your account so that you can set custom away message and Hour boost as well as card farm games on steam.
- Discord bot - allows to control tess using discord bot commands from home assistant discord channel.
- Youtube - download video only or sound only or both from a youtube link.

### Assistant roadmap -> [Roadmap](https://github.com/SynergYFTW/HomeAssistant/projects/1)

### Assistant Folder Heirarchy
Each foldername has its own value, use this diagram to know which folders can be renamed.
![Diagram](Assistant/Resources/AssistantFolderHierarchy.jpeg)

### Contributions
This project was started as my hobby project, just so that i could learn more of C# and .NET Framework and work with Linux commands and get familier with Raspian system. I always try my best to write the best code i can write as far as my knowledge go, if you feel you can improve a certain part of the code, feel free to send a pull request, with the specification on how i can improve the part so that i can correct myself for future codes i write!
