using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CustomAttributes;
using UnityEngine.Assertions;
using Oculus.Interaction.Input;
using UnityEngine.Events;
using System;

public enum SteeringMethod
{
    Feet,
    Head,
    Hand,
}

public enum TravelMode
{
    _2D,

    _3D,
}

public class RowLocomotionController : MonoBehaviour
{

    [Header("Locomotion Methods")]
    [SerializeField] public SteeringMethod steeringMethod = SteeringMethod.Feet;
    [SerializeField] public TravelMode travelMode = TravelMode._2D;

    [Header("General Settings")]
    public float speedScalar = 1f;
    public float upScalar = 1;

    [Header("Acceleration")]

    public int smoothSamples = 10;
    public float minAccThreshold = 1;
    public float maxAccThreshold = 3;

    public float handlebarForwardSpeedScalar = 10;
    public float handlebarTurnSpeedScalar = 10;
    public float handlebarUpSpeedScalar = 10;

    private int sampleCounter = 0;
    private float accBucket = 0;
    private float lastLeftHandDistance = 0;
    private float lastRightHandDistance = 0;

    [Header("Feet Method Settings")]
    public float feetForceTurnThreshold = 1000;
    public float feetForceTurnReduction = 3000;
    public float feetForceUpThreshold = 7700; // Amount of force from heels to go up
    public float feetForceUpReduction = 7000;
    public float feetUpTimeThreshold = 1.5f; // Time until going up is activated 

    [Header("Hand Method Settings")]
    public int handTurnForceReduction = 1000;
    public float secondsForCalibration = 3;
    public float handUpForceScalar = 1;

    [Header("Head Method Settings")]
    public int headAngleTurnThreshold = 20;
    public int headTurnForceReduction = 1;
    public int headTurnForceMax = 2;
    public int headAngleUpThreshold = 20;
    public int headAngleDownThreshold = 40;
    public float headUpForceScalar = 1;
    public float headForceUpMax = 10;
    public float headUpForceOffset = 10;
    public bool inverted = false;


    [Header("Control on Desktop Settings")]
    public bool controlOnDesktop = false;
    public float desktopSpeed = 20;
    public float desktopTurnForce = 1;
    public float desktopUpForce = 1;

    [Header("Sensor Events")]
    [Tooltip("Event is triggered when receiving sensor hand data for steering")]
    public UnityEvent<int, int, Vector3> OnHandInput;

    [Tooltip("Event is triggered when receiving sensor feet data for steering")]
    public UnityEvent<int, int, int, int> OnFeetInput;

    [Tooltip("Event is triggered when use")]
    public UnityEvent<Vector3> OnHeadInput;

    [Header("Measurements")]
    [field: SerializeField, ReadOnlyAttribute] private float TurnForce = 0;
    [field: SerializeField, ReadOnlyAttribute] private float UpForce = 0;
    [field: SerializeField, ReadOnlyAttribute] private float HandlebarSpeed { get; set; }
    [field: SerializeField, ReadOnlyAttribute] private float Speed { get; set; }

    [field: SerializeField, ReadOnlyAttribute] private Vector3 gyroscope = new Vector3(0, 0, 0);

    [field: SerializeField, ReadOnlyAttribute] private float TopSpeed = 0;
    [field: SerializeField, ReadOnlyAttribute] private float TopHandlebarSpeed = 0;


    [Header("Dependencies")]
    public Camera head;
    public GameObject boat;
    public AudioClip calibrationCompleted;
    public LeftHand leftHand;
    public RightHand rightHand;


    #region "Internal General"
    private Rigidbody _rb;
    private RowRefPoint _rowRefPoint;
    private Hand _leftHandTracker;
    private Hand _rightHandTracker;
    private bool _lostLeftHandConnection;
    private bool _lostRightHandConnection;

    #endregion

    #region "Internal Hand Method"
    private Vector3 gyroscopeOffset = new Vector3(0, 0, 0);
    private bool pendingCalibration = false;
    private float pendingCalibrationStartTime = 0;
    private bool calibrationIsComplete = false;
    private Vector3 rawGyroscope = new Vector3(0, 0, 0);
    private AudioSource _audioSource;
    #endregion

