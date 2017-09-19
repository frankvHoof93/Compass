using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class encapsulates Unity's locational assets
/// These include both the Compass & the GPS-Coordinates
/// </summary>
public class GPS : MonoBehaviour
{
    #region Variables
    #region Static
    /// <summary>
    /// Singleton-Instance
    /// </summary>
    public static GPS instance { get; private set; }
    #endregion

    #region Public
    /// <summary>
    /// Whether the GPS is Running
    /// </summary>
    public bool Running { get; private set; }
    /// <summary>
    /// Current GeoCoordinate
    /// </summary>
    public GeoCoordinate Coordinate { get; private set; }
    /// <summary>
    /// Previous GeoCoordinate
    /// </summary>
    public GeoCoordinate LastCoordinate { get; private set; }
    /// <summary>
    /// GeoCoordinate of Origin ((0, 0, 0) in Unity)
    /// </summary>
    public GeoCoordinate Origin { get; private set; }
    /// <summary>
    /// Whether the GPS is using a fake location
    /// </summary>
    public bool IsUsingFakeLocation { get { return useFakeLocation; } }
    #endregion

    #region Private
    /// <summary>
    /// Compass being used by this GPS
    /// </summary>
    private ICompass myCompass;
    /// <summary>
    /// Current status of Android LocationService
    /// </summary>
    private LocationServiceStatus currentStatus { get { return Input.location.status; } }
    #endregion

    #region Editor
    /// <summary>
    /// UI-Text that shows Latitude
    /// </summary>
    [SerializeField]
    private Text textLat;
    /// <summary>
    /// UI-Text that shows Longitude
    /// </summary>
    [SerializeField]
    private Text textLong;
    /// <summary>
    /// Accuracy of GPS (in meters)
    /// </summary>
    [SerializeField]
    private float accuracy;
    /// <summary>
    /// Minimal update distance (in meters)
    /// </summary>
    [SerializeField]
    private float minUpdateDistance;
    /// <summary>
    /// Timeout for startup (in seconds)
    /// </summary>
    [SerializeField]
    private int timeout;
    /// <summary>
    /// Whether to Log to console
    /// </summary>
    [SerializeField]
    private bool debugLocation;
    /// <summary>
    /// Whether UnityRemote is being used
    /// </summary>
    [SerializeField]
    private bool unityRemote;
    /// <summary>
    /// Whether to use a fake location
    /// </summary>
    [SerializeField]
    private bool useFakeLocation;
    /// <summary>
    /// Fake Location to use
    /// </summary>
    [SerializeField]
    private Vector2 fakeLocation;
    /// <summary>
    /// Whether to use tilt-compensated compass (WIP)
    /// </summary>
    [SerializeField]
    private bool useCompensatedCompass;
    #endregion
    #endregion

    #region Methods
    #region UnityMethods
    /// <summary>
    /// Setup of default values & singleton-object
    /// </summary>
    void Awake()
    {
        if (instance != null)
            Destroy(this.gameObject);
        instance = this;
        DontDestroyOnLoad(gameObject);
        if (useCompensatedCompass)
            myCompass = gameObject.AddComponent<MyTiltCompensatedCompass>();
        else myCompass = gameObject.AddComponent<UnCompensatedCompass>();
        Coordinate = new GeoCoordinate(0, 0);
        LastCoordinate = new GeoCoordinate(0, 0);
    }

    /// <summary>
    /// Init of GPS
    /// </summary>
    void Start()
    {
        this.Running = false;
        #if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isRemoteConnected)
        {
            unityRemote = true;
        }
        #endif
        if (!useFakeLocation)
            StartCoroutine(StartLocationService());
        else
        {
            GeoCoordinate fakeCoord = new GeoCoordinate(fakeLocation.x, fakeLocation.y);
            Origin = fakeCoord;
            LastCoordinate = Origin;
            Coordinate = Origin;
            Running = true;
        }
    }

    /// <summary>
    /// GPS Update
    /// </summary>
    void Update()
    {
        if (Running)
        {
            if (!useFakeLocation)
                Coordinate = new GeoCoordinate(Input.location.lastData.latitude, Input.location.lastData.longitude);
            if (textLat != null)
                textLat.text = Coordinate.latitude.ToString();
            if (textLong != null)
                textLong.text = Coordinate.longitude.ToString();
            if (debugLocation)
            {
                Debug.Log("GPS | lat: " + Input.location.lastData.latitude.ToString());
                Debug.Log("GPS | long: " + Input.location.lastData.longitude.ToString());
                Debug.Log("Compass |" + Input.compass.trueHeading);
                Debug.Log("Distance: " + GeoCoordinate.DistanceBetweenGeoCoordinates(Coordinate, LastCoordinate));
            }
        }
    }
    #endregion

    #region Public
    /// <summary>
    /// Gets rotation (in degrees) from Geographical North
    /// </summary>
    /// <returns>Rotation in Degrees</returns>
    public float GetRotationFromNorth()
    {
        return myCompass.GetRotation();
    }

    /// <summary>
    /// Updates Last Position (to current Position)
    /// </summary>
    public void UpdateLastPosition()
    {
        LastCoordinate = new GeoCoordinate(Coordinate);
    }
    #endregion

    #region Private
    /// <summary>
    /// IEnumerator to Start Location Services
    /// Use with Coroutine
    /// </summary>
    /// <returns>Used in Coroutine</returns>
    private IEnumerator StartLocationService()
    {
        Debug.Log("Starting Compass");
        Input.compass.enabled = true;
        Debug.Log("Starting GPS");
        // Wait until the editor and unity remote are connected before starting a location service
        if (unityRemote)
            yield return new WaitForSeconds(2);
        Input.location.Start(accuracy, minUpdateDistance);
        if (unityRemote)
            yield return new WaitForSeconds(2);
        while (currentStatus == LocationServiceStatus.Initializing && timeout > 0)
        {
            timeout--;
            Debug.Log(currentStatus);
            yield return new WaitForSeconds(1);
        }
        if (timeout <= 0)
        {
            Debug.LogError("GPS: Init timed out");
            yield break;
        }
        if (currentStatus == LocationServiceStatus.Failed)
        {
            Debug.LogError("GPS: Unable to init GPS");
            yield break;
        }
        Origin = new GeoCoordinate(Input.location.lastData.latitude, Input.location.lastData.longitude);
        Coordinate = new GeoCoordinate(Input.location.lastData.latitude, Input.location.lastData.longitude);
        Running = true;
        LastCoordinate = Coordinate;

        Debug.Log("GPS Running");
        Gyro gyro = GameObject.FindObjectOfType<Gyro>();
        gyro.EnableGyro();
        gyro.cameraContainer.transform.RotateAround(Vector3.up, 90f);
        yield break;
    }
    #endregion
    #endregion
}