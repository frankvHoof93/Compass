using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// WORK IN PROGRESS
/// </summary>
public class MyTiltCompensatedCompass : MonoBehaviour, ICompass {
    #region Debug
    public Text roll, pitch, yaw;
    public Text accx, accy, accz;
    public Text magx, magy, magz;
    
    float xMax = 68.59999f;//50.54321f;
    float xMin = -90.09999f;//-68.80035f;
    float yMax = 154.8f;//44.02466f;
    float yMin = -74f;//-52.41699f;
    float zMax = 55.59999f;//46.09833f;
    float zMin = -90.89999f;//-60.99091f;
    #endregion

    #region Variables
    #region Private
    private float Phi, Theta, Psi;

    private bool started = false;

    float minX, minY, minZ;
    float maxX, maxY, maxZ;
    #endregion
    #endregion


    #region Methods
    /// <summary>
    /// Gets Rotation from North (on the plane of the earths surface) (Yaw)
    /// </summary>
    /// <returns></returns>
    public float GetRotation()
    {
        return 0f;
    }

	/// <summary>
    /// Initializes Compass
    /// </summary>
	void Start () {
        minX = 1000;
        minY = 1000;
        minZ = 1000;
        maxX = 0;
        maxY = 0;
        maxZ = 0;
        Input.compensateSensors = false;
        Input.compass.enabled = true;
        Input.gyro.enabled = true;
        if (Input.location.status == LocationServiceStatus.Stopped)
            StartCoroutine(startLoc());
    }

    /// <summary>
    /// Starts Location Services
    /// Run in a CoRoutine
    /// </summary>
    /// <returns>CoRoutine</returns>
    private IEnumerator startLoc()
    {
        Input.location.Start(1, 1);
        yield return new WaitForSeconds(2);
        started = true;
    }
	
	/// <summary>
    /// Update Compass
    /// </summary>
	void Update () {
        if (!started) return;
        accx.text = Input.acceleration.x.ToString();
        accy.text = Input.acceleration.y.ToString();
        accz.text = Input.acceleration.z.ToString();
        magx.text = Input.compass.rawVector.x.ToString();
        magy.text = Input.compass.rawVector.y.ToString();
        magz.text = Input.compass.rawVector.z.ToString();
   /*     Vector3 Magn = Input.compass.rawVector;
        if (Magn.x > maxX)
            maxX = Magn.x;
        if (Magn.y > maxY)
            maxY = Magn.y;
        if (Magn.z > maxZ)
            maxZ = Magn.z;
        if (Magn.x < minX)
            minX = Magn.x;
        if (Magn.y < minY)
            minY = Magn.y;
        if (Magn.z < minZ)
            minZ = Magn.z;
        accx.text = maxX.ToString();
        magx.text = minX.ToString();
        accy.text = maxY.ToString();
        magy.text = minY.ToString();
        accz.text = maxZ.ToString();
        magz.text = minZ.ToString();*/
        Vector3 offset = new Vector3((xMax + xMin) / 2f, (yMax + yMin) / 2f, (zMax + zMin) / 2f);

        //calcAngles(Input.acceleration, Input.compass.rawVector);

        float accelerationX = ((int)(Input.acceleration.x*100))/100f;
        float accelerationY = ((int)(Input.acceleration.y * 100)) / 100f;
        float accelerationZ = ((int)(Input.acceleration.z * 100)) / 100f;
        //float myRoll = Mathf.Atan(accelerationY / Mathf.Sqrt(accelerationX * accelerationX + accelerationZ * accelerationZ));
        float myRoll = Rad2Deg(Mathf.Atan2(accelerationX, accelerationZ + accelerationY * 0.001f));
        myRoll += 180f;
        float lambda = Mathf.Sqrt(accelerationX * accelerationX + accelerationZ * accelerationZ);
        float myPitch = Rad2Deg(Mathf.Atan(-accelerationY / lambda));

        float bx = Input.compass.rawVector.y;
        float by = Input.compass.rawVector.x;
        float bz = Input.compass.rawVector.z;
        bx -= offset.y;
        by -= offset.x;
        bz -= offset.z;
        float x = by * Mathf.Cos(myRoll) - bz * Mathf.Sin(myRoll);
        float epsilon = by * Mathf.Sin(myRoll) + bz * Mathf.Cos(myRoll);
        float delta = bx * Mathf.Cos(myPitch) + epsilon * Mathf.Sin(myPitch);
        float yawVal = Rad2Deg(Mathf.Atan2(-x, delta));
        yaw.text = yawVal.ToString();
        roll.text = myRoll.ToString();
        pitch.text = myPitch.ToString();

        //   float myPitch = 180 * Mathf.Atan(accelerationX / Mathf.Sqrt(accelerationY * accelerationY + accelerationZ * accelerationZ)) / Mathf.PI;
        //   
        // magx.text = "Roll: " + (Mathf.Atan2(Input.acceleration.y, Input.acceleration.z) * (180f/Mathf.PI)).ToString();
        //  magy.text = "Pitch: " + myPitch.ToString();
        //   roll.text = "Roll: " + myRoll.ToString();
        //   pitch.text = "Pitch: " + myPitch.ToString();
        //   yaw.text = Input.acceleration.y.ToString();
        //pitch.text = Input.compass.rawVector.y.ToString();
        //yaw.text = Input.compass.rawVector.z.ToString();
        //this.transform.eulerAngles = new Vector3(Phi, Theta, Psi);
        Vector3 translatedMagn = new Vector3(bx, by, bz);
        translatedMagn.Normalize();
        Vector3 cross = Vector3.Cross(Input.acceleration.normalized, translatedMagn);
        accx.text = cross.x.ToString();
        accy.text = cross.y.ToString();
        accz.text = cross.z.ToString();

        this.transform.eulerAngles = new Vector3(myRoll, yawVal-Input.compass.trueHeading, myPitch);
        //this.transform.eulerAngles = new Vector3(cross.x * 360f, cross.y * 360f, cross.z * 360f);
    }

