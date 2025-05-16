using UnityEngine;

public class FollowHead : MonoBehaviour
{
    public Transform headPos; // Connecting the headset to this script
    public float distanceFromHead = 2.0f; // The amount of distance the canvas will be placed from your eyes
    public float followSpeed = 5.0f; // How fast the canvas follows you're movement

    //The update function constantly checks the operators movement and adjusts the position of the canvas in the 3D world to match
    void Update()
    {
        if (headPos == null) return;

        // Saves the intended position of the canvas for each update
        Vector3 canvasPos = headPos.position + headPos.forward * distanceFromHead;

        // Moves the canvas to intended position
        transform.position = Vector3.Lerp(transform.position, canvasPos, Time.deltaTime * followSpeed);

        // Rotates the canvas to follow head movement
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.position - headPos.position), Time.deltaTime * followSpeed);
    }
}

