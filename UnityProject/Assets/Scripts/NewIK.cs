using UnityEngine;

public class NewIK : MonoBehaviour
{
    public bool debug = false;

    void Start()
    {
        if (debug)
        {
            Vector3 shoulderPosition = new Vector3(0, 0, 1.5f); // Shoulder at chest level
            Vector3 wristPosition = new Vector3(0.2f, 0.1f, 1.4f); // Hand forward and slightly downward

            bool isLeftArm = true;
            float[] angles = GetArmAngles(shoulderPosition, wristPosition, isLeftArm);
            Debug.Log($"Shoulder Pitch: {angles[0]} Shoulder Roll: {angles[1]} Elbow Yaw: {angles[2]} Elbow Roll: {angles[3]}");
            Debug.Log($"In degrees: Shoulder Pitch: {angles[0] * Mathf.Rad2Deg} Shoulder Roll: {angles[1] * Mathf.Rad2Deg} Elbow Yaw: {angles[2] * Mathf.Rad2Deg} Elbow Roll: {angles[3] * Mathf.Rad2Deg}");
        }
    }

    private Vector3 EstimateElbowPosition(Vector3 shoulderPos, Vector3 wristPos, bool isLeftArm)
    {
        float upperArmLength = 0.18f; // 180 mm
        float forearmLength = 0.15f;  // 150 mm

        Vector3 shoulderToWrist = wristPos - shoulderPos;
        float totalArmLength = shoulderToWrist.magnitude;

        // Clamp within reachable arm limits
        totalArmLength = Mathf.Clamp(totalArmLength, Mathf.Abs(upperArmLength - forearmLength), upperArmLength + forearmLength);

        // Law of Cosines for elbow bend angle
        float cosElbowAngle = (upperArmLength * upperArmLength + totalArmLength * totalArmLength - forearmLength * forearmLength) / (2 * upperArmLength * totalArmLength);
        float elbowBendAngle = Mathf.Acos(Mathf.Clamp(cosElbowAngle, -1f, 1f));

        Vector3 elbowBendDirection = isLeftArm ? Vector3.left : Vector3.right;
        Vector3 planeNormal = Vector3.Cross(shoulderToWrist.normalized, elbowBendDirection).normalized;

        Quaternion elbowRotation = Quaternion.AngleAxis(elbowBendAngle * Mathf.Rad2Deg, planeNormal);
        Vector3 finalElbowDirection = elbowRotation * shoulderToWrist.normalized;

        return shoulderPos + finalElbowDirection * upperArmLength;
    }

    private float[] GetArmAngles(Vector3 shoulderPosition, Vector3 wristPosition, bool isLeftArm)
    {
        Vector3 elbowPosition = EstimateElbowPosition(shoulderPosition, wristPosition, isLeftArm);
        Vector3 wristPositionMM = wristPosition * 1000;
        Vector3 elbowPositionMM = elbowPosition * 1000;

        (float shoulderPitch, float shoulderRoll) = GetArmPartialAngles(elbowPositionMM, isLeftArm);
        float elbowRoll = CalculateTheta4(shoulderPitch, shoulderRoll, wristPositionMM, isLeftArm);
        float elbowYaw = CalculateTheta3(shoulderPitch, shoulderRoll, elbowRoll, wristPositionMM, isLeftArm);

        return new float[] { shoulderPitch, shoulderRoll, elbowYaw, elbowRoll, 0 };
    }

    public static (float shoulderPitch, float shoulderRoll) GetArmPartialAngles(Vector3 elbowPosition, bool isLeftArm)
    {
        float shoulderRollLimit = Mathf.Deg2Rad * 90;
        float shoulderPitchLimit = Mathf.Deg2Rad * 120;

        float l1 = -57.0f, l3 = 86.82f, l4 = 181.2f, l6 = 0.13f;
        float l2 = isLeftArm ? 149.74f : -149.74f;
        float l5 = isLeftArm ? 15.0f : -15.0f;

        float ey = elbowPosition.y;
        float ex = elbowPosition.x;
        float ez = elbowPosition.z;

        float shoulderRoll = Mathf.Asin((ey - l2) / Mathf.Sqrt(l4 * l4 + l5 * l5)) - Mathf.Atan(l5 / l4);
        shoulderRoll = Mathf.Clamp(shoulderRoll, -shoulderRollLimit, shoulderRollLimit);

        float n = l4 * Mathf.Cos(shoulderRoll) - l5 * Mathf.Sin(shoulderRoll);
        float shoulderPitch = Mathf.Atan2(ex - l1, ez - l3) - Mathf.Atan2(Mathf.Sqrt((ex - l1) * (ex - l1) + (ez - l3) * (ez - l3) - l6 * l6), l6);
        shoulderPitch = Mathf.Clamp(shoulderPitch, -shoulderPitchLimit, shoulderPitchLimit);

        return (shoulderPitch, shoulderRoll);
    }

    public static float CalculateTheta4(float t1, float t2, Vector3 wristPosition, bool isLeftArm)
    {
        float elbowRollLimit = Mathf.Deg2Rad * 90;

        float l1 = -57.0f, l3 = 86.82f, l4 = 150f, d3 = 181.2f, z3 = 0.13f;
        float l2 = isLeftArm ? 149.74f : -149.74f;
        float alpha = Mathf.Deg2Rad * 9f;

        float term3 = (wristPosition.x - l1) * Mathf.Sin(t1) + (wristPosition.z - l3) * Mathf.Cos(t1) - z3;
        float term4 = (wristPosition.z - l3) * Mathf.Sin(t1) * Mathf.Sin(t2) + (wristPosition.y - l2) * Mathf.Cos(t2) - d3 - (wristPosition.x - l1) * Mathf.Sin(t2) * Mathf.Cos(t1);
        float term2 = Mathf.Sin(alpha) * term3 + Mathf.Cos(alpha) * term4;
        float theta4 = Mathf.Acos(Mathf.Clamp(term2 / l4, -1f, 1f));

        theta4 = isLeftArm ? -theta4 : theta4;
        return Mathf.Clamp(theta4, -elbowRollLimit, elbowRollLimit);
    }

    public static float CalculateTheta3(float t1, float t2, float t4, Vector3 wristPosition, bool isLeftArm)
    {
        float elbowYawLimit = Mathf.Deg2Rad * 135;

        float d3 = 181.2f, d5 = 150f, l2 = isLeftArm ? 149.74f : -149.74f;
        float a3 = isLeftArm ? -15.0f : 15.0f;

        float alpha = Mathf.Deg2Rad * 9f;

        float aterm = (d3 + d5 * Mathf.Cos(alpha) * Mathf.Cos(t4)) / (d5 * Mathf.Sin(t4) * Mathf.Sin(alpha));
        float bterm = (a3 - Mathf.Cos(t2) * (wristPosition.y - l2)) / (d5 * Mathf.Sin(t4));

        float theta3 = Mathf.Atan2(aterm, bterm);
        return Mathf.Clamp(theta3, -elbowYawLimit, elbowYawLimit);
    }
}