    #region "Internal Feet Method"
    private float feetUpTime = 0;
    #endregion


    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _audioSource = GetComponent<AudioSource>();
        _rowRefPoint = GetComponentInChildren<RowRefPoint>();
    }

    void Start()
    {
        InfoBoard.Instance.FollowMe(gameObject);
        _leftHandTracker = leftHand.gameObject.GetComponentInParent<Hand>();
        _rightHandTracker = rightHand.gameObject.GetComponentInParent<Hand>();

    }

    public void HandMethod(string message)
    {
        string[] input = message.Split(";"); // Since minus values exist;                

        int leftButton = int.Parse(input[1]) == 0 ? -1 : 0;
        int rightButton = int.Parse(input[2]) == 0 ? 1 : 0;


        if (leftButton == -1 && rightButton == 1)
        {
            CalibrateGyroscope();
        }
        else if (pendingCalibration && !calibrationIsComplete)
        { // When a button is not pressed
            CancelCalibrateGyroscope();
        }
        else if (pendingCalibration && calibrationIsComplete)
        { // When a button is not pressed
            pendingCalibration = false;
            calibrationIsComplete = false;
        }




        rawGyroscope.x = float.Parse(input[3]);
        rawGyroscope.y = float.Parse(input[4]);
        rawGyroscope.z = float.Parse(input[5]);

        gyroscope.x = rawGyroscope.x - gyroscopeOffset.x;
        gyroscope.y = rawGyroscope.y - gyroscopeOffset.y;
        gyroscope.z = rawGyroscope.z - gyroscopeOffset.z;


        OnHandInput?.Invoke(leftButton, rightButton, gyroscope);
        if (controlOnDesktop || steeringMethod != SteeringMethod.Hand)
        {
            return;
        }


        if (gyroscope.x > 20 || gyroscope.x < -20)
        {
            TurnForce = -1 * (gyroscope.x / handTurnForceReduction);
        }
        else
        {
            TurnForce = 0;
        }

        if (travelMode == TravelMode._3D)
        {
            UpForce = (leftButton + rightButton) * handUpForceScalar;
        }
    }

    public void ToggleInvertedHead()
    {
        inverted = !inverted;
    }

    public void ToggleControlOnDesktop()
    {
        controlOnDesktop = !controlOnDesktop;
    }

    private void HeadMethod()
    {

        // Calculate the signed angle along the y-axis
        float signedYRotation = Quaternion.Angle(
            Quaternion.Euler(0f, head.transform.parent.localEulerAngles.y, 0f),
            Quaternion.Euler(0f, head.transform.localEulerAngles.y, 0f)
        ) * Mathf.Sign(head.transform.localRotation.y);


        if (inverted)
        {
            signedYRotation *= -1;
        }

        float tiltDirection = Mathf.Sign(head.transform.localRotation.x);

        float signedXRotation = (-1 * Quaternion.Angle(
        Quaternion.Euler(head.transform.parent.localEulerAngles.x, 0f, 0f),
        Quaternion.Euler(head.transform.localEulerAngles.x, 0f, 0f)
    ) * tiltDirection) - headUpForceOffset;

        if (inverted)
        {
            signedXRotation *= -1;
        }

        OnHeadInput?.Invoke(head.transform.localEulerAngles);

        if (steeringMethod != SteeringMethod.Head)
        {
            return;
        }


        if (Mathf.Abs(signedYRotation) > headAngleTurnThreshold)
        {
            TurnForce = Mathf.Max(Mathf.Min(signedYRotation / headTurnForceReduction, headTurnForceMax), -headTurnForceMax);
        }
        else
        {
            TurnForce = 0;
        }


        if (travelMode == TravelMode._3D)
        {

            if (signedXRotation > headAngleUpThreshold)
            {
                UpForce = Mathf.Min(signedXRotation * headUpForceScalar, headForceUpMax);
            }
            else if (signedXRotation < (-1 * headAngleDownThreshold))
            {
                UpForce = Mathf.Max(signedXRotation * headUpForceScalar, -headForceUpMax);
            }
            else
            {
                UpForce = 0;
            }
        }

    }

    private void CalculateFeetTurnForce(float rawTurnForce)
    {
        if (Mathf.Abs(rawTurnForce) > feetForceTurnThreshold)
        {

            TurnForce = -1 * (rawTurnForce / feetForceTurnReduction);

        }
        else
        {
            TurnForce = 0;
        }
    }

    private void CalculateFeetUpForce(float rawUpForce)
    {

        if (Mathf.Abs(rawUpForce) > feetForceUpThreshold)
        {
            if (feetUpTime > feetUpTimeThreshold)
            {
                UpForce = rawUpForce / feetForceUpReduction;
                if (UpForce > 0)
                {
                    boat.transform.rotation.eulerAngles.Set(-45, 0, 0);
                    //boat.transform.Rotate(new Vector3(-20, 0, 0), Space.Self);
                }
                else
                {
                    boat.transform.rotation.eulerAngles.Set(45, 0, 0);
                    //boat.transform.Rotate(new Vector3(20, 0, 0), Space.Self);
                }

            }
            else if (feetUpTime == 0)
            {
                // Makes feetUpTime tick in update
                feetUpTime += 0.001f;
            }
            else
            {
                // Do Nothing;
            }
        }

        else
        {
            feetUpTime = 0;
            UpForce = 0;
            boat.transform.rotation.eulerAngles.Set(0, 0, 0);
            //boat.transform.rotation.eulerAngles.Set(0, 0, 0);
            //boat.transform.Rotate(new Vector3(0, 0, 0), Space.Self);
        }
    }

    public void FeetMethod(string message)
    {
        string[] input = message.Split("-");
        int leftHeelForce = int.Parse(input[1]);
        int rightHeelForce = int.Parse(input[2]);
        int leftToeForce = int.Parse(input[3]);
        int rightToeForce = int.Parse(input[4]);

        OnFeetInput?.Invoke(leftHeelForce, rightHeelForce, leftToeForce, rightToeForce);
        if (controlOnDesktop || steeringMethod != SteeringMethod.Feet)
        {
            return;
        }

        float turnForce = rightToeForce + rightHeelForce - (leftToeForce + leftHeelForce);

        CalculateFeetTurnForce(turnForce);

        if (travelMode == TravelMode._3D)
        {
            float upForce = -1 * (rightHeelForce + leftHeelForce - (rightToeForce + leftToeForce));
            CalculateFeetUpForce(upForce);
        }
    }

    public void SetSpeedForce(string speedMessage)
    {
        try
        {
            string[] message = speedMessage.Split("-");
            float ANTspeed = float.Parse(message[1]);

            Speed = ANTspeed / 30;


            if (Speed > TopSpeed)
            {
                TopSpeed = Speed;
            }

        }
        catch (UnityException ex)
        {
            Debug.Log(ex);
        }
    }


    public void CalibrateGyroscope()
    {
        Debug.Log("Calibrate!");

        if (!pendingCalibration)
        {
            pendingCalibration = true;
            calibrationIsComplete = false;
            Debug.Log("Init calibration");
            InfoBoard.Instance.SetCalibrationText("Calibrating...");

            pendingCalibrationStartTime = Time.time;
            pendingCalibration = true;
            return;
        }
        else if (secondsForCalibration > (Time.time - pendingCalibrationStartTime))
        {
            Debug.Log("Calibrating...");
            return;
        }
        else if (calibrationIsComplete != true)
        {
            calibrationIsComplete = true;
            Debug.Log("Gyroscope offset before: " + gyroscopeOffset.ToString());
            gyroscopeOffset = rawGyroscope;
            Debug.Log("Gyroscope offset after: " + gyroscopeOffset.ToString());
            InfoBoard.Instance.SetCalibrationText("Complete!");
            _audioSource.PlayOneShot(calibrationCompleted);
            StartCoroutine(ResetCalibrationText(2));
        }
    }

    IEnumerator ResetCalibrationText(float s)
    {
        yield return new WaitForSeconds(s);
        InfoBoard.Instance.SetCalibrationText("");
    }


    public void CancelCalibrateGyroscope()
    {
        Debug.Log("Calibration cancelled");
        pendingCalibration = false;
        calibrationIsComplete = false;
        StartCoroutine(ResetCalibrationText(2));
    }

    public void SetSteeringMethod(string methodMessage)
    {
        int method = int.Parse(methodMessage.Split(";")[1]);

        if (method == 100)
        {
            steeringMethod = SteeringMethod.Feet;
        }
        else if (method == 200)
        {
            steeringMethod = SteeringMethod.Head;
        }
        else if (method == 300)
        {
            steeringMethod = SteeringMethod.Hand;
        }
        else
        {
            Debug.Log("Nope!");
        }
    }

    public void SetTravelMethod(string methodMessage)
    {
        int travel = int.Parse(methodMessage.Split(";")[1]);

        if (travel == 400)
        {
            travelMode = TravelMode._2D;
        }
        else if (travel == 500)
        {
            travelMode = TravelMode._3D;
        }
        else
        {
            Debug.Log("Nope!");
        }
    }

    void Update()
    {
        if (controlOnDesktop)
        {


            HandlebarSpeed = 1;
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");
            int speed = Input.GetMouseButton(0) ? 1 : 0;

            Speed = speed * desktopSpeed;

            TurnForce = moveHorizontal * desktopTurnForce;

            UpForce = moveVertical * desktopUpForce;


        }
    }


    void CalculateHandlebarSpeed()
    {

        float distance;

        float leftHandDistance = Vector3.Distance(_rowRefPoint.transform.position, leftHand.transform.position);
        float rightHandDistance = Vector3.Distance(_rowRefPoint.transform.position, rightHand.transform.position);

        if (!_lostLeftHandConnection && !_lostRightHandConnection)
        {
            distance = ((lastLeftHandDistance - leftHandDistance) + (lastRightHandDistance - rightHandDistance)) / 2;
        }
        else if (_lostLeftHandConnection)
        {
            distance = lastRightHandDistance - rightHandDistance;
        }
        else if (_lostRightHandConnection)
        {
            distance = lastLeftHandDistance - leftHandDistance;
        }
        else
        {
            distance = 0;
        }


        if (sampleCounter < smoothSamples)
        {
            sampleCounter++;
            accBucket += distance / Time.fixedDeltaTime;
        }
        else
        {

            HandlebarSpeed = Mathf.Min(Mathf.Max(accBucket / sampleCounter, 0), maxAccThreshold);
            InfoBoard.Instance.SetRowAccelerationText("Row power: " + HandlebarSpeed);
            if (HandlebarSpeed < minAccThreshold)
            {
                HandlebarSpeed = 0;
            }
            else
            {
                HandlebarSpeed += 1;
            }

            if (HandlebarSpeed > TopHandlebarSpeed)
            {
                TopHandlebarSpeed = HandlebarSpeed;
            }


            sampleCounter = 0;
            accBucket = 0;
        }
        lastLeftHandDistance = leftHandDistance;
        lastRightHandDistance = rightHandDistance;
    }

    void PreventGoingBelowOcean()
    {
        if (transform.position.y <= 1)
        {
            Vector3 position = transform.position;
            position.y = 1;
            transform.position = position;
            UpForce = Mathf.Max(UpForce, 0);
        }
    }

    void CheckHandTrackingConnection()
    {
        if (!_leftHandTracker.IsConnected && !_lostLeftHandConnection)
        {
            _lostLeftHandConnection = true;
        }
        else if (_leftHandTracker.IsConnected && _lostLeftHandConnection)
        {
            lastLeftHandDistance = Vector3.Distance(_rowRefPoint.transform.position, leftHand.transform.position);
            _lostLeftHandConnection = false;
        }

        if (!_rightHandTracker.IsConnected && !_lostRightHandConnection)
        {
            _lostRightHandConnection = true;
        }
        else if (_rightHandTracker.IsConnected && _lostRightHandConnection)
        {
            lastRightHandDistance = Vector3.Distance(_rowRefPoint.transform.position, rightHand.transform.position);
            _lostRightHandConnection = false;
        }
    }


    void FixedUpdate()
    {

        CheckHandTrackingConnection();


        HeadMethod();


        if (steeringMethod == SteeringMethod.Feet && feetUpTime != 0)
        {
            feetUpTime += Time.fixedDeltaTime;
        }

        float frameScalar = 70 * Time.fixedDeltaTime;

        CalculateHandlebarSpeed();

        PreventGoingBelowOcean();

        // Rotation 
        _rb.AddTorque(transform.up * TurnForce * Mathf.Pow(HandlebarSpeed * handlebarTurnSpeedScalar, 2) * frameScalar); // TODO: Test if speed sensitive.

        // 2D force        
        _rb.AddForce(transform.forward * (Speed * speedScalar) * (HandlebarSpeed * handlebarForwardSpeedScalar) * frameScalar);

        // 3D force
        _rb.AddForce(transform.up * (UpForce * upScalar) * (Speed * speedScalar) * (HandlebarSpeed * handlebarUpSpeedScalar) * frameScalar);


    }


}
