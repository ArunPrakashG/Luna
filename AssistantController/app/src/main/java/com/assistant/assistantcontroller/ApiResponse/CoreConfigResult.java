package com.assistant.assistantcontroller.ApiResponse;

import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

import java.util.List;

public class CoreConfigResult {

	@SerializedName("RelayPins")
	@Expose
	public List<Integer> relayPins = null;
	@SerializedName("IRSensorPins")
	@Expose
	public List<Integer> iRSensorPins = null;
	@SerializedName("AutoRestart")
	@Expose
	public Boolean autoRestart;
	@SerializedName("AutoUpdates")
	@Expose
	public Boolean autoUpdates;
	@SerializedName("EnableConfigWatcher")
	@Expose
	public Boolean enableConfigWatcher;
	@SerializedName("EnableModuleWatcher")
	@Expose
	public Boolean enableModuleWatcher;
	@SerializedName("EnableModules")
	@Expose
	public Boolean enableModules;
	@SerializedName("UpdateIntervalInHours")
	@Expose
	public Integer updateIntervalInHours;
	@SerializedName("KestrelServerUrl")
	@Expose
	public String kestrelServerUrl;
	@SerializedName("ServerAuthCode")
	@Expose
	public Integer serverAuthCode;
	@SerializedName("PushBulletLogging")
	@Expose
	public Boolean pushBulletLogging;
	@SerializedName("TCPServerPort")
	@Expose
	public Integer tCPServerPort;
	@SerializedName("TCPServer")
	@Expose
	public Boolean tCPServer;
	@SerializedName("KestrelServer")
	@Expose
	public Boolean kestrelServer;
	@SerializedName("GPIOSafeMode")
	@Expose
	public Boolean gPIOSafeMode;
	@SerializedName("DisplayStartupMenu")
	@Expose
	public Boolean displayStartupMenu;
	@SerializedName("EnableGpioControl")
	@Expose
	public Boolean enableGpioControl;
	@SerializedName("Debug")
	@Expose
	public Boolean debug;
	@SerializedName("ZomatoApiKey")
	@Expose
	public String zomatoApiKey;
	@SerializedName("OwnerEmailAddress")
	@Expose
	public String ownerEmailAddress;
	@SerializedName("EnableFirstChanceLog")
	@Expose
	public Boolean enableFirstChanceLog;
	@SerializedName("EnableTextToSpeech")
	@Expose
	public Boolean enableTextToSpeech;
	@SerializedName("MuteAssistant")
	@Expose
	public Boolean muteAssistant;
	@SerializedName("OpenWeatherApiKey")
	@Expose
	public String openWeatherApiKey;
	@SerializedName("GitHubToken")
	@Expose
	public String gitHubToken;
	@SerializedName("PushBulletApiKey")
	@Expose
	public String pushBulletApiKey;
	@SerializedName("AssistantEmailId")
	@Expose
	public String assistantEmailId;
	@SerializedName("AssistantDisplayName")
	@Expose
	public String assistantDisplayName;
	@SerializedName("AssistantEmailPassword")
	@Expose
	public Object assistantEmailPassword;
	@SerializedName("ProgramLastStartup")
	@Expose
	public String programLastStartup;
	@SerializedName("ProgramLastShutdown")
	@Expose
	public String programLastShutdown;
	@SerializedName("CloseRelayOnShutdown")
	@Expose
	public Boolean closeRelayOnShutdown;
}
