using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Server for communication with Arduino (through Internet)
/// </summary>
public class ArduinoServer : MonoBehaviour {
    #region Variables
    #region Static
    /// <summary>
    /// Singleton-instance
    /// </summary>
    public static ArduinoServer server;
    #endregion

    #region Public
    /// <summary>
    /// Whether the socket is currently connected
    /// </summary>
    public bool SocketReady { get; private set; }
    #endregion

    #region Private
    /// <summary>
    /// The Socket of this connection
    /// </summary>
    private TcpClient mySocket;
    /// <summary>
    /// The NetworkStream to Write & Read to/from
    /// </summary>
    private NetworkStream stream;
    /// <summary>
    /// StreamWriter to handle writing to Socket
    /// </summary>
    private StreamWriter writer;
    /// <summary>
    /// StreamReader to handle reading from Socket
    /// </summary>
    private StreamReader reader;
    #endregion

    #region Editor
    /// <summary>
    /// IP-adress of Arduino
    /// </summary>
    [SerializeField]
    private String Host;// = "INSERT the public IP of router or Local IP of Arduino";
    /// <summary>
    /// Port of arduino
    /// </summary>
    [SerializeField]
    private Int32 Port;// = 5001;
    /// <summary>
    /// TimeOut for Ping command
    /// (To check if Arduino is available)
    /// </summary>
    [SerializeField]
    private float TimeOut;// = 3.0f
    #endregion
    #endregion

    #region Methods
    #region UnityMethods
    /// <summary>
    /// Sets Singleton-instance
    /// </summary>
    void Awake()
    {
        if (server != null)
            Destroy(this.gameObject);
        server = this;
    }

    /// <summary>
    /// Initializes connection
    /// </summary>
    void Start () {
        StartCoroutine(Connect());
    }
	
	/// <summary>
    /// Checks for received data
    /// </summary>
	void Update () {
        if (!SocketReady || !mySocket.Connected || stream == null)
            return;
        if (stream.DataAvailable)
        {
            string receivedData = Read();
            if (receivedData == "Light on")
            {
                Debug.Log("I RECEIVED: Light on");
            }
            if (receivedData == "Light off")
            {
                Debug.Log("I RECEIVED: Light off");
            }
        }
    }

    /// <summary>
    /// Disconnect when the Server is Destroyed
    /// </summary>
    void OnDestroy()
    {
        Disconnect();
    }
    #endregion

    #region Public
    /// <summary>
    /// Connects to the Arduino
    /// </summary>
    public IEnumerator Connect()
    {
        Ping ping = new Ping(Host);
        yield return new WaitForSecondsRealtime(TimeOut);
        if (!ping.isDone)
        {
            ping.DestroyPing();
            Debug.LogError("Unable to connect to Arduino");
            yield break;
        }
        ping.DestroyPing();
        try
        {
            mySocket = new TcpClient(Host, Port);
            stream = mySocket.GetStream();
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);
            SocketReady = true;
        }
        catch (Exception e)
        {
            Debug.Log("Socket error:" + e);
        }
        yield break;
    }
    
    /// <summary>
    /// Writes a message to the Arduino
    /// </summary>
    /// <param name="theLine">Message to write</param>
    public void Write(string theLine)
    {
        if (!SocketReady)
            return;
        String tmpString = theLine;
        writer.Write(tmpString);
        writer.Flush();
    }

    /// <summary>
    /// Reads a message from the Arduino
    /// Returns an empty string if the socket is not ready, or NoData if there is no data
    /// </summary>
    /// <returns>Read message from the Arduino</returns>
    public String Read()
    {
        if (!SocketReady)
            return "";
        if (stream.DataAvailable)
            return reader.ReadLine();
        return "NoData";
    }

    /// <summary>
    /// Closes the connection to the Arduino
    /// </summary>
    public void Disconnect()
    {
        if (!SocketReady)
            return;
        writer.Close();
        reader.Close();
        mySocket.Close();
        SocketReady = false;
    }
    #endregion
    #endregion
}