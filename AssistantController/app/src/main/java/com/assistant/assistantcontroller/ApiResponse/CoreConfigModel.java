package com.assistant.assistantcontroller.ApiResponse;
import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

public class CoreConfigModel {
	@SerializedName("Result")
	@Expose
	public CoreConfigResult result;
	@SerializedName("Response")
	@Expose
	public String response;
	@SerializedName("ResponseCode")
	@Expose
	public Integer responseCode;
	@SerializedName("DateTime")
	@Expose
	public String dateTime;
}
