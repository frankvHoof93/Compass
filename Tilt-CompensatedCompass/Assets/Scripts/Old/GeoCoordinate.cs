using UnityEngine;

/// <summary>
/// A position on Earth
/// Determined by Latitude & Longitude
/// Latitude: South to North
/// Longitude: West to East
/// </summary>
public class GeoCoordinate {
    #region Constructors
    /// <summary>
    /// Constructor for Coordinate using decimal notation
    /// </summary>
    /// <param name="latitudeDec">Decimal Latitude</param>
    /// <param name="longitudeDec">Decimal Longitude</param>
    public GeoCoordinate(double latitudeDec, double longitudeDec)
    {
        this.latitude = latitudeDec;
        this.longitude = longitudeDec;
    }

    /// <summary>
    /// Constructor for Coordinate using DMS-notation
    /// DMS: Degrees, Minutes, Seconds
    /// </summary>
    /// <param name="latitudeDegrees">Degrees for Latitude</param>
    /// <param name="latitudeMinutes">Minutes for Latitude</param>
    /// <param name="latitudeSeconds">Seconds for Latitude</param>
    /// <param name="longitudeDegrees">Degrees for Longitude</param>
    /// <param name="longitudeMinutes">Minutes for Longitude</param>
    /// <param name="longitudeSeconds">Seconds for Longitude</param>
    public GeoCoordinate(int latitudeDegrees, int latitudeMinutes, double latitudeSeconds, int longitudeDegrees, int longitudeMinutes, double longitudeSeconds)
    {
        this.latitude = latitudeDegrees + ((float)latitudeMinutes / 60f) + ((float)latitudeSeconds / 3600f);
        this.longitude = longitudeDegrees + ((float)longitudeMinutes / 60f) + ((float)longitudeSeconds / 3600f);
    }

    /// <summary>
    /// Constructor used for duplication of a coordinate
    /// </summary>
    /// <param name="original">GeoCoordinate to duplicate</param>
    public GeoCoordinate(GeoCoordinate original)
    {
        this.latitude = original.latitude;
        this.longitude = original.longitude;
    }
    #endregion

    #region Variables
    #region Public
    /// <summary>
    /// Latitude in decimal notation
    /// </summary>
    public double latitude { get; private set; }
    /// <summary>
    /// Longitude in decimal notation
    /// </summary>
    public double longitude { get; private set; }
    /// <summary>
    /// Degrees-value of Latitude
    /// </summary>
    public int LatDegrees { get { return (int)latitude; } }
    /// <summary>
    /// Minutes-value of Latitude
    /// </summary>
    public int LatMinutes { get { return (int)((latitude - LatDegrees) * 60f); } }
    /// <summary>
    /// Seconds-value of Latitude
    /// </summary>
    public double LatSeconds { get { return (latitude - LatDegrees - (LatMinutes / 60)) * 3600; } }
    /// <summary>
    /// Degrees-value of Longitude
    /// </summary>
    public int LonDegrees { get { return (int)longitude; } }
    /// <summary>
    /// Minutes-value of Longitude
    /// </summary>
    public int LonMinutes { get { return (int)((longitude - LonDegrees) * 60f); } }
    /// <summary>
    /// Seconds-value of Longitude
    /// </summary>
    public double LonSeconds { get { return (longitude - LonDegrees - (LonMinutes / 60)) * 3600; } }
    #endregion

    #region Private
    /// <summary>
    /// Radius of the Earth
    /// Used for calculating the distance between Coordinates
    /// </summary>
    private const float EARTH_RADIUS = 6371000;
    #endregion
    #endregion

    #region Methods
    #region Static
    #region Public
    /// <summary>
    /// Distance between GeoCoordinates in Meters
    /// </summary>
    /// <param name="coord1">First Coordinate</param>
    /// <param name="coord2">Second Coordinate</param>
    /// <returns>Distance in Meters between the Coordinates</returns>
    public static double DistanceBetweenGeoCoordinates(GeoCoordinate coord1, GeoCoordinate coord2)
    {
        double distLat = Deg2Rad(coord1.latitude - coord2.latitude);
        double distLon = Deg2Rad(coord1.longitude - coord2.longitude);
        double a = Mathf.Pow(Mathf.Sin((float)(distLat / 2)), 2) +
            Mathf.Cos((float)Deg2Rad(coord1.latitude)) * Mathf.Cos((float)Deg2Rad(coord2.longitude)) *
            Mathf.Pow(Mathf.Sin((float)(distLon / 2)), 2);
        a = 2 * Mathf.Atan2(Mathf.Sqrt((float)a), Mathf.Sqrt((float)(1 - a)));
        return a * EARTH_RADIUS;
    }

    /// <summary>
    /// Vector2 of distance between coordinates
    /// x = Distance in Latitude
    /// y = Distance = Longitude
    /// </summary>
    /// <param name="start">Coordinate of "origin"</param>
    /// <param name="destination">Coordinate of "destination"</param>
    /// <returns>Vector2 Distance between Coordinates. X = Latitudinal distance, Y = Longitudinal distance</returns>
    public static Vector2 VectorBetweenGeoCoordinates(GeoCoordinate start, GeoCoordinate destination)
    {
        double b2 = 2 * Mathf.Pow(EARTH_RADIUS, 2);
        // Latitude
        double alpha = destination.latitude - start.latitude;
        alpha *= Mathf.PI / 180f; //Radian
        double cos = 1 - alpha * alpha / 2; //Math.cos(a)
        float x = Mathf.Sqrt((float)(b2 - b2 * cos)); //Al Kashi Theorem
        if (start.latitude > destination.latitude)
            x *= -1f;
        // Longitude
        alpha = destination.longitude - start.longitude;
        alpha *= Mathf.PI / 180f; //Radian
        cos = 1 - alpha * alpha / 2; //Math.cos(a)
        float y = Mathf.Sqrt((float)(b2 - b2 * cos)); //Al Kashi Theorem
        if (start.longitude > destination.longitude)
            y *= -1f;
        return new Vector2(x, y);
    }
    #endregion

    #region Private
    /// <summary>
    /// Degrees to Radians
    /// </summary>
    /// <param name="deg">Value to turn into Radians (in Degrees)</param>
    /// <returns>Value in Radians</returns>
    private static double Deg2Rad(double deg)
    {
        return deg * (Mathf.PI / 180f);
    }

    /// <summary>
    /// Radians to Degrees
    /// </summary>
    /// <param name="rad">Value to turn into Degrees (in Radians)</param>
    /// <returns>Value in Degrees</returns>
    private static double Rad2Deg(double rad)
    {
        return rad * (180f / Mathf.PI);
    }
    #endregion
    #endregion
    #endregion
}