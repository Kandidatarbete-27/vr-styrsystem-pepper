using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

public class MoveHead
{
    // Variables for the head movement
    private Quaternion     _headsetRotation;
    private Vector3        _firstHead;
    private float[]        _lastHead;
    private readonly float _angleThreshold;
    private float          _lastRotation;
    private Vector3        _lastPosition;
    private bool           _firstData;

    // Thread logic
    private Thread                  _updateThread;
    private bool                    _running;
    private int                     _updateFrequency = 10;
    private ConcurrentQueue<VRData> _commandQueue;


    public MoveHead(Vector3 firstHead, float angleThreshold, int updateFrequency, ConcurrentQueue<VRData> commandQueue)
    {
        _firstHead       = firstHead;
        _lastHead        = new float[2] { 0f, 0f };
        _angleThreshold  = angleThreshold;
        _headsetRotation = new Quaternion(0, 0, 0, 0);
        _lastRotation    = 0;
        _lastPosition    = new Vector3(0, 0, 0);
        _firstData       = false;
        _commandQueue    = commandQueue;
        _updateFrequency = updateFrequency;
        _running         = true;

        // Start a new thread for processing head updates
        _updateThread = new Thread(UpdateHeadLoop)
        {
            IsBackground = true
        };
        _updateThread.Start();

        Debug.Log("MoveHead thread started");
    }

    public float UpdateData(Quaternion headsetRotation, Vector3 lastPosition)
    {
        _headsetRotation = headsetRotation;
        _lastPosition    = lastPosition;
        if (!_firstData) _firstData = true;
        return _lastRotation;
    }

    void UpdateHeadLoop()
    {
        while (_running)
        {
            if(_firstData) UpdateHead();
            Thread.Sleep(1000 / _updateFrequency);
        }
    }

    void UpdateHead()
    {
        // Extracts the rotation and last position from Unity
        Vector3 eulerRotation = _headsetRotation.eulerAngles;
        Vector3 lastPosition  = _lastPosition;

        // Keeps the rotation of Pepper within it's boundaries
        if (Mathf.Abs(eulerRotation.x) > 180) eulerRotation.x -= 360;
        if (Mathf.Abs(eulerRotation.y) > 180) eulerRotation.y -= 360;



        // Converts the headset data to radians and subtracts the headset's initial position
        // Converts the headset data to radians and subtracts the headset's initial position
        float[] headAngles = new float[2] {
            Mathf.Deg2Rad * (Mathf.DeltaAngle(_firstHead.x, eulerRotation.x)),
            Mathf.Deg2Rad * (Mathf.DeltaAngle(_firstHead.y, eulerRotation.y))
        };

        // Check if the head has rotated beyond 45 degrees
        if (Mathf.Abs(headAngles[1]) > Mathf.PI/4)  // 45 degrees in radians
        {
            Debug.Log("Should update rotation with:");
            Debug.Log(headAngles[1]);
            UpdateRotation(headAngles[1], lastPosition);
            _firstHead = eulerRotation;
            return;
        }

        // Checks if there is a need for sending new data
        if (Helper.HasAngleChanged(headAngles, _lastHead, _angleThreshold))
        {
            // Updates the data structure
            HeadData data = new HeadData
            {
                type      = "head",
                HeadYaw   = -1 * headAngles[1],
                HeadPitch = headAngles[0]
            };
            // Sends the data to the queue
            _commandQueue.Enqueue(data);
            // Updates these new angles as last sent
            Helper.UpdateAngles(_lastHead, headAngles);
        }
        return;
    }

    void UpdateRotation(float rotate, Vector3 lastPosition)
    {
        _lastRotation -= rotate;
        if (_lastRotation < -Mathf.PI) _lastRotation += Mathf.PI * 2;
        if (_lastRotation > Mathf.PI)  _lastRotation -= Mathf.PI * 2;

        MoveData movedata = new MoveData
        {
            type = "move",
            x     = -lastPosition.x,
            y     =  lastPosition.y,
            z     =  lastPosition.z,
            theta =  _lastRotation
        };

        Debug.Log("Sending rotation data");
        Debug.Log(rotate);
        _commandQueue.Enqueue(movedata);

        return;
    }

    // Stop the running thread
    public void Stop()
    {
        _running = false;
        _updateThread.Join(); // Wait for the thread to finish
    }
}