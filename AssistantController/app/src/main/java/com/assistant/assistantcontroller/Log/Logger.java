package com.assistant.assistantcontroller.Log;

import android.view.Gravity;
import android.widget.Toast;

import com.assistant.assistantcontroller.MainActivity;

import java.io.File;
import java.io.FileOutputStream;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Date;

public class Logger {
	public static String LogFileName = "Log.txt";
	private String LogIdentifier;
	private static MainActivity Main;

	public Logger(String logIdentifier, MainActivity... main){
		if(main.length > 0){
			Main = main[0];
		}

		if(logIdentifier.isEmpty()){
			LogIdentifier = "UNKNOWN";
		}else{
			LogIdentifier = logIdentifier;
		}
	}

	public void Log(String logBody){
		if(logBody.isEmpty()){
			return;
		}

		File Dir = Main.getFilesDir();

		try {
			File file = new File(Dir + "/Logs/", LogFileName);
			if (file.getParentFile().mkdirs()) {
				file.createNewFile();
				FileOutputStream fos = new FileOutputStream(file);
				DateFormat df = new SimpleDateFormat("dd/MM/yy HH:mm:ss");
				Date dateobj = new Date();
				String logData = df.format(dateobj) + " | " + "[" + LogIdentifier + "]" + " | " + logBody;
				fos.write(logData.getBytes());
				fos.flush();
				fos.close();
			}
		} catch (Exception e) {
			e.printStackTrace();
		}
	}
}
