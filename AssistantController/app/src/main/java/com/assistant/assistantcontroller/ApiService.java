package com.assistant.assistantcontroller;

import com.assistant.assistantcontroller.ApiResponse.CoreConfigModel;

import io.reactivex.Single;
import retrofit2.http.GET;
import retrofit2.http.Path;
import retrofit2.http.Query;

public interface ApiService {
	@GET("/api/config/coreconfig")
	Single<CoreConfigModel> GetCoreConfig(@Query("apiKey") String apiKey);
}
