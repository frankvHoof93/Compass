using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TiltCompass
{
    public enum Axes : int
        {
            X = 0,
            Y = 1,
            Z = 2
        };
    public class Utils : MonoBehaviour
    {
        public static Vector3 Vector3Abs(Vector3 value)
        {
            Vector3 ans = new Vector3()
            {
                x = Mathf.Abs(value.x),
                y = Mathf.Abs(value.y),
                z = Mathf.Abs(value.z)
            };
            return ans;
        }

        /// <summary>
        /// Returns Vector rotated to match axes, and inverted where needed
        /// Input: Vector3(a,b,c)
        /// Output: Vector3(X,Y,Z)
        /// </summary>
        /// <param name="value">Value to Transform (a,b,c)</param>
        /// <param name="rotation">Axes to rotate to (0=a, 1=b, 2=c)</param>
        /// <param name="invert">Whether to invert axis (a, b, c)</param>
        /// <returns></returns>
        public static Vector3 TransformVector(Vector3 value, Axes[] rotation, bool[] invert)
        {
            // Invert
            value.x *= (invert[0] ? -1 : 1);
            value.y *= (invert[1] ? -1 : 1);
            value.z *= (invert[2] ? -1 : 1);
            // Rotate
            if (rotation[0] != Axes.X || rotation[1] != Axes.Y || rotation[2] != Axes.Z) // Check if it's even necessary to rotate, to save CPU
            {
                Vector3 temp = new Vector3()
                {
                    x = GetAxes(value, rotation[0]),
                    y = GetAxes(value, rotation[1]),
                    z = GetAxes(value, rotation[2])
                };
                value = temp;
            }
            return value;
        }

        /// <summary>
        /// Returns value from Vector based on axis
        /// </summary>
        /// <param name="values">Vector to get axis from</param>
        /// <param name="axis">Axis to get (values.x, values.y, values.z)</param>
        /// <returns></returns>
        public static float GetAxes(Vector3 values, Axes axis)
        {
            return values[(int)axis];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="expectedAxis"></param>
        /// <param name="expectedValue"></param>
        /// <param name="inversions"></param>
        /// <param name="axes"></param>
        public static void SetAxis(Vector3 value, Axes expectedAxis, int expectedValue, ref bool[] inversions, ref Axes[] axes)
        {
            List<float> AbsVals = new List<float>() { Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z) };
            // 0 = a, 1 = b, 2 = c
            int indexOfMaxVal = AbsVals.IndexOf(AbsVals.Max());
            // Distance between expected & actual > 1  -> actual = -1 * expected
            inversions[indexOfMaxVal] = (Mathf.Abs(value[indexOfMaxVal] - expectedValue) > 1);
            // Set tranformationAxis for internal axis (e.g. a = X)
            axes[indexOfMaxVal] = expectedAxis;
        }

        /// <summary>
        /// Checks whether the current AccelerometerOrientation is valid for checking
        /// This means 1 of the axes is BIG, whilst the other 2 are SMALL
        /// (1 value bigger than .6f and 2 values smaller than .4f)
        /// </summary>
        /// <param name="accelVector"></param>
        /// <returns></returns>
        public static bool IsCheckableAccelerometerOrientation(Vector3 accelVector)
        {
            // Value is 2 if 2 axes are below .4f in size AND one is above .6f in size (.5f+.5f+1f)
            float val = AxisValue(accelVector.x) + AxisValue(accelVector.y) + AxisValue(accelVector.z);
            if (val != 2) return false;
            // Value CAN be true if 2 axes are above .6f and one is between .4f and .6f (0+1+1)
            if (AxisValue(accelVector.x) == 0 || AxisValue(accelVector.y) == 0 || AxisValue(accelVector.z) == 0)
            {
                // 1 value is between .4 and .6
                // return false is the other 2 are above .6
                if ((AxisValue(accelVector.x) == 1 && AxisValue(accelVector.y) == 1) || (AxisValue(accelVector.x) == 1 && AxisValue(accelVector.z) == 1) || (AxisValue(accelVector.y) == 1 && AxisValue(accelVector.z) == 1))
                    return false;
            }
            // 1 of the values = 1, the two others are .5
            return true;
        }

        /// <summary>
        /// Checks 1 single axis for IsCheckableOrientation
        /// </summary>
        /// <param name="value">Value of Axis</param>
        /// <returns>.5 if value size is below 0.4, 1 if value size is above .6, 0 if value size is between .4 and .6</returns>
        private static float AxisValue(float value)
        {
            if (Mathf.Abs(value) < 0.4f)
                return .5f;
            else if (Mathf.Abs(value) > 0.6f)
                return 1;
            else return 0;
        }
    }
}