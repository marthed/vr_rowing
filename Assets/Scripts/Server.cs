using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Singleton;


public class UDPClient
{
    public IPEndPoint Endpoint { get; set; }
    public string ID { get; set; }
}

public class Serve2r : Singleton<Serve2r>
{
    public int PORT = 1234;
    private UdpClient udpServer;

    private List<UDPClient> connectedClients;

    public void Start()
    {
        udpServer = new UdpClient(PORT);
        connectedClients = new List<UDPClient>();       
        StartCoroutine(StartServer());
        Debug.Log("UDP server started on port " + PORT);
    }

    IEnumerator StartServer() {
       yield return Listen();
    }

    public async Task Listen2()
{
    bool listen = true;

    while (listen)
    {
        try
        {
            Debug.Log("Waiting for incoming message");
            UdpReceiveResult result = await udpServer.ReceiveAsync();
            ProcessMessage(result.RemoteEndPoint, result.Buffer);
        }
        catch (Exception e)
        {
            Debug.Log("Error!");
            Debug.Log(e);
            listen = false;
        }
    }

    // Once the loop exits, you can choose to restart the server or perform any cleanup operations.
    // For example, you can call Start() again to restart the server:
    // Start();
}

    public async Task Listen() {

        bool listen = true;
 
        while (listen)
        {
            try {
            Debug.Log("Waiting for incoming message");
            UdpReceiveResult result = await udpServer.ReceiveAsync();
            ProcessMessage(result.RemoteEndPoint, result.Buffer);
            } catch (Exception e) {
                Debug.Log("Error!");
                Debug.Log(e);
                listen = false;
                
            }
        }

    } 

    private void ProcessMessage(IPEndPoint clientEndpoint, byte[] message)
    {
        string messageText = Encoding.ASCII.GetString(message);
        Debug.Log("Message received from " + clientEndpoint.Address + ":" + clientEndpoint.Port + ": " + messageText);

        byte[] responseData = Encoding.ASCII.GetBytes("OK All Goody!");

        udpServer.Send(responseData, responseData.Length, clientEndpoint);

    }

    private void OnDestroy() {
        udpServer.Close();
    }
}