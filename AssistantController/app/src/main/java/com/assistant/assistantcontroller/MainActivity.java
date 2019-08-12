package com.assistant.assistantcontroller;

import androidx.appcompat.app.AppCompatActivity;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.widget.Button;
import android.widget.Toast;

import com.assistant.assistantcontroller.ApiResponse.CoreConfigModel;
import com.assistant.assistantcontroller.Log.Logger;
import io.reactivex.Single;
import io.reactivex.SingleObserver;
import io.reactivex.android.schedulers.AndroidSchedulers;
import io.reactivex.disposables.CompositeDisposable;
import io.reactivex.disposables.Disposable;
import io.reactivex.schedulers.Schedulers;
import retrofit2.Retrofit;
import retrofit2.adapter.rxjava2.RxJava2CallAdapterFactory;
import retrofit2.converter.gson.GsonConverterFactory;

public class MainActivity extends AppCompatActivity {
	private com.assistant.assistantcontroller.Log.Logger Logger = new Logger("MAIN");
	private Retrofit RetroFit;
	private CompositeDisposable DisposableContainer = new CompositeDisposable();
	private static final String API_KEY = "sadasd21edas21d1asd51a2Dasd12a";

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_main);

		RetroFit = new Retrofit.Builder().baseUrl(Constants.ApiBaseUrl).addConverterFactory(GsonConverterFactory.create()).
		addCallAdapterFactory(RxJava2CallAdapterFactory.create()).build();

		findViewById(R.id.getCoreConfigbttn).setOnClickListener(new View.OnClickListener(){
			@Override
			public void onClick(View v){
				getCoreConfig(API_KEY);
			}
		});
	}

	@Override
	protected void onDestroy() {
		if(DisposableContainer.size() <= 0){
			return;
		}

		if (!DisposableContainer.isDisposed()) {
			DisposableContainer.dispose();
		}
		super.onDestroy();
	}

	private void getCoreConfig(@org.jetbrains.annotations.NotNull String apiKey){
		if(apiKey.isEmpty()){
			MakeToast("The api key cannot be null or empty!");
			return;
		}

		ApiService service = RetroFit.create(ApiService.class);
		Single<CoreConfigModel> coreConfigModel = service.GetCoreConfig(apiKey);
		coreConfigModel.subscribeOn(Schedulers.io()).observeOn(AndroidSchedulers.mainThread()).subscribe(new SingleObserver<CoreConfigModel>() {
			@Override
			public void onSubscribe(Disposable d) {
				DisposableContainer.add(d);
			}

			@Override
			public void onSuccess(CoreConfigModel coreConfigModel) {
				if(coreConfigModel.responseCode != 200){
					MakeToast("Failed to request core config from assistant.");
					return;
				}

				MakeToast(coreConfigModel.result.ownerEmailAddress);
			}

			@Override
			public void onError(Throwable e) {
				MakeToast(e.getMessage());
			}
		});
	}

	public void MakeToast(String toastMsg){
		Toast toast = Toast.makeText(this, toastMsg, Toast.LENGTH_LONG);
		toast.setGravity(Gravity.CENTER, 0, 0);
		toast.show();
	}
}
