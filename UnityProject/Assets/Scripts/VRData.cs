using System;


[Serializable]
public abstract class VRData
{
    public string type;
}

[Serializable]
public class LeftArmData : VRData
{
    public float LShoulderPitch;
    public float LShoulderRoll;
    public float LElbowYaw;
    public float LElbowRoll;
    public float LWristYaw;
    public float LHand;

    public LeftArmData(float[] angles, float hand)
    {
        type = "leftArm";
        LShoulderPitch = angles[0];
        LShoulderRoll = angles[1];
        LElbowYaw = angles[2];
        LElbowRoll = angles[3];
        LWristYaw = angles[4];
        LHand = hand;
    }
}

[Serializable]
public class RightArmData : VRData
{
    public float RShoulderPitch;
    public float RShoulderRoll;
    public float RElbowYaw;
    public float RElbowRoll;
    public float RWristYaw;
    public float RHand;

    public RightArmData(float[] angles, float hand)
    {
        type = "rightArm";
        RShoulderPitch = angles[0];
        RShoulderRoll = angles[1];
        RElbowYaw = angles[2];
        RElbowRoll = angles[3];
        RWristYaw = angles[4];
        RHand = hand;
    }
}

// creates a structure for data related to head movement
[Serializable]
public class HeadData : VRData
{
    public float HeadYaw;
    public float HeadPitch;
}

// creates a structure for data related to positional movement
[Serializable]
public class MoveData : VRData
{
    public float x;
    public float y;
    public float z;
    public float theta;
}

// A data structure making pepper shut down, when closing unity
[Serializable]
public class QuitData : VRData
{
    public QuitData()
    {
        type = "quit";
    }
}
