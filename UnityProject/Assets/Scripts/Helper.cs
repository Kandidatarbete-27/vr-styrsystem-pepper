using UnityEngine;
using System;

public class Helper
{
   public static void UpdateAngles(float[] oldAngles, float[] newAngles)
    {
        for (int i = 0; i < oldAngles.Length; i++)
        {
            oldAngles[i] = newAngles[i];
        }
    }

    public static bool HasAngleChanged(float[] currentAngles, float[] lastAngles, float threshold)
    {
        // Checks if any angles have changed from the last time, if so returns true
        for (int i = 0; i < currentAngles.Length; i++)
        {
            if (Mathf.Abs(currentAngles[i] - lastAngles[i]) > threshold)
            {
                return true;
            }
        }
        return false;
    }

    public static bool PositionHasChanged(Vector3 ChangedPos, float threshold)
    {
        // Compares the values to the threshold
        if (Math.Abs(ChangedPos.x) > threshold || Math.Abs(ChangedPos.z) > threshold)
        {
            return true;
        }
        return false;
    }
}
