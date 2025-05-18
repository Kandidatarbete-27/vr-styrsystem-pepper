// Necessary imports for the script
using System;
using UnityEngine;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using UnityEngine.InputSystem;
using System.Collections.Concurrent;

// ----------------------------------------------------- //
// This is the main program that sends data to the robot //
// ----------------------------------------------------- //

public class TcpSender : MonoBehaviour
{
    // Settings for the script
    public bool noSetup = false;
    public bool rotationDebug = false;
    public bool shoulderDebug = false;
    public bool armDebug = false;
    public float angleThreshold = 0.1f;
    public float moveThreshold = 0.1f;
    public float userArmLength = 0.33f;
    public int   updateFrequency = 10;

    // Sets up threads and the TCP client
    private Thread thread;
    private Thread sendThread;
    private TcpClient client;
    private NetworkStream stream;

    // Values for the other computer's ip and port
    public string serverIp = "127.0.0.1";
    private int serverPort = 55002;

    // Start values for position and rotation of headset
    private Vector3 FirstHead;
    private Vector3 FirstShoulderRotation;
    private Vector3 startingPosition;

    // A bool that puts some code into effect only the first iteration
    private bool FirstTime = true;

    // Variables that keep track of the last values sent
    private Vector3 lastPosition;
    private float lastRotation;
    private float[] lastHead = new float[2];

    // Variables that keep track of shoulder position
    private Vector3 currentLeftShoulder;
    private Vector3 currentRightShoulder;

    // bools that control the script depending on their values
    private volatile bool isConnected = false;
    private volatile bool isRunning = true;

    // Creates a connection from the physical equipment to this script
    public Transform headset;
    public Transform LeftController;
    public Transform RightController;
    public InputActionProperty CloseRHand;
    public InputActionProperty CloseLHand;
    

    // Useful variables in the script
    private float timeSinceLastSend = 0f;
    private float sendInterval = 0.5f;
    private Vector3 initialLeftShoulderPos;
    private Vector3 initialRightShoulderPos;

    // Starts a queue that commands can be sent to
    private ConcurrentQueue<VRData> commandQueue = new ConcurrentQueue<VRData>();

    private MoveArm  moveArm;
    private MoveHead moveHead;
    private MoveBody moveBody;


    // The start function which is run first
    void Start()
    {
        // Either gets the values from the manager or sets them to default
        if (!noSetup)
        {
            initialLeftShoulderPos = Manager.Instance.leftShoulderPos;
            initialRightShoulderPos = Manager.Instance.rightShoulderPos;
            userArmLength = Manager.Instance.armLength;
            Debug.Log($"Left: {initialLeftShoulderPos}, Right: {initialRightShoulderPos}, Arm length: {userArmLength}");
        }
        else
        {
            initialLeftShoulderPos = new Vector3(-0.15f, -0.15f, 0f);
            initialRightShoulderPos = new Vector3(0.15f, -0.15f, 0f);
            if(userArmLength <= 0.1) userArmLength = 0.4f;
        }

        startingPosition = headset.position;

        // Initialize MoveArm with input actions and initial values
        moveArm = new MoveArm(CloseRHand, CloseLHand, commandQueue, userArmLength, updateFrequency);

        // Initialize MoveHead with initial values
        moveHead = new MoveHead(headset.rotation.eulerAngles, angleThreshold, updateFrequency, commandQueue);

        // Initialize MoveBody with initial values
        moveBody = new MoveBody(headset.rotation.eulerAngles, startingPosition, commandQueue, moveThreshold, updateFrequency);

        Debug.Log("Starting TCP Sender");
        // Some start values
        lastRotation = 0;
        FirstHead = headset.rotation.eulerAngles;
        FirstShoulderRotation = headset.rotation.eulerAngles;
        if (FirstHead.x > 180) FirstHead.x -= 360;
        if (FirstHead.y > 180) FirstHead.y -= 360;
        // Starts a thread for ConnectToServer()
        Debug.Log("Starting connection thread");
        thread = new Thread(ConnectToServer);
        thread.IsBackground = true;
        thread.Start();

        // Starts a thread for SendVRDataToServer()
        sendThread = new Thread(SendData);
        sendThread.IsBackground = true;
        sendThread.Start();


    }

    // The update function runs on every frame
    void Update()
    {
        if (isConnected)
        {   // Increases a variable that keeps track of how long ago the last message was sent
            timeSinceLastSend += Time.deltaTime;
            // Checks that there has been some time since the last sent message, avoids "overload"
            if (timeSinceLastSend >= sendInterval)
            {   // Only proceeds if the equipment is connected
                if (headset != null && RightController != null && LeftController != null)
                {
                    // If this is the first "Update()" starting values are saved
                    if (FirstTime)
                    {
                        lastPosition = headset.position - startingPosition;
                        FirstTime = false;
                        Debug.Log("Setup Complete");
                    }

                    // Update shoulders
                    UpdateShoulders();

                    // Send new data to the arm module
                    moveArm.UpdateData(currentLeftShoulder, currentRightShoulder, FirstShoulderRotation, 
                        LeftController.position, RightController.position, headset.rotation.eulerAngles);

                    // Send new data to the head module and receive updated rotation
                    lastRotation = moveHead.UpdateData(headset.rotation, lastPosition);

                    // Updates movement  
                    lastPosition = moveBody.UpdateData(headset.position, lastRotation);

                    // resets the time
                    timeSinceLastSend = 0f;
                }

                // If the equipment did not connect
                else
                {
                    Debug.LogError("Equipment not detected");
                }
            }
        }
    }

