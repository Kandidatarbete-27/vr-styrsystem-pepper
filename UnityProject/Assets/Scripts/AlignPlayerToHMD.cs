using UnityEngine;
using UnityEngine.XR;

// ----------------------------------------------------------------------------- //
// This script aligns the player's XR Origin to the headset's forward direction. //
// ----------------------------------------------------------------------------- //

public class AlignPlayerToHMD : MonoBehaviour
{
    // Reference to the XR main camera transform
    public Transform cameraTransform;

    void Start()
    {
        StartCoroutine(AlignToHeadset());
    }

    System.Collections.IEnumerator AlignToHeadset()
    {
        // Wait for XR system to initialize and track the headset
        yield return new WaitForSeconds(0.1f);

        // Project the headset's forward direction onto the horizontal plane
        Vector3 hmdForward = cameraTransform.forward;
        hmdForward.y = 0f;
        hmdForward.Normalize();

        if (hmdForward != Vector3.zero)
        {
            // Rotate this object (XR origin) to match the HMD's horizontal forward
            transform.rotation = Quaternion.LookRotation(hmdForward, Vector3.up);
        }
    }
}
