using UnityEngine;

namespace TiltCompass
{
    public class Utils : MonoBehaviour
    {
        public static float DegSin(float degrees)
        {
            return Mathf.Rad2Deg * Mathf.Sin(Mathf.Deg2Rad * degrees);
        }

        public static float DegCos(float degrees)
        {
            return Mathf.Rad2Deg * Mathf.Cos(Mathf.Deg2Rad * degrees);
        }

        public static float DegTan(float degrees)
        {
            return Mathf.Rad2Deg * Mathf.Tan(Mathf.Deg2Rad * degrees);
        }

        public static float DegAsin(float value)
        {
            return Mathf.Rad2Deg * Mathf.Asin(value);
        }

        public static float DegAcos(float value)
        {
            return Mathf.Rad2Deg * Mathf.Acos(value);
        }

        public static float DegAtan(float value)
        {
            return Mathf.Rad2Deg * Mathf.Atan(value);
        }

        public static float DegAtan2(float y, float x)
        {
            return Mathf.Rad2Deg * Mathf.Atan2(y, x);
        }
    }
}