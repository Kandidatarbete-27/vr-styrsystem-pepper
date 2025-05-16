using UnityEngine;

public class ForwardKinematics
{

    public bool debug = false;

    private float shoulderOffset = 0.15f;
    private float shoulderSideOffset = 0.015f;
    private float upperArmLength = 0.18f;
    private float foreArmLength = 0.15f;
    private float wristOffset = 0.07f;
    private float handOffset = 0.03f;

    public float[] armAngles = new float[5];

    public ForwardKinematics(bool setDebug) {
        debug = setDebug;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (debug)
        {
            Debug.Log("Forward Kinematics Test");
            float[] testAngles = new float[]
            {
                Mathf.PI / 4,
                Mathf.PI / 6,
                Mathf.PI / 3,
                - Mathf.PI / 4,
                Mathf.PI / 2
            };


            bool isLeftArm = true;

            var result = CalculateHandPosition(testAngles, isLeftArm);

            Vector3 position = result.Item1;
            float[,] orientation = result.Item2;
            float[,] jacobian = result.Item3;

            Debug.Log("Position: " + position);
            Debug.Log("Orientation: ");
            for (int i = 0; i < 3; i++)
            {
                string row = "";
                for (int j = 0; j < 3; j++)
                {
                    row += orientation[i, j].ToString("F3") + "\t";  // Print with 3 decimal places
                }
                Debug.Log(row);
            }

            Debug.Log("Jacobian Matrix (3x5):");
            for (int i = 0; i < 3; i++)
            {
                string row = "";
                for (int j = 0; j < 5; j++)
                {
                    row += jacobian[i, j].ToString("F3") + "\t";  // Print with 3 decimal places
                }
                Debug.Log(row);
            }
        }
    }


