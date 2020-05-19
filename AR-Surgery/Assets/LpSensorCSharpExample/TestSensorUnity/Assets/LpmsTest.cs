using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System;

using LpmsCSharpWrapper;

public class LpmsTest : MonoBehaviour {

    public Button objectResetButton;
    private GameObject dm;
    string lpmsSensor = "00:04:3E:4B:31:F6";

    // Use this for initialization
    void Start ()
    {
        // Initialize sensor manager
        dm = GameObject.Find("DataManager");
        if (dm != null)
        {
            Debug.Log("DataManager found!\n");
        }
        LpSensorManager.initSensorManager();

        // connects to sensor
        LpSensorManager.connectToLpms(LpSensorManager.DEVICE_LPMS_B2, lpmsSensor);

        // Wait for establishment of sensor1 connection
        while (LpSensorManager.getConnectionStatus(lpmsSensor) != 1)
        {
            System.Threading.Thread.Sleep(100);
        }
        Debug.Log("Sensor connected");

        // Sets sensor offset
        LpSensorManager.setOrientationOffset(lpmsSensor, LpSensorManager.LPMS_OFFSET_MODE_HEADING);
        Debug.Log("Offset set");
    }

    // Update is called once per frame
    void Update () {        
        if(LpSensorManager.getConnectionStatus(lpmsSensor) == LpSensorManager.SENSOR_CONNECTION_CONNECTED)
        {
            SensorData sd;
            unsafe
            {
                sd = *((SensorData*)LpSensorManager.getSensorData(lpmsSensor));
            }
            //Debug.Log("Timestamp: " + sd.timeStamp + " q: " + sd.qw + " " + sd.qx + " " + sd.qy + " " + sd.qz);
            Quaternion q = new Quaternion( sd.qx, sd.qz, sd.qy, sd.qw);
            transform.rotation = q;
            if(dm != null)
            {
                dm.GetComponent<DataManager>().set_Iniertial(this.transform.rotation);
            }
        }
        
    }

    void OnDestroy()
    {
        Debug.Log("PrintOnDestroy");
        LpSensorManager.disconnectLpms(lpmsSensor);
        // Destroy sensor manager and free up memory
        LpSensorManager.deinitSensorManager();
    }
}
