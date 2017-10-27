using System;
using UnityEngine;

namespace TiltCompass
{
    public class Storage {

        private const string AccelOrientationX = "AccelOrientA";
        private const string AccelOrientationY = "AccelOrientB";
        private const string AccelOrientationZ = "AccelOrientC";
        private const string AccelInvertX = "AccelInvertA";
        private const string AccelInvertY = "AccelInvertB";
        private const string AccelInvertZ = "AccelInvertC";

        public static void ClearAllData()
        {
            PlayerPrefs.DeleteAll();
        }

        public static void LoadAccelerometerOrientation(ref Axes[] axes, ref bool[] invert)
        {
            // Reset arrays to proper sizes
            axes = new Axes[3];
            invert = new bool[3];
            if (!AccelParametersExist())
                ResetAccelerometerOrientation();
            // Axes
            axes[0] = ToEnum<Axes>(PlayerPrefs.GetString(AccelOrientationX));
            axes[1] = ToEnum<Axes>(PlayerPrefs.GetString(AccelOrientationY));
            axes[2] = ToEnum<Axes>(PlayerPrefs.GetString(AccelOrientationZ));
            // Inversions
            invert[0] = ToBool(PlayerPrefs.GetString(AccelInvertX));
            invert[1] = ToBool(PlayerPrefs.GetString(AccelInvertY));
            invert[2] = ToBool(PlayerPrefs.GetString(AccelInvertZ));
        }

        public static void SaveAccelerometerOrientation(Axes[] axes, bool[] invert)
        {
            SetParameters(axes, invert);
            PlayerPrefs.Save();
        }

        public static void ResetAccelerometerOrientation()
        {
            Axes[] defaultAxes = new Axes[] { Axes.X, Axes.Y, Axes.Z };
            bool[] defaultInvert = new bool[] { false, false, false };
            SetParameters(defaultAxes, defaultInvert);
            PlayerPrefs.Save();
        }

        private static void SetParameters(Axes[] axes, bool[] invert)
        {
            PlayerPrefs.SetString(AccelOrientationX, axes[0].ToString());
            PlayerPrefs.SetString(AccelOrientationY, axes[1].ToString());
            PlayerPrefs.SetString(AccelOrientationZ, axes[2].ToString());
            PlayerPrefs.SetString(AccelInvertX, invert[0].ToString());
            PlayerPrefs.SetString(AccelInvertY, invert[1].ToString());
            PlayerPrefs.SetString(AccelInvertZ, invert[2].ToString());
        }

        private static bool AccelParametersExist()
        {
            return (PlayerPrefs.HasKey(AccelOrientationX) && PlayerPrefs.HasKey(AccelOrientationY) && PlayerPrefs.HasKey(AccelOrientationZ) &&
                PlayerPrefs.HasKey(AccelInvertX) && PlayerPrefs.HasKey(AccelInvertY) && PlayerPrefs.HasKey(AccelInvertZ));
        }

        private static T ToEnum<T>(string val)
        {
            return (T)Enum.Parse(typeof(T), val);
        }

        private static bool ToBool(string val)
        {
            return Boolean.Parse(val);
        }
    }
}