using UnityEngine;

public class IKtesting : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InverseKinematics ik = new InverseKinematics();
        Vector3 shoulderPos = Vector3.zero;
        Vector3 armPos = new Vector3(-0.35f, 0, 0);
        float[] startingGuess = { 0f, 0f, 0f, -0.05f, 0f };
        float[] returnAngles = ik.CalculateJointAngles(armPos, startingGuess, true, 0.02f, 100);
        Debug.Log($"Return angles: {returnAngles[0]}  {returnAngles[1]}  {returnAngles[2]}  {returnAngles[3]}  {returnAngles[4]}");
        Debug.Log($"In degrees: {Mathf.Rad2Deg * returnAngles[0]}  {Mathf.Rad2Deg * returnAngles[1]}  {Mathf.Rad2Deg * returnAngles[2]}  {Mathf.Rad2Deg * returnAngles[3]}  {Mathf.Rad2Deg * returnAngles[4]}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
