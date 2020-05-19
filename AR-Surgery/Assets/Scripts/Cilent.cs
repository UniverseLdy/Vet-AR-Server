using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class Cilent : MonoBehaviour
{
    // Start is called before the first frame update
    int connection_ID;
    private int Client_ID;
    private const int MAX_CONNECTION = 100;

    //"127.0.0.1" is the local Ip, change this to the ip address of the computer running the server scene when hololens is used.
    private string IP_Address = "127.0.0.1";
    int Reliable_Channel_ID;
    int Unreliable_Channel_ID;

    private float Connection_Time;
    int Host_ID;
    int Web_Host_ID;
    int Socket_Port = 5701;
    public bool is_started;
    byte error;

    private string playername;
    void Start()
    {
        is_started = false;
        Connection_Time = 0;
        connection_ID = -1;
        playername = "Hololens User";
        Client_ID = -1;
    }

    // Update is called once per frame
    void Update()
    {
        if (!is_started) return;

        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData)
        {
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving from" + connectionId + " : " + msg);
                //Debug.Log("length is: " + msg.Length);
                string[] splitData = msg.Split('|');

                switch (splitData[0])
                {
                    case "ASKNAME":
                        On_AskName(splitData[1]);
                        break;

                    case "CNN":
                        //When a player is registered successully at the server end
                        Debug.Log("Player: " + splitData[1] + "has joined!\n");
                        break;

                    case "DC":
                        //When a player is disconnected from the server end.
                        break;

                    case "DAT":
                        //When the server sends the information of the handle and cones to the hololens end
                        On_HandleData(splitData[1]);
                        break;
                    case "CAL":
                        //When the server sends the requirement of calibration.
                        On_Calibration(splitData[1]);
                        break;
                    default:
                        Debug.Log("Invalid Message : " + msg);
                        break;
                }
                break;

                break;
        }

    }

    public void Connect()
    {

        NetworkTransport.Init();


        ConnectionConfig config = new ConnectionConfig();

        Reliable_Channel_ID = config.AddChannel(QosType.Reliable);
        Unreliable_Channel_ID = config.AddChannel(QosType.Unreliable);

        HostTopology topology = new HostTopology(config, MAX_CONNECTION);


        Host_ID = NetworkTransport.AddHost(topology,0);

        connection_ID = NetworkTransport.Connect(Host_ID, "127.0.0.1", Socket_Port, 0, out error);
        if(connection_ID == -1)
        {
            Debug.Log("Unable to find the host!\n");
        }
        Connection_Time = Time.time;
        is_started = true;
    }

    public void Disconnect()
    {
        NetworkTransport.Disconnect(Host_ID, connection_ID, out error);
    }

    public void Send_Message(string msg)
    {
        byte[] buffer = Encoding.Unicode.GetBytes(msg);
        NetworkTransport.Send(Host_ID, connection_ID, Reliable_Channel_ID, buffer, msg.Length * sizeof(char), out error);

    }

    private void On_AskName(string data)
    {
        Client_ID = int.Parse(data);

        Send("NAMEIS|" + playername + "|", Reliable_Channel_ID);
    }

    private void Send(string message, int channelID)
    {
        Debug.Log("Sending to the server : " + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);

        NetworkTransport.Send(Host_ID, connection_ID, channelID, msg, message.Length * sizeof(char), out error);
    }

    private void On_HandleData(string data)
    {
        Debug.Log("Received Handle Data: " + data);

        // To do: set the local transforms of the handle and drills with the respect to the vuMark.
    }

    private void On_Calibration(string data)
    {
        Debug.Log("Received Calibration Data: " + data);

        //To do: Read the current orientation of the virtual handle to the server
    }
}
