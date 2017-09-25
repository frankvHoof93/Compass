using System;
using System.Collections;
using UnityEngine;

namespace TiltCompass
{
    public enum Axes
    {
        X,
        Y,
        Z
    };

    public class TiltCompensatedCompass : MonoBehaviour
    {
        /// <summary>
        /// Internal axes: 0 = x, 1 = y, 2 = z
        /// Used to hold rotation of accelerometer in device
        /// </summary>
        [SerializeField]
        public Axes[] accelerometerAxes = { Axes.X, Axes.Y, Axes.Z };
        /// <summary>
        /// Invert any of the Accelerometer Sensors
        /// </summary>
        [SerializeField]
        private bool invertAccelX;
        [SerializeField]
        private bool invertAccelY;
        [SerializeField]
        private bool invertAccelZ;

        /// <summary>
        /// Unity remote on?
        /// Delays boot to let Unity Remote start
        /// </summary>
        [SerializeField]
        private bool UnityRemote;
        /// <summary>
        /// TimeOut for Location Boot
        /// </summary>
        [SerializeField]
        private float timeout;

        // Use this for initialization
        void Start()
        {

            Debug.Log("Starting Compass");
            Input.compensateSensors = false;
            Input.compass.enabled = true;
            StartCoroutine(StartLocationServices());
        }
        
        // Update is called once per frame
        void Update()
        {
            if (Input.location.status != LocationServiceStatus.Running)
                return;
         //   Debug.Log(Input.compensateSensors);
          //  Debug.Log(Input.deviceOrientation);
           // Debug.Log("Accel: " + Input.acceleration);
            float Roll, Pitch, Yaw;
            if (Input.deviceOrientation == DeviceOrientation.Portrait || Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
            {
                Roll = CalculcateRollPortrait();
                Pitch = CalculateRollLandscape(Input.acceleration);
            }
            else
            {
                Roll = CalculateRollLandscape(Input.acceleration);
                Pitch = 0;
            }

            Debug.Log("Roll: " + Roll);
            float RadRoll = Mathf.Deg2Rad * Roll;
            float SinRoll = Mathf.Rad2Deg * Mathf.Sin(RadRoll);
            float CosRoll = Mathf.Rad2Deg * Mathf.Cos(RadRoll);
            //float Pitch = Mathf.Rad2Deg * Mathf.Atan2(-Input.acceleration.x, Input.acceleration.y * SinRoll + Input.acceleration.z * CosRoll);
            Debug.Log("Pitch: " + Pitch);
        }

        public void CheckAccelerometerOrientation()
        {
            Vector3 accel = Input.acceleration;
            if (!IsCheckableOrientation(accel))
                return;
            float Expected = 0;
            int InternalAxes = 0;
            switch (Input.deviceOrientation)
            {
                case DeviceOrientation.Unknown:
                    Debug.LogError("Device Orientation Unknown. Cannot check AccelerometerOrientation");
                    return;
                case DeviceOrientation.Portrait:
                    // Expected: Down (0, -1, 0)
                    Expected = -1;
                    InternalAxes = 1;
                    break;
                case DeviceOrientation.PortraitUpsideDown:
                    // Expected: Up   (0, 1, 0)
                    Expected = 1;
                    InternalAxes = 1;
                    break;
                case DeviceOrientation.LandscapeLeft:
                    // Expected: Left (-1, 0, 0)                    
                    Expected = -1;
                    InternalAxes = 0;
                    break;
                case DeviceOrientation.LandscapeRight:
                    // Expected: Right(1, 0, 0)
                    Expected = 1;
                    InternalAxes = 0;
                    break;
                case DeviceOrientation.FaceUp:
                    // Expected: Back (0, 0, -1)
                    Expected = -1;
                    InternalAxes = 2;
                    break;
                case DeviceOrientation.FaceDown:
                    // Expected: Forward(0, 0, 1)
                    Expected = 1;
                    InternalAxes = 2;
                    break;
            }
            bool isInverted = false;
            Axes axes = ClosestAxes(accel, Expected, ref isInverted);
            if (accelerometerAxes[InternalAxes] != axes)
            {
                Debug.Log("AAAAAA");
            }
            accelerometerAxes[InternalAxes] = axes;
            if (InternalAxes == 0)
                invertAccelX = isInverted;
            else if (InternalAxes == 1)
                invertAccelY = isInverted;
            else if (InternalAxes == 2)
                invertAccelZ = isInverted;
        }

        // Checks for 2 values being smaller than .5f, and 1 being bigger
        private bool IsCheckableOrientation(Vector3 vector)
        {
            return (Mathf.Abs(vector.x) < .5f ? 1 : 0) + (Mathf.Abs(vector.y) < .5f ? 1 : 0) + (Mathf.Abs(vector.z) < .5f ? 1 : 0) == 2;
        }
        
        /// <summary>
        /// Ensure Orientation is checkable first
        /// </summary>
        /// <param name="value"></param>
        /// <param name="compare"></param>
        /// <param name="isInverted"></param>
        /// <returns></returns>
        private Axes ClosestAxes(Vector3 value, float compare, ref bool isInverted)
        {
            Axes axes = Axes.X;
            float val1 = 0;
            if (Mathf.Abs(value.x) >= Mathf.Abs(value.y) && Mathf.Abs(value.x) >= Mathf.Abs(value.z)) // Check input x
            {
                val1 = value.x;
                axes = Axes.X;
            }
            else if (Mathf.Abs(value.y) >= Mathf.Abs(value.x) && Mathf.Abs(value.y) >= Mathf.Abs(value.z)) // Check input y
            {
                val1 = value.y;
                axes = Axes.Y;
            }
            else if (Mathf.Abs(value.z) >= Mathf.Abs(value.x) && Mathf.Abs(value.z) >= Mathf.Abs(value.y)) // Check input z
            {
                val1 = value.z;
                axes = Axes.Z;
            }            
            if (Mathf.Abs(val1 - compare) > 1)
                isInverted = true;
            else isInverted = false;
            return axes;
        }
        
        

        private float CalculateRollLandscape(Vector3 acceleration)
        {
            float Roll = (90f + Mathf.Rad2Deg * Mathf.Atan2(acceleration.z, acceleration.x));
            if (Roll < 0)
                Roll += 360;
            return Roll;
        }

        private float CalculatePitchPortrait(Vector3 acceleration)
        {
            float Pitch = (90f + Mathf.Rad2Deg * Mathf.Atan2(acceleration.y, acceleration.z));
            if (Pitch < 0)
                Pitch += 360;
            return Pitch;
        }

        private float CalculcateRollPortrait()
        {
            return 0;
        }

        IEnumerator StartLocationServices()
        {
            Debug.Log("Starting GPS");
            // Wait until the editor and unity remote are connected before starting a location service
            if (UnityRemote)
                yield return new WaitForSeconds(2);
            Input.location.Start(1, 1);
            if (UnityRemote)
                yield return new WaitForSeconds(2);
            if (!Input.location.isEnabledByUser || Input.location.status == LocationServiceStatus.Failed || Input.location.status == LocationServiceStatus.Stopped)
            {
                Debug.LogError("GPS not available");
                yield break;
            }
            while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
            {
                timeout--;
                Debug.Log(Input.location.status);
                yield return new WaitForSeconds(1);
            }
            if (timeout <= 0)
            {
                Debug.LogError("GPS: Init timed out");
                yield break;
            }
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("GPS: Unable to init GPS");
                yield break;
            }
            Debug.Log("GPS Running");
            Input.compensateSensors = true;
            yield break;
        }

