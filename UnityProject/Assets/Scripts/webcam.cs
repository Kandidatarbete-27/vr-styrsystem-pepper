
// Imports that are necessary for the script
using UnityEngine;
using UnityEngine.UI;


public class Webcam : MonoBehaviour
{

    // Creates a connection to the rawimage gameobject and a useful variable
    public RawImage displayImage; 
    private WebCamTexture webcamTexture;

    void Start()
    {
        // Checks if you're computer has a camera
        if (WebCamTexture.devices.Length > 0)
        {
            // Starts the video feed
            webcamTexture = new WebCamTexture();
            displayImage.texture = webcamTexture;
            displayImage.material.mainTexture = webcamTexture;

            webcamTexture.Play();
        }
        // warns if there was no camera
        else
        {
            Debug.LogError("No webcam found!");
        }
    }

    // Stop the video feed when Unity gets stopped
    void OnApplicationQuit()
    {

        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }
    }
}
