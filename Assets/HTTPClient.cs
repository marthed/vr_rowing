using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Singleton;
using System.Text;
using System.Text.RegularExpressions;
enum RequestType {
    GET,
    POST,
}

public class HTTPClient : Singleton<HTTPClient>
{

    [Header("Settings")]
    public string receivingServerAddress = "http://192.168.180.92";
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
        string url = receivingServerAddress + ":" + serverPort + "/" + route;
        Debug.Log("Sending request to: " + url);
        Debug.Log(data);
        _dataToSend = data;
        StartCoroutine(SendRequest(data, RequestType.POST, url));

    }

    public void SetExternalServerIp(string msg) {
        string ip = msg.Split(";")[1];

        string trimmedString = ip.Trim();
        string noNull = trimmedString.Replace("\0", "");
        string cleanedString = Regex.Replace(noNull, @"[^\u0020-\u007E]", "");
        
        Debug.Log($"Trimmed String: '{cleanedString}'");

        receivingServerAddress = "http://" + cleanedString;
    }
        

}
