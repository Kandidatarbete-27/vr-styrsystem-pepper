using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Concurrent;
using System.Threading;

public class MoveArm
{
    // Debug flag
    public bool armDebug = false;

    // These variables are only updated by main thread
    private Vector3 _leftShoulderPosition;
    private Vector3 _rightShoulderPosition;
    private Vector3 _firstShoulderRotation;
    private Vector3 _leftControllerPosition;
    private Vector3 _rightControllerPosition;
    private Vector3 _headsetRotation;
    private float _userArmLength = 0.33f;

    private ConcurrentQueue<VRData> _commandQueue;

    // Input actions - Should only be accessed in Unity's main thread
    private InputActionProperty _closeRHand;
    private InputActionProperty _closeLHand;


    // Last known arm angles - used for starting guess in IK
    private float[] _lastLeftArm = new float[5] { 0f, 0f, 0f, -0.5f, 0f };
    private float[] _lastRightArm = new float[5] { 0f, 0f, 0f, 0.5f, 0f };

    // Inverse kinematics solver and related values
    private readonly InverseKinematics _ik = new();
    private readonly float _precision;
    private readonly int _maxIterations;

    // Thread variables
    private bool _running = true;
    private Thread _updateThread;
    private int _updateFrequency;


    // Constructor to initialize the class
    public MoveArm(InputActionProperty closeRHand, InputActionProperty closeLHand, ConcurrentQueue<VRData> queue,
                   float userArmLength, int updateFrequency = 10, float precision = 0.04f, int maxIterations = 100)
    {
        // Store input actions for reading in the update loop
        _closeRHand = closeRHand;
        _closeLHand = closeLHand;
        _commandQueue = queue;
        _updateFrequency = updateFrequency;
        _precision = precision;
        _maxIterations = maxIterations;
        _userArmLength = userArmLength;
        // Start a new thread for processing arm updates
        _updateThread = new Thread(UpdateArmsLoop)
        {
            IsBackground = true
        };
        _updateThread.Start();

        Debug.Log("MoveArm thread started");
    }

    // Thread loop
    private void UpdateArmsLoop()
    {
        while (_running)
        {
            UpdateArms();
            Thread.Sleep(1000 / _updateFrequency); // Prevent CPU from being overloaded
        }
    }

    // Logic for creating arm data
    private void UpdateArms()
    {
        // Copy values to local variables to prevent partial reads
        Vector3 leftShoulder = _leftShoulderPosition;
        Vector3 rightShoulder = _rightShoulderPosition;
        Vector3 firstRotation = _firstShoulderRotation;

        Vector3 leftController = _leftControllerPosition;
        Vector3 rightController = _rightControllerPosition;
        Vector3 headsetRotation = _headsetRotation;

        // Read input actions on the main thread and store their values
        float rHandClosed = _closeRHand.action.ReadValue<float>();
        float lHandClosed = _closeLHand.action.ReadValue<float>();

        // Calculate joint angles
        float[] leftArmAngles = CalculateJointAngles(leftShoulder, leftController, true, headsetRotation);
        float[] rightArmAngles = CalculateJointAngles(rightShoulder, rightController, false, headsetRotation);

        bool updateLeft = true; // Temporary, in case we want to add some logic here later
        bool updateRight = true;

        if (updateLeft)
        {
            //Debug.Log("Sending left arm data");
            LogDebug("Left", leftArmAngles);
            _commandQueue.Enqueue(new LeftArmData(leftArmAngles, lHandClosed));
            Helper.UpdateAngles(_lastLeftArm, leftArmAngles);
        }

        if (updateRight)
        {
            //Debug.Log("Sending right arm data");
            LogDebug("Right", rightArmAngles);
            _commandQueue.Enqueue(new RightArmData(rightArmAngles, rHandClosed));
            Helper.UpdateAngles(_lastRightArm, rightArmAngles);
        }
    }


    // Calculates the joint angles using inverse kinematics
    private float[] CalculateJointAngles(Vector3 shoulderPosition, Vector3 controllerPosition, bool isLeftArm, Vector3 headsetRotation)
    {
        // Calculate the local direction of the controller relative to the shoulder
        Vector3 shoulderToController = controllerPosition - shoulderPosition;
        Quaternion bodyRotation = Quaternion.Euler(headsetRotation - _firstShoulderRotation);
        bodyRotation = Quaternion.Euler(0, bodyRotation.eulerAngles.y, 0);         // Only rotate around y-axis
        Vector3 localDirection = Quaternion.Inverse(bodyRotation) * shoulderToController;

        // Scale to match Pepper's arm length
        float pepperArmLength = 0.40f;
        float scaleFactor = _userArmLength / pepperArmLength;
        Vector3 scaledDirection = localDirection / scaleFactor;

        //If the arm is extended too far, point with straight arm towards the controller
        if (scaledDirection.magnitude > 0.38f)
        {
            // Fix for holding arm across the chest
            if ((scaledDirection.x < -0.1 && !isLeftArm) || (scaledDirection.x > 0.1 && isLeftArm))
            {
                scaledDirection = scaledDirection.normalized * 0.27f;
                scaledDirection = isLeftArm ? Quaternion.Euler(0, -15f, 0) * scaledDirection
                                            : Quaternion.Euler(0, 15f, 0) * scaledDirection;
            }
            else
            {
                float shoulderPitch = Mathf.Atan2(scaledDirection.y, new Vector2(scaledDirection.x, scaledDirection.z).magnitude);
                float shoulderRoll = Mathf.Atan2(scaledDirection.x, scaledDirection.z);
                //float shoulderPitch = Mathf.Atan2(scaledDirection.z, scaledDirection.y);
                //float shoulderRoll = Mathf.Asin(scaledDirection.x / scaledDirection.magnitude);
                return new float[] { -shoulderPitch, -shoulderRoll, 0, 0, 0 };
            }
        }

        //If the arm is too close to the body, scale the direction vector to a point pepper can reach
        if (scaledDirection.magnitude < 0.27f)
        {
            scaledDirection = scaledDirection.normalized * 0.27f;
        }

        //Estimate the elbow roll based on law of cosines


        // Correct axes to match Pepper's coordinate system
        Vector3 correctedAxes = new Vector3(scaledDirection.z, -scaledDirection.x, scaledDirection.y);

        // Perform inverse kinematics
        return isLeftArm ? _ik.CalculateJointAngles(correctedAxes, _lastLeftArm, true, _precision, _maxIterations)
                         : _ik.CalculateJointAngles(correctedAxes, _lastRightArm, false, _precision, _maxIterations);
    }

    private void LogDebug(string arm, float[] angles)
    {
        if (armDebug)
        {
            Debug.Log($"{arm} arm data: " +
                $"Sh. ptch: {angles[0]}, Sh. rol: {angles[1]}, El. jaw: {angles[2]}, " +
                $"El. rol: {angles[3]}, Wr. yaw: {angles[4]}");
        }
    }

    // Stop the running thread
    public void Stop()
    {
        _running = false;
        _updateThread.Join(); // Wait for the thread to finish
    }

    // Update local variables with data from main script
    public void UpdateData(Vector3 leftShoulder, Vector3 rightShoulder, Vector3 firstRotation,
                           Vector3 leftController, Vector3 rightController, Vector3 headsetRotation)
    {
        _leftShoulderPosition = leftShoulder;
        _rightShoulderPosition = rightShoulder;
        _firstShoulderRotation = firstRotation;

        _leftControllerPosition = leftController;
        _rightControllerPosition = rightController;
        _headsetRotation = headsetRotation;
    }
}