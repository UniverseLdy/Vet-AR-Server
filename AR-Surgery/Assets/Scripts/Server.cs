﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Using the UDP protocol for transport layer.
/// </summary>

public class ServerClient
{
    public int connectionId;
    public string PlayerName;
    public Quaternion Rotation;
}
public class Server : MonoBehaviour
{
    private const int MAX_CONNECTION = 100;

    private int port = 5701;

    private int HostId;
    private int WebHostID;

    private int ReliableChannel;
    private int UnreliableChannel;

    private bool is_started = false;
    private byte error;

    private List<ServerClient> clients = new List<ServerClient>();

    private float lastRotationUpdate;
    private float UpdateRate = 0.05f;

    public bool BoradCasting;
    private bool BoradCasting_Changed;
    private void Start()
    {
        NetworkTransport.Init();
        ConnectionConfig CC = new ConnectionConfig();

        ReliableChannel = CC.AddChannel(QosType.Reliable);
        UnreliableChannel = CC.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(CC,MAX_CONNECTION);

        HostId = NetworkTransport.AddHost(topo, port, null);

        WebHostID = NetworkTransport.AddWebsocketHost(topo, port, null);

        is_started = true;
        Debug.Log("Server is Ready");
        BoradCasting = false;
    }

    private void Update()
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
            case NetworkEventType.ConnectEvent:
                Debug.Log("Player:" + connectionId + " has connected!");
                Onconnection(connectionId);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer,0,dataSize);
                //Debug.Log("Receiving from" + connectionId + " : " + msg);
                //Debug.Log("length is: " + msg.Length);
                string[] splitData = msg.Split('|');

                switch (splitData[0])
                {
                    case "NAMEIS":
                        OnNameIs(connectionId,splitData[1]);
                        break;

                    case "MYROT":
                        OnMyRot(connectionId,splitData[1]);
                        break;

                    case "CALIS":
                        OnCALIS(connectionId, splitData[1]);
                        break;

                    default:
                        Debug.Log("Invalid Message : " + msg);
                        break;
                }
                break;

                break;
            case NetworkEventType.DisconnectEvent:
                OnDisconnection(connectionId);
                Debug.Log("Player:" + connectionId + " has disconnected");
                break;

            case NetworkEventType.BroadcastEvent:

                break;
        }
        //Ask for rotation update
        if (BoradCasting)
        {
            if (Time.time - lastRotationUpdate > UpdateRate)
            {
                if (BoradCasting_Changed)
                {
                    Send("ONBC|", ReliableChannel, clients);
                    BoradCasting_Changed = false;
                }
                lastRotationUpdate = Time.time;
                string m = "ROT|";
                GameObject datamanager = GameObject.Find("DataManager");
                Quaternion data = datamanager.GetComponent<DataManager>().RealSense_Rotation;
                m += data.x.ToString() + '%' + data.y.ToString() + '%' + data.z.ToString() + '%' + data.w.ToString() + '%';
                data = datamanager.GetComponent<DataManager>().Inertia_Rotation;
                m += data.x.ToString() + '%' + data.y.ToString() + '%' + data.z.ToString() + '%' + data.w.ToString() + '%';
                m += '|';
                Send(m, UnreliableChannel, clients);
            }
        }
        if(!BoradCasting)
        {
            if (BoradCasting_Changed)
            {
                Send("OFFBC|", ReliableChannel, clients);
                BoradCasting_Changed = false;
            }
        }
        
    }

    private void OnCALIS(int connectionId, string data)
    {
        string[] rots = data.Split('%');
        Quaternion rot = new Quaternion(float.Parse(rots[0]), float.Parse(rots[1]), float.Parse(rots[2]), float.Parse(rots[3]));
        GameObject Handle = GameObject.Find("RS_Handle");
        if(Handle!= null)
        {
            //Handle.GetComponent<Handle>().Set_Cali(rot);
            //Handle.GetComponent<Handle>().Calibrated = true;
        }
    }

    private void Onconnection(int connectionid)
    {
        //Add this player to a list;
        ServerClient c = new ServerClient();
        c.connectionId = connectionid;
        c.PlayerName = "Default";
        clients.Add(c);

        //When the player joins this server, tell him his id;
        //Ask for name and send this name to all the other clients.

        string msg = "ASKNAME|" + connectionid + "|";
        foreach(ServerClient sc in clients)
        {
            msg += sc.PlayerName + '%' + sc.connectionId + "|";
        }
        Send(msg, ReliableChannel, connectionid);
    }
    private void OnDisconnection(int cnnId)
    {
        clients.Remove(clients.Find(x => x.connectionId == cnnId));
        Send("DC|" + cnnId + "|", ReliableChannel, clients);
    }
    private void OnNameIs(int cnnId, string PlayerName)
    {

        clients.Find(x => x.connectionId == cnnId).PlayerName = PlayerName;
        //Debug.Log("NAME:" + PlayerName);
        //Debug.Log("Length" + PlayerName.Length);
        string msg = "CNN|" + PlayerName + '|' + cnnId + '|';
        Send(msg, ReliableChannel, clients);
    }

    private void OnMyRot(int cnnId,string data)
    {
        return;
    }
    private void Send(string message,int channelId, int cnnId)
    {
        List<ServerClient> c = new List<ServerClient>();
        c.Add(clients.Find(x => x.connectionId == cnnId));
        Send(message, channelId, c);
    }

    private void Send(string message,int channelId, List<ServerClient> c)
    {
        Debug.Log("Sending:" + message);
        byte[] msg = Encoding.Unicode.GetBytes(message);
        foreach (ServerClient sc in clients)
        {
            NetworkTransport.Send(HostId, sc.connectionId, channelId, msg, msg.Length * sizeof(char), out error);
        }
    }
    public void Set_BoradCasting()
    {
        BoradCasting = !BoradCasting;
        BoradCasting_Changed = true;
    }

    public void Calibrate()
    {
        GameObject DataManager = GameObject.Find("DataManager");
        int Cal_x, Cal_y, Cal_z;
        bool Reversed;
        if (DataManager != null)
        {
            Cal_x = DataManager.GetComponent<DataManager>().cal_x;
            Cal_y = DataManager.GetComponent<DataManager>().cal_y;
            Cal_z = DataManager.GetComponent<DataManager>().cal_z;
            Reversed = DataManager.GetComponent<DataManager>().XZ_Reversed;
        }
        else
        {
            Cal_x = 1;
            Cal_y = 1;
            Cal_z = 1;
            Reversed = false;
        }
        string msg = "CAL|" + Cal_x.ToString() + '%' + Cal_y.ToString() + '%' + Cal_z.ToString() + '%' + Reversed.ToString() + '%' + '|';
        Send(msg,ReliableChannel,clients);
    }

    public void MoveDrill()
    {
        Send("MD" + '|', ReliableChannel, clients);
    }

    public void SendHandleData(string data)
    {
        string msg = "DAT|" + data + '|';
        Send(msg, ReliableChannel, clients);
    }

    public void Send_Adjust(float x, float y, float z,float x_rot,float y_rot,float z_rot, float scale)
    {
        string msg = "ADJ|";
        msg += x.ToString() + '%' + y.ToString() + '%' + z.ToString() + '%' + x_rot.ToString()+ '%' + y_rot.ToString() + '%' + z_rot.ToString() +'%'+ scale.ToString() + '|';
        Send(msg, ReliableChannel, clients);
    }
    public  void SendNailData(bool Nail_Status)
    {
        bool status = Nail_Status;
        int code;
        if (status) code = 1;
        else code = 0;
        string msg = "NAIL|" + code.ToString() + '|';
        Send(msg, ReliableChannel, clients);
    }
}
