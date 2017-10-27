using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IMU {

    private const float Beta = .1f;
    private const float SamplePeriod = 1f / 60f;

    private static Quaternion q = new Quaternion(0, 0, 0, 1);

    public static void Update(Quaternion q, Vector3 gyroAccel, Vector3 Accelerometer, Vector3 Magnetometer)
    {
        Accelerometer.Normalize();
        Magnetometer.Normalize();
        
        Quaternion h = q * new Quaternion(0, Magnetometer[0], Magnetometer[1], Magnetometer[2]) * Conjugate(q);
        Vector3 x = new Vector3(h[1], h[2], h[3]);
        Quaternion b = new Quaternion(0, x.magnitude, 0, h[3]);


    }


    private static Quaternion Conjugate(Quaternion q)
    {
        return new Quaternion(-q.x, -q.y, -q.z, q.w);
    }
}
