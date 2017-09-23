using UnityEngine;

/// <summary>
/// Implementation of Compass that does NOT compensate for
/// the rotation of the device (pitch & roll)
/// </summary>
public class UnCompensatedCompass : MonoBehaviour, ICompass {
    #region Methods
    #region Public
    /// <summary>
    /// Gets rotation of Compass from Geographical North (Yaw)
    /// </summary>
    /// <returns>Rotation from North (in Degrees)</returns>
    public float GetRotation()
    {
        return Input.compass.trueHeading;
    }
    #endregion
    #endregion
}
