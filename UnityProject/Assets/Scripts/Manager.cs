using UnityEngine;

// -------------------------------------------------------------- //
// This class manages the transfer of data between setup and main //
// -------------------------------------------------------------- //

public class Manager : MonoBehaviour
{
    public static Manager Instance;

    // Store shoulder positions
    public Vector3 leftShoulderPos;
    public Vector3 rightShoulderPos;
    public float armLength;

    private void Awake()
    {
        // Ensure only one instance of this class exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}