using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;



public class NativeShare : MonoBehaviour
{
	private bool _applicationFocus = false;
	private bool _appProcessing = false;
	
	[Tooltip("Optional assignment, displays additional text useful for troubleshooting.")]
	public Text infoText;


	#if !UNITY_EDITOR && UNITY_IOS
		[System.Runtime.InteropServices.DllImport("__Internal")]
		private static extern void _Native_Share_iOS(string file);
	#endif
	
	private void  Start () 
	{
		GetComponent<Button>().onClick.AddListener(Share);
		if (infoText != null)
		{
			#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
				infoText.text = "Try pressing the button on your device to start the native sharing";
			#else
				infoText.text = "You are running in the editor. Native sharing will only show up when running on the actual device";
			#endif
		}
	}

	private void OnApplicationFocus (bool isFocused) 
	{
		_applicationFocus = isFocused;
	}
	
	private void Share ()
	{
		if (infoText != null)
			infoText.text = "Button pressed.";
		
		if (_appProcessing) 
			return;
		
		#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR) 
			StartCoroutine(NativeSharing(TakeScreenshot()));
		#endif
	}
	private string TakeScreenshot()
	{
		var screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
		screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
		screenShot.Apply();
		var filepath = Path.Combine(Application.temporaryCachePath, "screenshot.png");
		File.WriteAllBytes(filepath, screenShot.EncodeToPNG());
		Destroy(screenShot);
		return filepath;
	}
	private IEnumerator NativeSharing (string filepath)
	{
		_appProcessing = true;
		
		#if UNITY_ANDROID && !UNITY_EDITOR
		{
			var unity = new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
			var currentActivity = unity.GetStatic<AndroidJavaObject> ("currentActivity");
			var intentClass = new AndroidJavaClass ("android.content.Intent");
			var intentObject = new AndroidJavaObject ("android.content.Intent");
			intentObject.Call<AndroidJavaObject> ("setAction", intentClass.GetStatic<string> ("ACTION_SEND"));
			var fileObject = new AndroidJavaObject("java.io.File", filepath);
			var fileProviderClass = new AndroidJavaClass("androidx.core.content.FileProvider");
			var providerParams = new object[3];
			providerParams[0] = currentActivity;
			providerParams[1] = (Application.identifier+".provider");
			providerParams[2] = fileObject;
			var uriObject = fileProviderClass.CallStatic<AndroidJavaObject>("getUriForFile", providerParams);
			intentObject.Call<AndroidJavaObject> ("putExtra", intentClass.GetStatic<string> ("EXTRA_STREAM"), uriObject);
			intentObject.Call<AndroidJavaObject> ("setType", "image/png");
			intentObject.Call<AndroidJavaObject> ("addFlags", intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION") );
			var chooser = intentClass.CallStatic<AndroidJavaObject> ("createChooser", intentObject, null);
			currentActivity.Call ("startActivity", chooser);
		}
		#elif UNITY_IOS && !UNITY_EDITOR
			_Native_Share_iOS(filepath);
		#endif
		
		yield return new WaitUntil (() => _applicationFocus);
		
		if (infoText != null)
			infoText.text = "Screenshot shared!";
		
		_appProcessing = false;
	}
}