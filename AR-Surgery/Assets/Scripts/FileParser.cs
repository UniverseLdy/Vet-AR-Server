using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class FileParser : MonoBehaviour
{

    private string path = "data.txt";
    public string data;
    // Start is called before the first frame update
    void Start()
    {
        data = "Empty data";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Read_Data()
    {
        StreamReader rd = new StreamReader(path);
        data = rd.ReadToEnd();
    }

    public void Send_Data()
    {
        GameObject server = GameObject.Find("Server");
        if(server != null)
        {
            server.GetComponent<Server>().SendHandleData(data);
        }
    }
}
