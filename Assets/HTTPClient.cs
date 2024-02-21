using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Singleton;
using System.Text;
using System;

enum RequestType {
    GET,
    POST,
}

public class HTTPClient : Singleton<HTTPClient>
{

    [Header("Settings")]
    public string serverAddress = "http://192.168.209.92";
    public int serverPort = 3000;
    private string _dataToSend = "";

    private UnityWebRequest CreateRequest(string path, RequestType type = RequestType.POST, string data = null) {
       UnityWebRequest request =  new UnityWebRequest(path, type.ToString());

       if (data != null) {
        var bodyRaw = Encoding.UTF8.GetBytes(data);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
       }

       request.downloadHandler = new DownloadHandlerBuffer();
       request.SetRequestHeader("Content-Type", "text/plain");
       return request;
    }

    private IEnumerator SendRequest(string data, RequestType type, string url)
    {
        UnityWebRequest request = CreateRequest(url, type, data);
        yield return request.SendWebRequest();
    }

    public void PostRequest(string route, string data)
    {
        string url = serverAddress + ":" + serverPort + "/" + route;
        Debug.Log("Sending request to: " + url);
        Debug.Log(data);
        _dataToSend = data;
        StartCoroutine(SendRequest(data, RequestType.POST, url));

    }
        

}
