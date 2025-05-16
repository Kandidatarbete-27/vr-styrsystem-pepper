using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;


public class MoveBody
{
    private Vector3 _headsetPosition;
    private Vector3 _firstMove;
    private Vector3 _lastPosition;
    private float   _lastRotation;
    private float   _moveThreshold;
    public  bool    _rotationDebug;
    private bool    _firstDataReceived;
    private Quaternion _firstRotation;

    // Thread logic
    private Thread  _updateThread;
    private bool    _running;
    private int     _updateFrequency;


    private ConcurrentQueue<VRData> _commandQueue;

    public MoveBody(Vector3 firstRotation, Vector3 firstMove, ConcurrentQueue<VRData> commandQueue, float moveThreshold, int updateFrequency)
    {
        _headsetPosition   = firstMove;
        _firstMove         = firstMove;
        _lastPosition      = new Vector3(0, 0, 0);
        _lastRotation      = 0;
        _moveThreshold     = moveThreshold;
        _firstDataReceived = false;
        _firstRotation     = Quaternion.Euler(0, firstMove.y, 0);

        _running           = true;
        _updateFrequency   = updateFrequency;
        _commandQueue      = commandQueue;


        _updateThread = new Thread(UpdatemoveLoop)
        {
            IsBackground = true
        };
        _updateThread.Start();

        Debug.Log("MoveBody thread started");
    }

    public Vector3 UpdateData(Vector3 headsetPosition, float lastRotation)
    {
        _headsetPosition = headsetPosition;
        _lastRotation    = lastRotation;

        // First data has been received
        if (!_firstDataReceived) _firstDataReceived = true;
        return _lastPosition;
    }

    void UpdatemoveLoop()
    {
        while (_running)
        {
            if(_firstDataReceived) UpdateMove();
            Thread.Sleep(1000 / _updateFrequency);
        }
    }

    void UpdateMove()
    {
        //Extracts the positional data from the headset and subtracts the intial position
        Vector3 Position = _headsetPosition - _firstMove;
        Position         = _firstRotation * Position;
        // Checks the difference since last movement
        Vector3 PositionDiff = Position - _lastPosition;

        // Rotate the movement vector by the robot's last known rotation
        Quaternion rotation = Quaternion.Euler(0, -_lastRotation, 0);

        // Checks if there's a need to send new data
        if (Helper.PositionHasChanged(PositionDiff, _moveThreshold))
        {
            // Updates the data structure for movement
            MoveData movedata = new MoveData
            {
                type = "move",
                x = -Position.x,  
                y =  Position.y,
                z =  Position.z, 
                theta = _lastRotation
            };
            Debug.Log("Sending movement data");
            Debug.Log(movedata);
            // sends the data to the queue and updates the last position
            _commandQueue.Enqueue(movedata);
            _lastPosition = Position;
        }
    }

    // Stops the thread
    public void Stop()
    {
        _running = false;
        _updateThread.Join(); // Wait for the thread to finish
    }
}
