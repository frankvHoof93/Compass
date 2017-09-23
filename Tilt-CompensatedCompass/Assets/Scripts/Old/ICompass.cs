/// <summary>
/// The Compass is used to get the Rotation of the device
/// in relation to North, projected on the plane of the Earth (yaw)
/// </summary>
public interface ICompass
{
    /// <summary>
    /// Gets rotation of Compass from Geographical North (Yaw)
    /// </summary>
    /// <returns>Rotation from Geographical North (in Degrees)</returns>
    float GetRotation();
}