        private Vector3 GetAccelerometer()
        {
            Vector3 accel = Input.acceleration;
            if (accelerometerAxes[0] != Axes.X || accelerometerAxes[1] != Axes.Y || accelerometerAxes[2] != Axes.Z)
            {
                Vector3 temp = Vector3.zero;
                temp.x = GetAxes(accel, accelerometerAxes[0]);
                temp.y = GetAxes(accel, accelerometerAxes[1]);
                temp.z = GetAxes(accel, accelerometerAxes[2]);
                accel = temp;
            }
            return accel;
        }

        private float GetAxes(Vector3 values, Axes axes)
        {
            switch (axes)
            {
                case Axes.X:
                    return values.x;
                case Axes.Y:
                    return values.y;
                case Axes.Z:
                    return values.z;
                default:
                    return 0;
            }
        }

        private void UpdateAccelerometer()
        {

        }

        private void OnDrawGizmos()
        {
            if (Input.location.status != LocationServiceStatus.Running)
                return;
            Gizmos.color = Color.green;
            Vector3 accel = Input.acceleration;
            if (invertAccelX)
                accel.x *= -1;
            if (invertAccelY)
                accel.y *= -1;
            if (invertAccelZ)
                accel.z *= -1;
            Gizmos.DrawLine(this.transform.position, this.transform.position + accel);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(this.transform.position, this.transform.position + Input.compass.rawVector);
            //Debug.Log(Input.compass.rawVector);

        }
    }
}