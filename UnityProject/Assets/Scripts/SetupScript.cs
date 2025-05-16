using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SetupScript : MonoBehaviour
{
    public GameObject TcpSender;
    public RawImage armsDownImage;
    public RawImage tPoseImage;
    public Text feedbackText;
    public bool simulatorDebug = false;

    public InputActionProperty primaryButton;

    private bool isFirstPhase = true;

    public Transform headsetTransform;
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;

    private Vector3 firstHeadsetRotation;

    private Vector3 leftShoulderPos;
    private Vector3 rightShoulderPos;

    private Vector3 leftHandLowerPos;
    private Vector3 rightHandLowerPos;

    private Vector3 leftHandHigherPos;
    private Vector3 rightHandHigherPos;
    private float pressed       = 0f;
    
    private float lastpressed       = 0f;
    
    private bool isSetupCompleted = false;

    void Start()
    {
        //TcpSender.SetActive(false);
        armsDownImage.gameObject.SetActive(true);
        tPoseImage.gameObject.SetActive(false);
        firstHeadsetRotation = headsetTransform.rotation.eulerAngles;
        feedbackText.text = "Arms down and then press the primary button.";
        

        //primaryButton.action.performed += ctx => OnButtonPressed(); //Primary button is B in simulator
    }
    void Update(){
        if (simulatorDebug)
        {
            pressed = Input.GetKey(KeyCode.B) ? 1f : 0f;
        }
        else
        {
            pressed = primaryButton.action.ReadValue<float>() >= 0.5f ? 1f : 0f;
        }

        if (isFirstPhase){
            if (pressed == 1f){
                OnConfirmArmsDownPose();
            }
        }
        else{
            if (pressed == 1f & pressed !=lastpressed){
                Debug.Log("lets go");
                OnConfirmTPose();
            }
        }
        if (pressed !=lastpressed){
            lastpressed = pressed;
        }


    }
    private void OnConfirmArmsDownPose()
    {
        // Record the positions of the hands when holding arms straight down.
        leftHandLowerPos  = leftControllerTransform.position  - headsetTransform.position;
        rightHandLowerPos = rightControllerTransform.position - headsetTransform.position;

        // Rotate the positions to have no y-axis rotation
        Vector3 headsetRotation = headsetTransform.rotation.eulerAngles;
        leftHandLowerPos  = Quaternion.Euler(0, -headsetRotation.y, 0) * leftHandLowerPos;
        rightHandLowerPos = Quaternion.Euler(0, -headsetRotation.y, 0) * rightHandLowerPos;

        feedbackText.text = "Get into a T-pose and then press the primary button.";
        armsDownImage.gameObject.SetActive(false);
        tPoseImage.gameObject.SetActive(true);
        isFirstPhase = false;
    }

    private void OnConfirmTPose()
    {
        if (!isSetupCompleted) {
            // Record the positions of the hands when at t-pose
            leftHandHigherPos  = leftControllerTransform.position  - headsetTransform.position;
            rightHandHigherPos = rightControllerTransform.position - headsetTransform.position;

            // Rotate the positions to have no y-axis rotation
            Vector3 headsetRotation = headsetTransform.rotation.eulerAngles;
            leftHandHigherPos  = Quaternion.Euler(0, -headsetRotation.y, 0) * leftHandHigherPos;
            rightHandHigherPos = Quaternion.Euler(0, -headsetRotation.y, 0) * rightHandHigherPos;

            // Calculate the relative position from headset to shoulders
            float shoulderHeight = (leftHandHigherPos.y + rightHandHigherPos.y) / 2;
            Vector3 leftShoulderDelta  = new Vector3(leftHandLowerPos.x , shoulderHeight, leftHandLowerPos.z);
            Vector3 rightShoulderDelta = new Vector3(rightHandLowerPos.x, shoulderHeight, rightHandLowerPos.z);

            Debug.Log($"Left: {leftShoulderDelta}, Right: {rightShoulderDelta}");

            float armLength = (leftHandHigherPos.y - leftHandLowerPos.y + rightHandHigherPos.y - rightHandLowerPos.y)/2;

            // Normalize the shoulder positions to x axis
            //leftShoulderDelta.x = Mathf.Sqrt(Mathf.Pow(leftShoulderDelta.x, 2) + Mathf.Pow(leftShoulderDelta.z, 2));
            //leftShoulderDelta.z = 0;

            // Normalize the shoulder positions to x axis
            //rightShoulderDelta.x = Mathf.Sqrt(Mathf.Pow(rightShoulderDelta.x, 2) + Mathf.Pow(rightShoulderDelta.z, 2));
            //rightShoulderDelta.z = 0;

            // Send the shoulder positions to the manager
            Debug.Log($"Left: {leftShoulderDelta}, Right: {rightShoulderDelta}");
            Manager.Instance.leftShoulderPos = leftShoulderDelta;
            Manager.Instance.rightShoulderPos = rightShoulderDelta;
            Manager.Instance.armLength = armLength;

            feedbackText.text = "Setup complete!";
            feedbackText.text = $"Left shoulder: {leftShoulderDelta}, Right shoulder: {rightShoulderDelta}. \n Your arm length is: {armLength}";
            tPoseImage.gameObject.SetActive(false);
            
            isSetupCompleted = true;
            TcpSender.SetActive(true);
            Invoke("DestroySelf", 10f); // Destroy after 2 seconds
        }
    }

    private void OnButtonPressed() {
        if (isFirstPhase) {
            OnConfirmArmsDownPose();
        }
        else  {
            OnConfirmTPose();
        }
    }

    void OnDestroy()
    {
       // primaryButton.action.performed -= ctx => OnButtonPressed(); // Unsubscribe to prevent memory leaks
    }


    void DestroySelf()
    {
        Destroy(gameObject);
    }
}
