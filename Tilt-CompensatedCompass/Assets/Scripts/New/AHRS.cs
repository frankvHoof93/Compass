using UnityEngine;

public class AHRS
{
    private const float Beta = 0.8f;



    public static Quaternion PerformUpdate(Quaternion startPos, Vector3 GyroAccel, Vector3 AccelMeas, Vector3 MagnMeas)
    {
        // Vector4 (x, y, z, w)   (Access the x, y, z, w components using [0], [1], [2], [3] respectively.)
        Vector4 q = new Vector4(startPos.x, startPos.y, startPos.z, startPos.w); 
        if (AccelMeas.magnitude.Equals(0))
            return Quaternion.identity; // NaN
        if (MagnMeas.magnitude.Equals(0))
            return Quaternion.identity; // NaN
        AccelMeas.Normalize();
        MagnMeas.Normalize();
        Vector4 squares, doubles;
        CalcAuxVars(q, out squares, out doubles);
        Vector2 earthRef = CalcEarthReference(q, doubles, squares, MagnMeas);
        Vector4 gradient = GradientDescentStep(q, doubles, squares, AccelMeas, MagnMeas, earthRef);
        Vector4 change = CalcRateOfChange(q, gradient, GyroAccel);
        return CalcIntegration(q, change);
    }

    private static void CalcAuxVars(Vector4 q, out Vector4 squares, out Vector4 doubles)
    {
        // Squares
        squares = new Vector4()
        {
            x = q.x * q.x, // q.w^2
            y = q.y * q.y, // q.x^2
            z = q.z * q.z, // q.y^2
            w = q.w * q.w  // q.z^2
        };
        // Doubles
        doubles = new Vector4()
        {
            x = 2f * q.x, // 2*q.x
            y = 2f * q.y, // 2*q.y
            z = 2f * q.z, // 2*q.z
            w = 2f * q.w  // 2*q.w
        };
    }

    private static Vector2 CalcEarthReference(Vector4 q, Vector4 doubles, Vector4 squares, Vector3 Magn)
    {
        float _2q1mx = doubles.w * Magn.x;
        float _2q1my = doubles.w * Magn.y;
        float _2q1mz = doubles.w * Magn.z;
        float _2q2mx = doubles.x * Magn.x;
        float hx = Magn.x * squares.w - _2q1my * q.z + _2q1mz * q.y + Magn.x * squares.x + doubles.x * Magn.y * q.y + doubles.x * Magn.z * q.z - Magn.x * squares.y - Magn.x * squares.z;
        float hy = _2q1mx * q.z + Magn.y * squares.w - _2q1mz * q.z + _2q2mx * q.y - Magn.y * squares.x + Magn.y * squares.y + doubles.y * Magn.z * q.z - Magn.y * squares.z;
        return new Vector2()
        {
            x = Mathf.Sqrt(hx * hx + hy * hy),
            y = -_2q1mx * q.y + _2q1my * q.x + Magn.z * squares.w + _2q2mx * q.z - Magn.z * squares.x + doubles.y * Magn.y * q.z - Magn.z * squares.y + Magn.z * squares.z
        };
    }