    /// <summary>
    /// Radians to Degrees
    /// </summary>
    /// <param name="rad">Radians</param>
    /// <returns>Degrees</returns>
    private float Rad2Deg(float rad)
    {
        return rad * (180f / Mathf.PI);        
    }

    /// <summary>
    /// Conversion from Euler angles to Quaternions (optional step)
    /// Qw = Cos(Phi/2)*Cos(Theta/2)*Cos(Psi/2) + Sin(Phi/2)*Sin(Theta/2)*Sin(Psi/2)
    /// Qx = Sin(Phi/2)*Cos(Theta/2)*Cos(Psi/2) – Cos(Phi/2)*Sin(Theta/2)*Sin(Psi/2)
    /// Qy = Cos(Phi/2)*Sin(Theta/2)*Cos(Psi/2) + Sin(Phi/2)*Cos(Theta/2)*Sin(Psi/2)
    /// Qz = Cos(Phi/2)*Cos(Theta/2)*Sin(Psi/2) – Sin(Phi/2)*Sin(Theta/2)*Cos(Psi/2)
    /// </summary>
    /// <param name="euler">Euler: X - Phi, Y - Theta, Z - Psi</param>
    /// <returns></returns>
    public Quaternion EulerToQuaternion(Vector3 euler)
    {
        float Phi = euler.x / 2f;
        float Theta = euler.y / 2f;
        float Psi = euler.z / 2f;
        float Qw = Mathf.Cos(Phi) * Mathf.Cos(Theta) * Mathf.Cos(Psi) + Mathf.Sin(Phi) * Mathf.Sin(Theta) * Mathf.Sin(Psi);
        float Qx = Mathf.Sin(Phi) * Mathf.Cos(Theta) * Mathf.Cos(Psi) - Mathf.Cos(Phi) * Mathf.Sin(Theta) * Mathf.Sin(Psi);
        float Qy = Mathf.Cos(Phi) * Mathf.Sin(Theta) * Mathf.Cos(Psi) + Mathf.Sin(Phi) * Mathf.Cos(Theta) * Mathf.Sin(Psi);
        float Qz = Mathf.Cos(Phi) * Mathf.Cos(Theta) * Mathf.Sin(Psi) - Mathf.Sin(Phi) * Mathf.Sin(Theta) * Mathf.Cos(Psi);
        return new Quaternion(Qx, Qy, Qz, Qw);
    }
    #endregion
}