    public (Vector3, float[,], float[,]) CalculateHandPosition(float[] angles, bool isLeftArm)
    {
        float convertedShoulderOffset = isLeftArm ? shoulderOffset : -shoulderOffset;
        float convertedShoulderSideOffset = isLeftArm ? shoulderSideOffset : -shoulderSideOffset;

        Matrix4x4 T1 = TransformY(-angles[0], 0, 0, 0);                                                      // Shoulder pitch 
        Matrix4x4 T2 = TransformZ(angles[1], 0, 0, 0);                                                       // Shoulder roll 
        Matrix4x4 T3 = TransformY(9.0f / 180.0f * Mathf.PI, upperArmLength, convertedShoulderSideOffset, 0); // Upper arm + angular offset to forearm
        Matrix4x4 T4 = TransformX(angles[2], 0, 0, 0);                                                       // Elbow yaw 
        Matrix4x4 T5 = TransformZ(angles[3], 0, 0, 0);                                                       // Elbow roll 
        Matrix4x4 T6 = TransformX(angles[4], foreArmLength, 0, 0);                                           // Forearm 
        Matrix4x4 T7 = TransformZ(0, wristOffset, 0, -handOffset);                                           // Wrist yaw 

        // Compute forward kinematics
        Matrix4x4 T1Abs = T1;
        Matrix4x4 T2Abs = T1Abs * T2;
        Matrix4x4 T3Abs = T2Abs * T3;
        Matrix4x4 T4Abs = T3Abs * T4;
        Matrix4x4 T5Abs = T4Abs * T5;
        Matrix4x4 T6Abs = T5Abs * T6;
        Matrix4x4 T7Abs = T6Abs * T7;

        // Extract position and orientation
        Vector3 position = T7Abs.GetColumn(3);   // The calculated position
        float[,] rotation = new float[3, 3];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                rotation[i, j] = T7Abs[i, j];
            }
        }

        float[,] jacobian = CalculateJacobian(T1Abs, T2Abs, T3Abs, T4Abs, T5Abs, T6Abs);

        return (position, rotation, jacobian);
    }

    // Function to compute the positional elements of the Jacobian matrix (3x5 matrix)
    float[,] CalculateJacobian(Matrix4x4 T1Abs, Matrix4x4 T2Abs, Matrix4x4 T3Abs, Matrix4x4 T4Abs, Matrix4x4 T5Abs, Matrix4x4 T6Abs)
    {
        // Initialize a 3x5 Jacobian matrix
        float[,] jacobian = new float[3, 5];

        //Calculate offset vectors
        Vector4 offsetT1 = shoulderOffset * T1Abs.GetColumn(1);
        Vector4 offsetT3 = T3Abs * new Vector4(upperArmLength, shoulderSideOffset, 0, 0);
        Vector4 offsetT6 = foreArmLength * T6Abs.GetColumn(0);
        Vector4 offsetT7 = T6Abs * new Vector4(wristOffset, 0, -handOffset, 0);

        // Define the joint axes (we take the relevant columns from each transformation matrix)
        Vector4 j1 = T1Abs.GetColumn(1);
        Vector4 j2 = T2Abs.GetColumn(2);
        Vector4 j3 = T3Abs.GetColumn(1);
        Vector4 j4 = T4Abs.GetColumn(0);
        Vector4 j5 = T5Abs.GetColumn(2);
        Vector4 j6 = T6Abs.GetColumn(0);

        // Define the vectors for the relative positions (offsets) of each joint
        Vector4 vec7 = offsetT7;
        Vector4 vec6 = vec7 + offsetT6;
        Vector4 vec5 = vec6;
        Vector4 vec4 = vec5;
        Vector4 vec3 = vec4 + offsetT3;
        Vector4 vec2 = vec3;
        Vector4 vec1 = vec2 + offsetT1;

        Vector3 J1 = CrossProduct(j1, vec1);
        Vector3 J2 = CrossProduct(j2, vec2);
        Vector3 J3 = CrossProduct(j4, vec4);
        Vector3 J4 = CrossProduct(j5, vec5);
        Vector3 J5 = CrossProduct(j6, vec6);

        for (int i = 0; i < 3; i++)
        {
            jacobian[i, 0] = J1[i];
            jacobian[i, 1] = J2[i];
            jacobian[i, 2] = J3[i];
            jacobian[i, 3] = J4[i];
            jacobian[i, 4] = J5[i];
        }
        return jacobian;
    }

    static Vector3 CrossProduct(Vector4 j, Vector4 v)
    {
        return new Vector3(
            j.y * v.z - j.z * v.y,  // t0
            j.z * v.x - j.x * v.z,  // t1
            j.x * v.y - j.y * v.x   // t2
        );
    }

    Matrix4x4 TransformX(float angle, float x, float y, float z)
    {
        float cosine = Mathf.Cos(angle);
        float sine = Mathf.Sin(angle);
        return new Matrix4x4(
            new Vector4(1, 0, 0, 0),           // X column
            new Vector4(0, cosine, sine, 0),   // Y column
            new Vector4(0, -sine, cosine, 0),  // Z column
            new Vector4(x, y, z, 1)            // Homogeneous coordinate (W column)
        );
    }

    Matrix4x4 TransformY(float angle, float x, float y, float z)
    {
        float cosine = Mathf.Cos(angle);
        float sine = Mathf.Sin(angle);
        return new Matrix4x4(
            new Vector4(cosine, 0, sine, 0),  // X column
            new Vector4(0, 1, 0, 0),          // Y column
            new Vector4(-sine, 0, cosine, 0), // Z column
            new Vector4(x, y, z, 1)           // Homogeneous coordinate (W column)
        );
    }

    Matrix4x4 TransformZ(float angle, float x, float y, float z)
    {
        float cosine = Mathf.Cos(angle);
        float sine = Mathf.Sin(angle);
        return new Matrix4x4(
            new Vector4(cosine, sine, 0, 0),   // X column
            new Vector4(-sine, cosine, 0, 0),  // Y column
            new Vector4(0, 0, 1, 0),           // Z column
            new Vector4(x, y, z, 1)            // Homogeneous coordinate (W column)
        );
    }
}
