using UnityEngine;

/// <summary>
/// Gyroscope that controls the Camera's rotation
/// </summary>
[RequireComponent(typeof(Camera))]
public class Gyro : MonoBehaviour {
    #region Variables
    #region Public
    /// <summary>
    /// Container Object for Camera
    /// </summary>
    public GameObject cameraContainer { get; private set; }
    #endregion

    #region Private
    /// <summary>
    /// Whether the Gyro is active
    /// </summary>
    private bool isRunning;
    /// <summary>
    /// Android Gyroscope Object
    /// </summary>
    private Gyroscope gyro;
    /// <summary>
    /// Rotation of Gyro at Start
    /// </summary>
    private Vector3 initialOrientation;
    #endregion

    #region Editor
    /// <summary>
    /// Whether to use filtered (unbiased) values
    /// </summary>
    [SerializeField]
    private bool useFilter;
    #endregion
    #endregion

    #region Methods
    #region UnityMethods
    /// <summary>
    /// Setup of Container-object
    /// </summary>
    void Start()
    {
        cameraContainer = new GameObject("Camera Container");
        cameraContainer.transform.SetParent(this.transform.parent);
        cameraContainer.transform.position = transform.position;
        transform.SetParent(cameraContainer.transform);
        if (GPS.instance.IsUsingFakeLocation)
            EnableGyro();
    }

    /// <summary>
    /// Updates Container-object with current rotation
    /// </summary>
    void Update()
    {
        if (isRunning)
        {
            if (useFilter)
                cameraContainer.transform.Rotate(initialOrientation.x - Input.gyro.rotationRateUnbiased.x, initialOrientation.y - Input.gyro.rotationRateUnbiased.y, initialOrientation.z + Input.gyro.rotationRateUnbiased.z);
            else
                cameraContainer.transform.Rotate(initialOrientation.x - Input.gyro.rotationRate.x, initialOrientation.y - Input.gyro.rotationRate.y, initialOrientation.z + Input.gyro.rotationRate.z);
        }
    }
    #endregion

    #region Public
    /// <summary>
    /// Enables Gyroscope
    /// </summary>
    public void EnableGyro()
    {
        if (SystemInfo.supportsGyroscope)
        {
            gyro = Input.gyro;
            gyro.enabled = true;
            ResetGyro();
            isRunning = true;
        }
        else isRunning = false;
    }

    /// <summary>
    /// Resets Gyroscope (pointing down)
    /// </summary>
    public void ResetGyro()
    {
        float compass = 0f;
        if (!GPS.instance.IsUsingFakeLocation)
            compass = GPS.instance.GetRotationFromNorth();
        cameraContainer.transform.rotation = Quaternion.Euler(90f, 0f, 0f - compass);
        initialOrientation = new Vector3(Input.gyro.rotationRateUnbiased.x, Input.gyro.rotationRateUnbiased.y, Input.gyro.rotationRateUnbiased.z);
    }
    #endregion
    #endregion    
}
