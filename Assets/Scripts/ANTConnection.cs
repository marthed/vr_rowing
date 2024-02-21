using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ANT_Managed_Library;
using System;

public class ANTConnection : MonoBehaviour
{
    // Start is called before the first frame update
    AntChannel speedChannel;

    //variable use for speed display
    int prevRev;
    int prevMeasTime = 0;
    void Start()
    {
        if (AntManager.Instance.device == null) {
            AntManager.Instance.Init();
            Debug.Log("Init ANT!");
        }
        AntManager.Instance.onDeviceResponse += OnDeviceResponse;
        AntManager.Instance.onSerialError += OnSerialError; //if usb dongle is unplugged for example


        speedChannel = AntManager.Instance.OpenChannel(ANT_ReferenceLibrary.ChannelType.BASE_Slave_Receive_0x00, 2, 0, 123, 0, 57, 8118, false); //bike speed Display
        speedChannel.onChannelResponse += OnChannelResponse;
        speedChannel.onReceiveData += ReceivedSpeedAntData;
        
    }

    void OnDeviceResponse(ANT_Response response) {
        //InfoBoard.Instance.SetInforBoardAntText("device:" + response.getMessageID().ToString());
        Debug.Log("device:" + response.getMessageID().ToString());
    }

    void OnChannelResponse(ANT_Response response)
    {

        if (response.getChannelEventCode() == ANT_ReferenceLibrary.ANTEventID.EVENT_RX_FAIL_0x02) {
        // InfoBoard.Instance.SetInforBoardAntText("channel " + response.antChannel.ToString() + " " + "RX fail");
            Debug.Log("channel " + response.antChannel.ToString() + " " + "RX fail");
        } else if (response.getChannelEventCode() == ANT_ReferenceLibrary.ANTEventID.EVENT_RX_FAIL_GO_TO_SEARCH_0x08) {
          //  InfoBoard.Instance.SetInforBoardAntText("channel " + response.antChannel.ToString() + " " + "Go to search");
            Debug.Log("channel " + response.antChannel.ToString() + " " + "Go to search"); 
        } else if (response.getChannelEventCode() == ANT_ReferenceLibrary.ANTEventID.EVENT_TX_0x03) {
           // InfoBoard.Instance.SetInforBoardAntText("channel " + response.antChannel.ToString() + " " + "Tx: (" + response.antChannel.ToString() + ")");
            Debug.Log("channel " + response.antChannel.ToString() + " " + "Tx: (" + response.antChannel.ToString() + ")");
        }
        else
           // InfoBoard.Instance.SetInforBoardAntText("channel " + response.antChannel.ToString() + " " + response.getChannelEventCode());
            Debug.Log("channel " + response.antChannel.ToString() + " " + response.getChannelEventCode());
    }

    void OnSerialError(SerialError serialError)
    {
        Debug.Log("Error:" + serialError.error.ToString());
       // InfoBoard.Instance.SetInforBoardAntText("Error:" + serialError.error.ToString());

        //attempt to auto reconnect if the USB was unplugged
        if (serialError.error == ANT_Device.serialErrorCode.DeviceConnectionLost)
        {
            foreach (AntChannel channel in AntManager.Instance.channelList)
                channel.PauseChannel();

            StartCoroutine("Reconnect", serialError.sender.getSerialNumber());
        }

    }

    void ReceivedSpeedAntData(Byte[] data)
    {
        //output the data to our log window
        string dataString = "RX: ";
        foreach (byte b in data)
            dataString += b.ToString("X") + " ";

        Debug.Log(dataString);

        //speed formula as described in the ant+ device profile doc
        int currentRevCount = (data[6]) | data[7] << 8;

        if (currentRevCount != prevRev && prevRev > 0)
        {
            int currentMeasTime = (data[4]) | data[5] << 8;
            float speed = (2.070f * (currentRevCount - prevRev) * 1024) / (currentMeasTime - prevMeasTime);
            speed *= 3.6f;
            prevMeasTime = currentMeasTime;
            Debug.Log("speed: " + speed.ToString("F2") + "km/h");
        }

        prevRev = currentRevCount;


    }

    IEnumerator Reconnect(uint serial)
    {
        Debug.Log("Looking for usb device: " + serial.ToString());
        // polling to try and find the USB device
        while (true)
        {

            if (ANT_Common.getNumDetectedUSBDevices() > 0)
            {
                ANT_Device device = new ANT_Device();
                if (device.getSerialNumber() == serial)
                {
                    Debug.Log("usb found!");
                    AntManager.Instance.Reconnect(device);
                    foreach (AntChannel channel in AntManager.Instance.channelList)
                        channel.ReOpen();

                    yield break;
                }
                else
                    device.Dispose();

            }

            yield return new WaitForSeconds(0.1f);
        }

    }



}