    // Establishes a connection to the computer running Pepper
    void ConnectToServer()
    {   //Checks if the application is running
        while (isRunning)
        {
            try
            {   // Checks if it should connect
                if (!isConnected)
                {
                    // Creates a client and connects to the IP and port
                    client = new TcpClient();
                    client.Connect(serverIp, serverPort);
                    stream = client.GetStream();

                    // Updates "isConnected" to show that everything is working
                    isConnected = true;
                    Debug.Log("Connected to server " + serverIp + ":" + serverPort);
                }
            }
            // If something went wrong
            catch (Exception e)
            {
                // If the application wasn't running, it will exit immediately
                if (!isRunning) break;
                // Otherwise it will try everything again
                Debug.LogWarning("Connection failed, retrying: " + e.Message);
                Thread.Sleep(1000);
            }
        }
    }
    // The function that sends the data through TCP
    void SendData()
    {   // Checks if the application is running

        Debug.Log($"SendData queue address: {commandQueue.GetHashCode()}");

        while (isRunning)
        {   // Checks wheter the communication link is connected, tries until true if not
            if (!isConnected)
            {
                Thread.Sleep(100);
                continue;
            }
            // Checks if there's a command in the queue,
            if (commandQueue.TryDequeue(out VRData data))
            {
                try
                {
                    // Converts data to "json"
                    string json = JsonUtility.ToJson(data);

                    // Calculates the length of what should be sent, and converts it to bytes
                    int length = json.Length;
                    byte[] lengthBytes = BitConverter.GetBytes(length);

                    // Checks if we need to reverse "lengthBytes"
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(lengthBytes);
                    }
                    // Sends the length of the message, and then an encoded json that contains the data
                    stream.Write(lengthBytes, 0, lengthBytes.Length);
                    byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                    stream.Write(jsonBytes, 0, jsonBytes.Length);
                }
                // If something didn't work, it disconnects from the server and breaks 
                catch (Exception e)
                {
                    if (!isRunning) break; //  Exit immediately if quitting
                    Debug.LogError("Error sending message: " + e.Message);
                    Disconnect();
                    break;
                }
            }
            // Saves CPU usage by sleeping
            else
            {
                Thread.Sleep(10);
            }
        }
    }

    void UpdateShoulders()
    {
        // Get the current headset rotation and normalize it relative to the initial rotation
        Vector3 relativeRotation = headset.rotation.eulerAngles - FirstShoulderRotation;
        Quaternion currentHeadsetRotation = Quaternion.Euler(0, relativeRotation.y, 0);

        // Rotate the initial shoulder positions to align with the current headset orientation
        Vector3 updatedLeftShoulder = currentHeadsetRotation * initialLeftShoulderPos;
        Vector3 updatedRightShoulder = currentHeadsetRotation * initialRightShoulderPos;

        // Calculate final shoulder positions relative to the current headset position
        Vector3 finalLeftShoulderPos = headset.position + updatedLeftShoulder;
        Vector3 finalRightShoulderPos = headset.position + updatedRightShoulder;

        // Update shoulder positions
        currentLeftShoulder  = finalLeftShoulderPos;
        currentRightShoulder = finalRightShoulderPos;
        if (shoulderDebug)
        {
            Debug.Log($"Left: {currentLeftShoulder}, Right: {currentRightShoulder}");
            Debug.Log($"Headset: {headset.position}");
        }
    }


    // The function which closes the connection to the server
    void Disconnect()
    {
        // Sets the local variables to false
        isRunning = false;
        isConnected = false;

        try
        {
            // closes the TCP client and "stream"
            if (stream != null) stream.Close();
            if (client != null) client.Close();
        }
        catch (Exception e)
        {
            // If something went wrong send a message to the console
            Debug.LogWarning("Error while closing connection: " + e.Message);
        }
        finally
        {
            // Sets stream and client to "null"
            stream = null;
            client = null;
        }
        // Prints status in console
        Debug.Log("Disconnected from server.");
    }
    // Function which is run if Unity is stopped
    void OnApplicationQuit()
    {
        // Sends quit message to the python program controlling Pepper
        if (isConnected)
        {
            try
            {
                commandQueue.Enqueue(new QuitData());
                while (commandQueue.Count > 0)
                {
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error sending quit message: " + e.Message);
            }
        }

        // Stops all movement threads
        moveArm.Stop();
        moveHead.Stop();
        moveBody.Stop();

        // Sets "isRunning" to false which will stop the other threads
        isRunning = false;
        Debug.Log("Stopping threads");

        //  Waits if the thread need time to close
        if (thread != null && thread.IsAlive)
        {
            thread.Join(500);
        }

        if (sendThread != null && sendThread.IsAlive)
        {
            sendThread.Join(500);
        }

        // Calls the disconnect function and ends the application
        Disconnect();
        Debug.Log("Application has quit.");
    }
}