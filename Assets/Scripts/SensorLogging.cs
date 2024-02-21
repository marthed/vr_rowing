using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomAttributes;

public class SensorLogging : MonoBehaviour
{

    [field: SerializeField, ReadOnlyAttribute] private Vector3 HandRotation { get; set;}
    [field: SerializeField, ReadOnlyAttribute] private Vector3 HeadRotation { get; set;}
    [field: SerializeField, ReadOnlyAttribute] private int NumberOfLeftButton { get; set;}
    [field: SerializeField, ReadOnlyAttribute] private int NumberOfRightButton { get; set;}

    [field: SerializeField, ReadOnlyAttribute] private float LeftToe { get; set;}
    [field: SerializeField, ReadOnlyAttribute] private float LeftHeel { get; set;}
    [field: SerializeField, ReadOnlyAttribute] private float RightToe { get; set;}
    [field: SerializeField, ReadOnlyAttribute] private float RightHeel { get; set;}



    private Vector3 _lastHandGyro;
    private Vector3 _lastHeadGyro;



    public void LogButtonPress(int left, int right) {
        NumberOfLeftButton += left;
        NumberOfRightButton += right;
    }

    public void LogFeetPressure(float lt, float lh, float rt, float rh) {
        LeftToe += lt;
        LeftHeel += lh;
        RightToe += rt;
        RightHeel += rh;
    }

    public void LogHandRotation(Vector3 nextGyro) {
        
        float diffX = Mathf.Abs((nextGyro.x - _lastHandGyro.x + 180) % 360 - 180);
        float diffY = Mathf.Abs((nextGyro.y - _lastHandGyro.y + 180) % 360 - 180);
        float diffZ = Math.Abs((nextGyro.z - _lastHandGyro.z + 180) % 360 - 180);

        _lastHandGyro = nextGyro;

        HandRotation += new Vector3(diffX, diffY, diffZ);

    }

    public void LogHeadRotation(Vector3 nextGyro) {
        
        float diffX = Mathf.Abs((nextGyro.x - _lastHeadGyro.x + 180) % 360 - 180);
        float diffY = Mathf.Abs((nextGyro.y - _lastHeadGyro.y + 180) % 360 - 180);
        float diffZ = Math.Abs((nextGyro.z - _lastHeadGyro.z + 180) % 360 - 180);

        _lastHeadGyro = nextGyro;

        HeadRotation += new Vector3(diffX, diffY, diffZ);

    }

    public void Flush() {
        HandRotation = Vector3.zero;
        HeadRotation = Vector3.zero;
        NumberOfLeftButton = 0;
        NumberOfRightButton = 0;
        LeftToe = 0;
        LeftHeel = 0;
        RightToe = 0;
        RightHeel = 0;
        _lastHandGyro = Vector3.zero;
        _lastHeadGyro = Vector3.zero;
    }

    public string GetMetricOrder() {
        return "hand x, hand y, hand z, left button, right button, head x, head y, head z, left toe, left heel, right toe, right heel";
    }
    
    public string GetMetricsAsString() {
        return HandRotation.x + "," + HandRotation.y + "," + HandRotation.z + "," + NumberOfLeftButton + "," + NumberOfRightButton + "," + HeadRotation.x + "," + HeadRotation.y + "," + HeadRotation.z + "," + LeftToe + "," + LeftHeel + "," + RightToe + "," + RightHeel;
    }
}
