using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using System;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Linq;


public class InverseKinematics
{
    public bool debug = false;
    public bool debugFinal = false;
    public bool debugFull = false;
    ForwardKinematics fk = new ForwardKinematics(false);

    //Pepper's right arm joint limits
    public static readonly (float min, float max)[] RightJointLimits = {
        (-2.0857f, 2.0857f),
        (-1.5620f, -0.0087f),
        (-2.0857f, 2.0857f),
        (0.0087f, 1.5620f),
        (-1.8239f, 1.8239f)
    };

    //Pepper's left arm joint limits
    public static readonly (float min, float max)[] LeftJointLimits = {
        (-2.0857f, 2.0857f),
        (0.0087f, 1.5620f),
        (-2.0857f , 2.0857f),
        (-1.5620f, -0.0087f),
        (-1.8239f, 1.8239f)
    };


    void Start()
    {
        float[] testAngles = new float[]
        {
            Mathf.PI / 4,
            Mathf.PI / 6,
            Mathf.PI / 3,
            - Mathf.PI / 4,
            Mathf.PI / 2
        };
        float[] startingAngles = new float[]
        {
            0,0,0,0,0
        };
        if (debug)
        {
            fk.debug = true;
            Debug.Log("Forward Kinematics Test");

            bool isLeftArm = true;
            var result = fk.CalculateHandPosition(testAngles, isLeftArm);

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

            Debug.Log("Inverse Kinematics Test");
            Matrix<float> inverseJacobian = InverseJacobian(jacobian);

            Debug.Log("Inverse Jacobian Matrix (5x3):");
            for (int i = 0; i < 5; i++)
            {
                string row = "";
                for (int j = 0; j < 3; j++)
                {
                    row += inverseJacobian[i, j].ToString("F3") + "\t";  // Print with 3 decimal places
                }
                Debug.Log(row);
            }

            if (debugFinal)
            {
                float[] finalresult = CalculateJointAngles(position, startingAngles, true, 0.01f);
                Debug.Log("Final Angles: ");
                for (int i = 0; i < finalresult.Length; i++)
                {
                    Debug.Log(finalresult[i]);
                }
            }
        }

        if (debugFull)
        {
            float[] testArr = new float[] { 0f, 0f, 0f, 0f, 0f };
            Vector3 target = new Vector3(0.39f, 0.117f, 0.0124f);
            float[] result = CalculateJointAngles(target, testArr, true, 0.05f);

            Debug.Log("Final Angles: ");
            for (int i = 0; i < result.Length; i++)
            {
                Debug.Log(result[i]);
            }
        }

    }

    private float[] EnforceJointLimits(float[] angles, bool isLeftArm)
    {
        var limits = isLeftArm ? LeftJointLimits : RightJointLimits;
        for (int i = 0; i < angles.Length; i++)
        {
            angles[i] = Mathf.Clamp(angles[i], limits[i].min, limits[i].max);
        }
        return angles;
    }

    public static Vector<float> ToVector(Vector3 vector)
    {
        return Vector<float>.Build.DenseOfArray(new float[] { vector.x, vector.y, vector.z });
    }

    public float[] CalculateJointAngles(Vector3 targetPosition, float[] startingAngles, bool isLeftArm, float precision = 0.01f, int max_iters = 100)
    {
        float[] returnAngles = startingAngles.Clone() as float[];
        float[] previousAngles = startingAngles.Clone() as float[];
        float oldDeltaError = 100f;
        float damping_factor = 0.1f;
        bool isFirstTry = true;

        for (int i = 0; i < max_iters; i++)
        {
            // Perform forward kinematics on the current angles to get the current hand position and inverse jacobian
            var result = fk.CalculateHandPosition(returnAngles, isLeftArm);
            Vector3 currentPosition = result.Item1;
            Matrix<float> invJacobian = InverseJacobian(result.Item3, 0.05f);

            Vector3 delta_pos = targetPosition - currentPosition;
            Vector<float> delta_angles = invJacobian * ToVector(delta_pos);

            // Apply secondary task to avoid joint limits
            var limits = isLeftArm ? LeftJointLimits : RightJointLimits;
            Vector<float> secondaryTask = Vector<float>.Build.Dense(5, 0);

            for (int j = 0; j < returnAngles.Length; j++)
            {
                if (returnAngles[j] < limits[j].min + 0.1f)
                {
                    secondaryTask[j] = 0.1f;
                }
                else if (returnAngles[j] > limits[j].max - 0.1f)
                {
                    secondaryTask[j] = -0.1f;
                }
            }

            // Null-space projection
            var J = Matrix<float>.Build.DenseOfArray(result.Item3);
            var J_pinv = J.PseudoInverse();
            var I = Matrix<float>.Build.DenseIdentity(5);
            var N = I - J_pinv * J;
            delta_angles += N * secondaryTask;

            // Update angles with damping to avoid big steps
            for (int j = 0; j < returnAngles.Length; j++)
            {
                returnAngles[j] += damping_factor * delta_angles[j];
            }
            returnAngles = EnforceJointLimits(returnAngles, isLeftArm);

            // Check if the error is within the desired precision
            float deltaError = Mathf.Abs(delta_pos.x) + Mathf.Abs(delta_pos.y) + Mathf.Abs(delta_pos.z);
            if (deltaError < precision)
            {

                return returnAngles;
            }
            else if (deltaError > oldDeltaError)
            {
                damping_factor *= 0.5f;
            }

            if (Mathf.Abs(deltaError - oldDeltaError) < precision / 100)
            {
                if (isFirstTry)
                {
                    isFirstTry = false;
                    returnAngles = isLeftArm ? new float[] { 0f, 0.85f, 0f, -0.85f, 0f }
                                             : new float[] { 0f, -0.85f, 0f, 0.85f, 0f };
                }
                else
                {
                    System.Random rand = new System.Random();

                    // Select the correct joint limits array based on the arm side
                    var jointLimits = isLeftArm ? LeftJointLimits : RightJointLimits;

                    // Generate random angles within the specified limits
                    float[] randomAngles = jointLimits.Select(limit =>
                        (float)(rand.NextDouble() * (limit.max - limit.min) + limit.min)
                    ).ToArray();

                    returnAngles = randomAngles;
                }

                // Reset the damping factor and old delta error
                oldDeltaError = 100f;
                damping_factor = 0.1f;
            }
            oldDeltaError = deltaError;
        }

        //Debug.LogError("No solution found");
        return previousAngles;

    }


    private Matrix<float> InverseJacobian(float[,] jacobian, float damping = 0.01f)
    {
        int rows = jacobian.GetLength(0);
        int cols = jacobian.GetLength(1);

        // Convert float[,] to Matrix<float>
        Matrix<float> J = DenseMatrix.OfArray(jacobian);

        // Transpose of J
        Matrix<float> J_T = J.Transpose();

        // Compute damped term: (J * J_T + damping^2 * I)^-1
        Matrix<float> dampingMatrix = (damping * damping) * DenseMatrix.CreateIdentity(rows);
        Matrix<float> dampedTermInv = (J * J_T + dampingMatrix).Inverse();

        // Compute inverse Jacobian
        Matrix<float> J_inv = J_T * dampedTermInv;
        return J_inv;
    }
}