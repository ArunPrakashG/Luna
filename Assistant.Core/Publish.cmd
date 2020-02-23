@echo off
dotnet publish -r linux-arm /p:ShowLinkerSizeComparison=true
pushd .\bin\Debug\netcoreapp2.2\linux-arm\publish
rem Update the commands with your password and device name. Also make sure the /home/pi/Desktop/HomeAssistant/AssistantCore folder exists on the Pi.
pscp -pw miniprakash -v -r .\*.* pi@raspberrypi:/home/pi/Desktop/HomeAssistant/Assistant.Core
rem use the following command instead to copy only the project files which is much quicker
rem pscp -pw "miniprakash" -v -r .\Assistant.Core.* pi@raspberrypi:/home/pi/Desktop/HomeAssistant/Assistant.Core
popd
