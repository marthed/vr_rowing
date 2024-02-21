using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Singleton;
using System.Text;



public class HTTPServer : Singleton<HTTPServer>
{

    [Header("Settings")]
    public string address = "http://192.168.54.92";
    public int port = 3000;
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

    private IEnumerator SendRequest(string data, RequestType type)
    {
        string url = address + ":" + port;
        UnityWebRequest request = CreateRequest(url, type, data);
        yield return request.SendWebRequest();
    }

    public void PostRequest(string data)
    {

        string url = address + ":" + port;
        Debug.Log("Sending request to: " + url);
        Debug.Log(data);
        _dataToSend = data;
        StartCoroutine(SendRequest(data, RequestType.POST));

    }
        

}
