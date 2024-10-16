using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Text;
using System.Net.NetworkInformation;
using UnityEngine.Events;

public class UDPServer : MonoBehaviour
{
    #region "Inspector Members"
    [SerializeField] int port = 8080;
    [SerializeField] bool hideLogs = false;

    [SerializeField] string oculusQuestIp = "192.168.209.218";

    [Header("Sensor Events")]
    [Tooltip("Event is triggered when UDPServer receives sensor data for steering")]
    public UnityEvent<string> OnHandInput;

    [Tooltip("Event is triggered when UDPServer receives sensor data for steering")]
    public UnityEvent<string> OnFeetInput;

    [Tooltip("Event is triggered when UDPServer receives sensor data for speed")]
    public UnityEvent<string> OnSpeedInput;
   [Tooltip("Event is triggered when UDPServer is requested to change steering method")]
   public UnityEvent<string> OnMethodChange;
   
   [Tooltip("Event is triggered when UDPServer receivs the external server ip")]
   public UnityEvent<string> OnIpReceived;
   
   [Tooltip("Event is triggered when UDPServer is requested to change travel method")]
   public UnityEvent<string> OnTravelChange;


    [Tooltip("Event is triggered when UDPServer is requested to go to next game state")]
    public UnityEvent<string> OnNextGameState;

    [Tooltip("Event is triggered when UDPServer is requested to change track")]
    public UnityEvent<string> OnTrackChange;

    [Tooltip("Event is triggered when UDPServer is requested to toggle head method axis")]
    public UnityEvent OnToggleInverted;

    [Tooltip("Event is triggered when UDPServer is requested to toggle control on desktop")]
    public UnityEvent OnToggleControlDesktop;

    #endregion

    #region "Private Members"

    private float moveDistance = 1f;
    private int frameWait = 3;
    private int maxClients = 3;
    Socket udp = null;
    int idAssignIndex = 0;
    Dictionary<EndPoint, UDPClient2> clients;
    #endregion

    void Start()
    {
        clients = new Dictionary<EndPoint, UDPClient2>();

        string wifiIpAddress = string.Empty;


        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface iface in interfaces)
        {
            if (iface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && iface.OperationalStatus == OperationalStatus.Up)
            {
                foreach (UnicastIPAddressInformation ip in iface.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        wifiIpAddress = ip.Address.ToString();
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(wifiIpAddress))
                {
                    break;
                }
            }
        }
        Debug.Log("Wifi ip address: " + wifiIpAddress);
        string testThisIpAdress = !string.IsNullOrEmpty(wifiIpAddress) ? wifiIpAddress : oculusQuestIp;  // Oculus Quest Ip

        IPAddress ipAddress = IPAddress.Parse(testThisIpAdress);
        IPEndPoint endPoint = new IPEndPoint(ipAddress, port);


        Debug.Log("Wi-Fi IP Address: " + testThisIpAdress);
        Debug.Log("Server IP Address: " + endPoint.Address);
        Debug.Log("Port: " + port);
        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Bind(endPoint);
        udp.Blocking = false;
    }

    void Update()
    {
        if (Time.frameCount % frameWait == 0 && udp != null && udp.Available != 0)
        {
            byte[] packet = new byte[64];
            EndPoint sender = new IPEndPoint(IPAddress.Any, port);

            int rec = udp.ReceiveFrom(packet, ref sender);
            string info = Encoding.Default.GetString(packet);

            if (!hideLogs)
            {
                Debug.Log("Server received: " + info);
            }


            HandleUDPMessage(info);


            if (info[0] == 'n' && clients.Count < maxClients)
                HandleNewClient(sender, info);
            else if (info[0] == 'e')
                DisconnectClient(sender, info);
            else if (rec > 0)
            {
                string id = Parser.ParseID(info);
                int seqNumber = Parser.ParseSequenceNumber(info);
                if (id == "" || seqNumber == -1) return;

                string userInput = Parser.ParseInput(info);
                if (userInput != "")
                    HandleUserMoveInput(sender, userInput, seqNumber);
            }
        }
    }

    void HandleUDPMessage(string msg) {
        switch(msg.Substring(0, 2)) {
            case "ip":
                OnIpReceived?.Invoke(msg);
                break;
            case "an":
                InfoBoard.Instance.SetSpeedText("Speed: " + msg);
                OnSpeedInput?.Invoke(msg);
                break;
            case "fs":
                OnFeetInput?.Invoke(msg);
                InfoBoard.Instance.SetFeetText("Feet: " + msg);

                break;
            case "bg": // button gyro
                OnHandInput?.Invoke(msg);
                InfoBoard.Instance.SetHandsText("Hands:" + msg);
                break;
            case "me":
                OnMethodChange?.Invoke(msg);
                break;
            case "tr":
                OnTravelChange?.Invoke(msg);
                break;
            case "gs":
                OnNextGameState?.Invoke(msg);
                HTTPClient.Instance.PostRequest("state", "" + GameManager.Instance.GetCurrentGameState());
                break;
            case "tk":
                OnTrackChange?.Invoke(msg);
                break;
            case "in":
                OnToggleInverted?.Invoke();
                break;
            case "cd":
                OnToggleControlDesktop?.Invoke();
                break;
        }
    }

    void HandleNewClient(EndPoint addr, string data)
    {
        string id = "c" + idAssignIndex++ + "t";
        Debug.Log("Handling new client with id " + id);
        SendPacket("a " + id, addr);

        Vector3 pos = Parser.ParseInitialPosition(data);
        clients.Add(addr, new UDPClient2(id, pos));
        SendPositionToAllClients();
    }

    void DisconnectClient(EndPoint sender, string data)
    {
        if (clients.ContainsKey(sender))
            clients.Remove(sender);
        Broadcast(data);
    }

    void Broadcast(string data)
    {
        foreach (KeyValuePair<EndPoint, UDPClient2> p in clients)
            SendPacket(data, p.Key);
    }

    void HandleUserMoveInput(EndPoint client, string userInput, int seqNumber)
    {
        if (!clients.ContainsKey(client) || clients[client].lastSeqNumber > seqNumber)
            return;

        if (!clients[client].history.ContainsKey(seqNumber))
        {
            clients[client].UpdateStateHistory(seqNumber);
            clients[client].lastSeqNumber = seqNumber;
        }
        UpdatePosition(client, userInput);
        /* so that clients see newly connected clients */
        SendPositionToAllClients();
    }

    void UpdatePosition(EndPoint addr, string input)
    {
        if (input.Equals("a"))
            clients[addr].position.x -= moveDistance;
        else if (input.Equals("d"))
            clients[addr].position.x += moveDistance;
        else if (input.Equals("w"))
            clients[addr].position.y += moveDistance;
        else if (input.Equals("s"))
            clients[addr].position.y -= moveDistance;
    }

    void SendPositionToAllClients()
    {
        foreach (KeyValuePair<EndPoint, UDPClient2> p in clients)
            foreach (KeyValuePair<EndPoint, UDPClient2> p2 in clients)
                SendPacket(p2.Value.ToString(), p.Key);
    }

    Vector3 ParsePosition(Match match)
    {
        float x, y, z;
        float.TryParse(match.Groups["x"].Value, out x);
        float.TryParse(match.Groups["y"].Value, out y);
        float.TryParse(match.Groups["z"].Value, out z);
        return new Vector3(x, y, z);
    }

    void SendPacket(string str, EndPoint addr)
    {
        byte[] arr = Encoding.ASCII.GetBytes(str);
        udp.SendTo(arr, addr);
    }
}