using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace TiltCompass
{
    [DisallowMultipleComponent]
    public class TiltCompensatedCompass : MonoBehaviour
    {
        private readonly Quaternion rotateQuat = new Quaternion(1, 0, 0, 1);
        public Axes NorthAxis;
        public bool NorthAxisInverted;
        public Axes EastAxis;
        public bool EastAxisInverted;
        public Axes DownAxis;
        public bool DownAxisInverted;

        [Range(0,1)]
        public float AccelSmooth;
        [Range(0, 1)]
        public float MagnSmooth;
        public Text tf;

        /// <summary>
        /// Internal axes: 0 = a, 1 = b, 2 = c => (Vec3(a,b,c)) -> Vec3(X,Y,Z)/Vec3(Y,Z,X)/etc.
        /// Used to hold rotation of accelerometer in device
        /// </summary>
        [SerializeField]
        public Axes[] accelerometerAxes = { Axes.X, Axes.Y, Axes.Z };
        /// <summary>
        /// Internal axes: 0 = a, 1 = b, 2 = c
        /// Inversion is applied BEFORE transformation
        /// </summary>
        [SerializeField]
        public bool[] accelAxesInverted = { false, false, false };

        [SerializeField]
        public Axes[] magnetometerAxes = { Axes.X, Axes.Y, Axes.Z };
        [SerializeField]
        public bool[] magnAxesInverted = { false, false, false };

        /// <summary>
        /// Transformed & inverted Accelerometer Reading
        /// X-Axis points to top of device, Y-Axis points to right of device, Z-Axis points to front of device
        /// Also known as NEU (North, East, Up)
        /// This is NOT NED, because Unity has an inverted z-axis compared to NED
        /// </summary>
        public Vector3 Accelerometer;
        public Vector3 Magnetometer;

        public float TestAngle;

        public Vector3 AccelerometerNormalized { get { return Accelerometer.normalized; } }
        public Vector3 MagnetometerNormalized { get { return Magnetometer.normalized; } }

        [SerializeField]
        private Vector3 HardIronMinimums;
        [SerializeField]
        private Vector3 HardIronMaximums;
        [SerializeField]
        private Vector3 RawMagnetometer;

        private Quaternion referanceRotation;

        #region Kalman
        /// <summary>
        /// Estimate of orientation by gyro
        /// </summary>
        private Quaternion GyroEstimate;
        /// <summary>
        /// Measured orientation by Accel + Magn
        /// </summary>
        private Quaternion MeasuredQuat;
        #endregion

        /// <summary>
        /// Unity remote on?
        /// Delays boot to let Unity Remote start
        /// </summary>
        [SerializeField]
        private bool UnityRemote;
        /// <summary>
        /// TimeOut for Location Boot
        /// </summary>
        [SerializeField, Range(0, 300)]
        private float timeout;

        /// <summary>
        /// Values for Earth's Magnetic field (Range 20-70 MicroTeslas)
        /// </summary>
        [SerializeField]
        private float MagnetMin = 20;
        /// <summary>
        /// Values for Earth's Magnetic field (Range 20-70 MicroTeslas)
        /// </summary>
        [SerializeField]
        private float MagnetMax = 70;

        private List<Vector3> accelData = new List<Vector3>();
        private List<Vector3> magnData = new List<Vector3>();
        private List<float> angleData = new List<float>();

        private List<Vector3> magnCalib = new List<Vector3>();

        public Transform AccelMagnOrientation;
        public Transform ActualPos;

        private bool isCalibrating = false;
        public Vector3 magnOffset = Vector3.zero;

        #region Gyro

        private Quaternion baseOrientation = Quaternion.Euler(90, 0, 0);
        #endregion
        // Use this for initialization
        void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            // Load AccelerometerOrientation from PlayerPrefs
            Storage.LoadAccelerometerOrientation(ref this.accelerometerAxes, ref this.accelAxesInverted);
            Input.compensateSensors = false;
           
            Input.gyro.enabled = true;
            //Input.gyro.updateInterval = 1f / 60f;
            Input.gyro.updateInterval = 1f / AHRS3.sampleFreq;
            StartCoroutine(StartLocationServices());
            Quaternion calibration;
            var fw = (Input.gyro.attitude) * (-Vector3.forward);
            fw.z = 0;
            if (fw == Vector3.zero)
            {
                calibration = Quaternion.identity;
            }
            else
            {
                calibration = (Quaternion.FromToRotation(Quaternion.identity * Vector3.up, fw));
            }
            referanceRotation = Quaternion.Inverse(baseOrientation) * Quaternion.Inverse(calibration);
        }

        void UpdateGyroRotation()
        {
            GyroEstimate = ConvertRotation(referanceRotation * Input.gyro.attitude);
            Vector3 AngularVelocity = Input.gyro.rotationRateUnbiased; // Has been processed to remove bias & give more accurate measurement
                                                                       // AngularVelocity is in RADIANS PER SECOND
            Debug.Log("G"+ AngularVelocity * Time.deltaTime);
            // Debug.Log(referanceRotation);
            // TODO: Remove this rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity * (ConvertRotation(referanceRotation * Input.gyro.attitude)) * Quaternion.identity, 0.2f);
            //transform.up = Vector3.Lerp(transform.up, -Accelerometer, 0.8f);
        }

        /// <summary>
        /// Converts the rotation from right handed to left handed.
        /// </summary>
        /// <returns>
        /// The result rotation.
        /// </returns>
        /// <param name='q'>
        /// The rotation to convert.
        /// </param>
        private static Quaternion ConvertRotation(Quaternion q)
        {
            return new Quaternion(q.x, q.z, q.y, -q.w);
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.location.status != LocationServiceStatus.Running)
                return;
            
            UpdateSensorMeasurement();

            if (isCalibrating)
                magnCalib.Add(new Vector3(Input.compass.rawVector.x, Input.compass.rawVector.z, Input.compass.rawVector.y));
            Magnetometer -= magnOffset;

            //UpdateGyroRotation();
            //AccelMagnOrientation.rotation = MeasuredQuat;
            //transform.rotation = ConvertRotation(AHRS.PerformUpdate(transform.rotation, Input.gyro.rotationRateUnbiased, Input.acceleration, Input.compass.rawVector));
            Vector3 g = -(Input.gyro.rotationRate / 2f); // Gyro is twice as fast when building? :S
            //AccelMagnOrientation.rotation = ConvertRotation(Input.gyro.attitude);
            //            Debug.Log(Input.gyro.rotationRate);
            //AHRS2.Update(g.x, g.z, g.y, Accelerometer.x, Accelerometer.y, Accelerometer.z);
            AHRS2.Update(Input.gyro.attitude, g.x, g.z, g.y, Accelerometer.x, Accelerometer.y, Accelerometer.z, Magnetometer.x, Magnetometer.y, Magnetometer.z);


            ActualPos.rotation = new Quaternion(0, -1, 0, 1) * rotateQuat * AHRS2.quat * Quaternion.identity;//Quaternion.Slerp(transform.rotation, new Quaternion(1, 0, 0, 1) * AHRS2.quat, .8f);
            tf.text = "UnityAngle: " + Input.compass.magneticHeading;
            //AccelMagnOrientation.rotation = Quaternion.identity;
            //ActualPos.rotation = new Quaternion(0, 1, 0, 1) * Quaternion.identity;
          //  ActualPos.rotation = GyroEstimate;
          //  ActualPos.rotation = Quaternion.Euler(transform.rotation.eulerAngles + AccelMagnOrientation.rotation.eulerAngles);
          //  transform.rotation = Quaternion.Slerp(transform.rotation, ActualPos.rotation, .2f);
            //    Debug.Log("A");
            //Debug.Log(transform.rotation);
            //Debug.Log(Input.gyro.rotationRateUnbiased * Mathf.Rad2Deg * Time.deltaTime);
            //transform.Rotate(Input.gyro.rotationRateUnbiased * Mathf.Rad2Deg * Time.deltaTime);
            
            
            //Debug.Log(transform.rotation);
            
            
            //   Debug.Log(Input.compensateSensors);
            //  Debug.Log(Input.deviceOrientation);
            // Debug.Log("Accel: " + Input.acceleration);
            float Roll, Pitch, Yaw;
            //if (Input.deviceOrientation == DeviceOrientati on.Portrait || Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
            //{
            //    Roll = CalculcateRollPortrait();
            //    Pitch = CalculateRollLandscape(this.Accelerometer);
            //}
            //else
            //{
            Roll = CalculateRollLandscape(this.Accelerometer);
            Pitch = CalculatePitchLandscape(this.Accelerometer, Roll);
            //}
            //Debug.Log("Roll: " + Roll);
            //Debug.Log("Pitch: " + Pitch);
            accelData.Add(Input.acceleration);
            magnData.Add(Input.compass.rawVector);
            //this.tf.text = Input.compass.trueHeading.ToString();
            angleData.Add(Input.compass.trueHeading);
        }

        public void WriteAccel()
        {
            string file = Application.persistentDataPath + "/Accel.csv";
            tf.text = file;
            Debug.Log(file);
            WriteToFile(accelData, file);
        }

        public void WriteMagn()
        {
            string file = Application.persistentDataPath + "/Magn.csv";
            WriteToFile(magnData, file);
        }

        public void WriteAngle()
        {
            string file = Application.persistentDataPath + "/Angle.csv";
            WriteToFileFloat(angleData, file);
        }

        public void WriteToFile(List<Vector3> data, string FileName)
        {
            FileStream stream;
            
            stream = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            
            string[] lines = new string[data.Count];
            string test = string.Empty;
            for (int i = 0; i < data.Count; i++)
            {
                lines[i] = data[i].x + "," + data[i].y + "," + data[i].z + Environment.NewLine;
                test += data[i].x + "," + data[i].y + "," + data[i].z + Environment.NewLine;
            }
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(stream, test);
            stream.Close();
        }

        public void WriteToFileFloat(List<float> data, string FileName)
        {
            FileStream stream;

            stream = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            string test = string.Empty;
            for (int i = 0; i < data.Count; i++)
            {
                test += data[i].ToString() + Environment.NewLine;
            }
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            formatter.Serialize(stream, test);
            stream.Close();
        }

        public void PerformAccelCheck()
        {
            if (Utils.IsCheckableAccelerometerOrientation(Input.acceleration))
                CheckAccelerometerOrientation(Input.acceleration);
        }

        public void SaveAccelerometerOrientation()
        {
            Storage.SaveAccelerometerOrientation(this.accelerometerAxes, this.accelAxesInverted);
        }

        public void CheckAccelerometerOrientation(Vector3 value)
        {
            Debug.Log("Checking Accelerometer Orientation: " + Input.deviceOrientation);
            int expectedValue = 0; // Either -1 or 1
            Axes expectedAxis = Axes.X; // X, Y or Z
            switch (Input.deviceOrientation)
            {
                case DeviceOrientation.Unknown:
                    Debug.LogError("Device Orientation Unknown. Cannot check AccelerometerOrientation");
                    return;
                case DeviceOrientation.Portrait:
                    // Expected: -1 on North
                    // Device held upright. Accelerometer points to bottom of device
                    expectedAxis = NorthAxis;
                    expectedValue = NorthAxisInverted ? 1 : -1;
                    break;
                case DeviceOrientation.PortraitUpsideDown:
                    // Expected: 1 on North
                    // Device held upright & upside-down. Accelerometer points to top of device                    
                    expectedAxis = NorthAxis;
                    expectedValue = NorthAxisInverted ? -1 : 1;
                    break;
                case DeviceOrientation.LandscapeLeft:
                    // Expected: -1 on East
                    // Device held in Landscape with Left pointing down. Accelerometer points to left of device
                    expectedAxis = EastAxis;
                    expectedValue = EastAxisInverted ? 1 : -1;
                    break;
                case DeviceOrientation.LandscapeRight:
                    // Expected: 1 on East
                    // Device held in Landscape with Right pointing down. Accelerometer points to right of device
                    expectedAxis = EastAxis;
                    expectedValue = EastAxisInverted ? -1 : 1;
                    break;
                case DeviceOrientation.FaceUp:
                    // Expected: 1 on Down
                    // Device held Flat with screen facing up. Accelerometer points to back of device
                    expectedAxis = DownAxis;
                    expectedValue = DownAxisInverted ? -1 : 1;
                    break;
                case DeviceOrientation.FaceDown:
                    // Expected: -1 on Down
                    // Device held Flat with screen facing down. Accelerometer points to front of device
                    expectedAxis = DownAxis;
                    expectedValue = DownAxisInverted ? 1 : -1;
                    break;
            }
            Utils.SetAxis(value, expectedAxis, expectedValue, ref this.accelAxesInverted, ref this.accelerometerAxes);
        }
        
        
        

        private float CalculateRollLandscape(Vector3 acceleration)
        {
            float Roll = (90f + Mathf.Rad2Deg * Mathf.Atan2(acceleration.z, acceleration.x));
            if (Roll < 0)
                Roll += 360;
            return Roll;
        }

        private float CalculatePitchLandscape(Vector3 acceleration, float Roll)
        {
            float Pitch = Mathf.Rad2Deg * Mathf.Atan2(-acceleration.x, (acceleration.y * Mathf.Sin(Mathf.Deg2Rad * Roll) + acceleration.z * Mathf.Cos(Mathf.Deg2Rad * Roll)));
            if (Pitch < 0)
                Pitch += 360;
            return Pitch;
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
                yield return new WaitForSeconds(3);
            Input.compass.enabled = true;
            Input.location.Start(1, 1);
            if (UnityRemote)
                yield return new WaitForSeconds(3);
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
            yield break;
        }

        

        private void UpdateSensorMeasurement()
        {
            Vector3 PrevAccel = Accelerometer;
            Accelerometer = Utils.TransformVector(Input.acceleration, accelerometerAxes, accelAxesInverted);

            Accelerometer = LowPassFilterSensor(PrevAccel, Accelerometer, AccelSmooth);

            PrevAccel = Magnetometer;
            Magnetometer = new Vector3(Input.compass.rawVector.x, Input.compass.rawVector.z, Input.compass.rawVector.y);
            Magnetometer = LowPassFilterSensor(PrevAccel, Magnetometer, MagnSmooth);


            ////float angle = Input.compass.trueHeading * Mathf.Deg2Rad;
            ////Debug.Log("Ang: " + TestAngle);
            //Quaternion previousMeasurement = MeasuredQuat;
            //Magnetometer = new Vector3(Mathf.Sin(Mathf.Deg2Rad * TestAngle), 0, Mathf.Cos(Mathf.Deg2Rad * TestAngle));
            //// New quaternion from two measurements
            //MeasuredQuat = Quaternion.LookRotation(Magnetometer, transform.TransformDirection(-Accelerometer));
            //// TODO: Calc Angular Velocity between previous & Current Quat
            //Vector3 AngularVelocity = (MeasuredQuat.eulerAngles - previousMeasurement.eulerAngles) * Mathf.Deg2Rad; // ?????????????? Yeah? No? IDK
            //// AngularVelocity is in Radians/deltaTime
            ////Debug.Log("S"+AngularVelocity);

            this.RawMagnetometer = Input.compass.rawVector;
            //MagnetWarning(RawMagnetometer);
            //UpdateHardIronOffset(RawMagnetometer);
            //RawMagnetometer = RemoveHardIronOffset(RawMagnetometer);
            //this.Magnetometer = Utils.TransformVector(RemoveHardIronOffset(RawMagnetometer), magnetometerAxes, magnAxesInverted);
        }

        public void StartCalibration()
        {
            isCalibrating = true;
            magnCalib.Clear();
        }

        public void EndCalibration()
        {
            isCalibrating = false;
            magnOffset = Vector3.zero;
            for (int i = 0; i < magnCalib.Count; i++)
                magnOffset += magnCalib[i];
            magnOffset /= magnCalib.Count;
        }


        private void MagnetWarning(Vector3 Magnetometer)
        {
            //if (Magnetometer.magnitude < MagnetMin || Magnetometer.magnitude > MagnetMax)
            //    Debug.LogWarning("Invalid values for Magnetometer. Is there a magnet nearby?");
        }

        private void UpdateHardIronOffset(Vector3 Magnetometer)
        {
            if (Magnetometer.Equals(Vector3.zero)) // Skip invalid vector
                return;
            if (HardIronMinimums == Vector3.zero)
                HardIronMinimums = Utils.Vector3Abs(Magnetometer);
            if (HardIronMaximums == Vector3.zero)
                HardIronMaximums = Utils.Vector3Abs(Magnetometer);
            // X-Axis
            if (Mathf.Abs(Magnetometer.x) < HardIronMinimums.x)
                HardIronMinimums.x = Mathf.Abs(Magnetometer.x);
            else if (Mathf.Abs(Magnetometer.x) > HardIronMaximums.x)
                HardIronMaximums.x = Mathf.Abs(Magnetometer.x);
            // Y-Axis
            if (Mathf.Abs(Magnetometer.y) < HardIronMinimums.y)
                HardIronMinimums.y = Mathf.Abs(Magnetometer.y);
            else if (Mathf.Abs(Magnetometer.y) > HardIronMaximums.y)
                HardIronMaximums.y = Mathf.Abs(Magnetometer.y);
            // Z-Axis
            if (Mathf.Abs(Magnetometer.z) < HardIronMinimums.z)
                HardIronMinimums.z = Mathf.Abs(Magnetometer.z);
            else if (Mathf.Abs(Magnetometer.z) > HardIronMaximums.z)
                HardIronMaximums.z = Mathf.Abs(Magnetometer.z);
        }

        private Vector3 RemoveHardIronOffset(Vector3 Magnetometer)
        {
            Vector3 Offset = new Vector3()
            {
                x = HardIronMaximums.x - HardIronMinimums.x,
                y = HardIronMaximums.y - HardIronMinimums.y,
                z = HardIronMaximums.z - HardIronMinimums.z
            };
            if (Magnetometer.x < 0)
                Offset.x *= -1;
            if (Magnetometer.y < 0)
                Offset.y *= -1;
            if (Magnetometer.z < 0)
                Offset.z *= -1;
            return Magnetometer - Offset;
        }



        private void OnDrawGizmos()
        {
            //Gizmos.color = Color.green;
            //Vector3 accel = this.Accelerometer;
            //Gizmos.DrawLine(this.transform.position, this.transform.position + accel);
            //Gizmos.color = Color.blue;
            //Gizmos.DrawLine(this.transform.position, this.transform.position + Input.compass.rawVector * 20f);
            ////Debug.Log(Input.compass.rawVector);
            //Vector3 c = Vector3.Cross(AccelerometerNormalized, Magnetometer);
            //Vector3 d = Vector3.Cross(AccelerometerNormalized, c);
            //Gizmos.color = Color.red;
            //Gizmos.DrawLine(this.transform.position, this.transform.position + Magnetometer);
            //Gizmos.color = Color.gray;
            //Gizmos.DrawLine(this.transform.position, this.transform.position + c);
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawLine(this.transform.position, this.transform.position + d);


            //Gizmos.color = Color.green;
            //Gizmos.DrawLine(this.transform.position, this.transform.position + transform.up * 200f);
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawLine(this.transform.position, this.transform.position + transform.TransformDirection(Accelerometer * 10f));
            //Vector3 WorldVec = Vector3.down;
            //Vector3 Estimate = transform.InverseTransformDirection(WorldVec);
            //Gizmos.color = Color.red;
            //Gizmos.DrawLine(this.transform.position, this.transform.position + transform.TransformDirection(Estimate * 20f));
            //Debug.Log("Current Accelerometer Value: " + Accelerometer);
            //Debug.Log("Current Accel Estimate: " + Estimate);
            //Debug.Log("Accel World Vector: " + transform.TransformDirection(Accelerometer));
            //Debug.Log("Wanted World Vector: " + WorldVec);
            //Gizmos.color = Color.cyan;
            //Gizmos.DrawLine(transform.position, transform.position + Magnetometer * 150f);
            //Debug.Log("Magn" + Magnetometer);
            //Quaternion.LookRotation(Vector3.forward, -transform.TransformDirection(Accelerometer)); // QUATERNION WITH Down as ACCELVAL, FWD as MagnVal
            //Debug.Log("Maths: " + transform.rotation * Accelerometer); IS EQUAL TO ^

            //Gizmos.color = Color.blue;
            //Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(Accelerometer));
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Accelerometer);

            //Gizmos.color = Color.blue;
            //Gizmos.DrawLine(this.transform.position, this.transform.position - GetUpVector(transform.rotation) * 200f);
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawLine(this.transform.position, this.transform.position + transform.up * 20f);
        }

        private Vector3 LowPassFilterSensor(Vector3 previousReading, Vector3 currentReading, float smoothingFactor)
        {
            return smoothingFactor * currentReading + (1 - smoothingFactor) * previousReading; // Low-Pass-Filter
        }

    }
}