    private static Vector4 GradientDescentStep(Vector4 q, Vector4 doubles, Vector4 squares, Vector3 Accel, Vector3 Magn, Vector2 EarthRef)
    {
        // Variables used to avoid repeated arithmetic
        float q1q2 = q.w * q.x;
        float q1q3 = q.w * q.y;
        float q2q4 = q.x * q.z;
        float q3q4 = q.y * q.z;

        float _Exq1 = EarthRef.x * q.w;
        float _Exq2 = EarthRef.x * q.x;
        float _Exq3 = EarthRef.x * q.y;
        float _Exq4 = EarthRef.x * q.z;
        float _Eyq1 = EarthRef.y * q.w;
        float _Eyq2 = EarthRef.y * q.x;
        float _Eyq3 = EarthRef.y * q.y;
        float _Eyq4 = EarthRef.y * q.z;

        float _2Exq3 = 2f * _Exq3;
        float _2Exq4 = 2f * _Exq4;
        float _2Eyq2 = 2f * _Eyq2;
        float _2Eyq3 = 2f * _Eyq3;

        // Naming Convention:
        // h = .5f, o = 1f, t = 2f
        // P = Plus, M = Minus, T = Times   
        // Mx = Magn.x, Az = Accel.z, Ex = EarthRef.x
        float _ExTq1q3Pq2q4P_EyThMq2q2Mq3q3Mmz = EarthRef.x * (q1q3 + q2q4) + EarthRef.y * (0.5f - squares[1] - squares[2]) - Magn.z;
        float _ExTq2q3Mq1q4P_EyTq1q2Pq3q4Mmy = EarthRef[0] * ((q[1] * q[2]) - (q[0] * q[3])) + EarthRef[1] * (q1q2 + q3q4) - Magn.y;
        float _ExThMq3q3Mq4q4P_EyTq2q4Mq1q3Mmx = EarthRef[0] * (.5f - squares[2] - squares[3]) + EarthRef[1] * (q2q4 - q1q3) - Magn.x;
        float _oMtTq2q2MtTq3q3Maz = 1 - 2f * squares[1] - 2f * squares[2] - Accel.z;
        float _2Tq2q4M_2Tq1q3MAx = 2f * q2q4 - (2f * q1q3) - Accel.x;
        float _2Tq1q2P_2Tq3q4MAy = 2f * q1q2 + (2f * q3q4) - Accel.y;

        // Perform actual Gradient Descent Corrective Step
        return new Vector4()
        {
            w = -doubles[2] * _2Tq2q4M_2Tq1q3MAx + doubles[1] * _2Tq1q2P_2Tq3q4MAy - _Eyq3 * _ExThMq3q3Mq4q4P_EyTq2q4Mq1q3Mmx + (-_Exq4 + _Eyq2) * _ExTq2q3Mq1q4P_EyTq1q2Pq3q4Mmy + _Exq3 * _ExTq1q3Pq2q4P_EyThMq2q2Mq3q3Mmz,
            x = doubles[3] * _2Tq2q4M_2Tq1q3MAx + doubles[0] * _2Tq1q2P_2Tq3q4MAy - 4f * q[1] * _oMtTq2q2MtTq3q3Maz + _Eyq4 * _ExThMq3q3Mq4q4P_EyTq2q4Mq1q3Mmx + (_Exq3 + _Eyq1) * _ExTq2q3Mq1q4P_EyTq1q2Pq3q4Mmy + (_Exq4 - _2Eyq2) * _ExTq1q3Pq2q4P_EyThMq2q2Mq3q3Mmz,
            y = -doubles[0] * _2Tq2q4M_2Tq1q3MAx + doubles[3] * _2Tq1q2P_2Tq3q4MAy - 4f * q[2] * _oMtTq2q2MtTq3q3Maz + (-_2Exq3 - _Eyq1) * _ExThMq3q3Mq4q4P_EyTq2q4Mq1q3Mmx + (_Exq2 + _Eyq4) * _ExTq2q3Mq1q4P_EyTq1q2Pq3q4Mmy + (_Exq1 - _2Eyq3) * _ExTq1q3Pq2q4P_EyThMq2q2Mq3q3Mmz,
            z = doubles[1] * _2Tq2q4M_2Tq1q3MAx + doubles[2] * _2Tq1q2P_2Tq3q4MAy + (-_2Exq4 + _Eyq2) * _ExThMq3q3Mq4q4P_EyTq2q4Mq1q3Mmx + (-_Exq1 + _Eyq3) * _ExTq2q3Mq1q4P_EyTq1q2Pq3q4Mmy + _Exq2 * _ExTq1q3Pq2q4P_EyThMq2q2Mq3q3Mmz
        }.normalized;
    }

    private static Vector4 CalcRateOfChange(Vector4 q, Vector4 gradient, Vector3 Gyro)
    {
        return new Vector4()
        {
            x = 0.5f * (-q[1] * Gyro.x - q[2]*Gyro.y-q[3]*Gyro.z) - Beta * gradient[0],
            y = 0.5f * (q[0] * Gyro.x + q[2] * Gyro.z - q[3] * Gyro.y) - Beta * gradient[1],
            z = 0.5f * (q[0] * Gyro.y - q[1] * Gyro.z + q[3] * Gyro.x) - Beta * gradient[2],
            w = 0.5f * (q[0] * Gyro.z + q[1] * Gyro.y - q[2] * Gyro.x) - Beta * gradient[3]
        };
    }

    private static Quaternion CalcIntegration(Vector4 q, Vector4 qDot)
    {
        Quaternion n = new Quaternion()
        {
            x = q[0] + qDot[0] * Time.deltaTime,
            y = q[1] + qDot[1] * Time.deltaTime,
            z = q[2] + qDot[2] * Time.deltaTime,
            w = q[3] + qDot[3] * Time.deltaTime
        };
        float norm = 1f / Mathf.Sqrt(n.w * n.w + n.x * n.x + n.y * n.y + n.z * n.z);
        n[0] *= norm;
        n[1] *= norm;
        n[2] *= norm;
        n[3] *= norm;
        return n;
    }
}