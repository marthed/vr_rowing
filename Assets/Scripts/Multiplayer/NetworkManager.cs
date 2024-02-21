using RiptideNetworking;
using RiptideNetworking.Utils;
using UnityEngine;
using Singleton;

public class NetworkManager : Singleton<NetworkManager>
{
    public Server Server { get; private set; }

    [SerializeField] private ushort port;
    [SerializeField] private ushort maxClientCount;



    void Start()
    {
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        Server = new Server();
        Server.Start(port, maxClientCount);
        Server.ClientConnected += ClientConnected;
        Server.ClientDisconnected += ClientDisconnected;
        
    }

    void ClientConnected(object sender, ServerClientConnectedEventArgs args) {
        Debug.Log("Client connected: " +  args.Client.Id);
       
    }

    void ClientDisconnected(object sender, ClientDisconnectedEventArgs args) {
        Debug.Log("Disconnect!");
        Debug.Log("Client disconnected: " +  args.Id);
    }

    private void FixedUpdate() {
        Server.Tick();
    }

    private void OnApplicationQuit() {
        Server.Stop();
    }
}
