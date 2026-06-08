namespace InkStrokeModeler.Internal.Prediction.KalmanFilter;

internal readonly record struct Vec4(double X, double Y, double Z, double W)
{
    public double this[int index] => index switch
    {
        0 => X,
        1 => Y,
        2 => Z,
        3 => W,
        _ => throw new IndexOutOfRangeException(),
    };

    public static Vec4 operator +(Vec4 lhs, Vec4 rhs) => new(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z, lhs.W + rhs.W);
    public static Vec4 operator *(Vec4 v, double k) => new(v.X * k, v.Y * k, v.Z * k, v.W * k);
    public static Vec4 operator /(Vec4 v, double k) => new(v.X / k, v.Y / k, v.Z / k, v.W / k);
}

internal readonly record struct Matrix4(
    double M00,
    double M01,
    double M02,
    double M03,
    double M10,
    double M11,
    double M12,
    double M13,
    double M20,
    double M21,
    double M22,
    double M23,
    double M30,
    double M31,
    double M32,
    double M33)
{
    public static Matrix4 Identity => new(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1);

    public static Matrix4 Zero => new(
        0, 0, 0, 0,
        0, 0, 0, 0,
        0, 0, 0, 0,
        0, 0, 0, 0);

    public double At(int row, int column) => (row, column) switch
    {
        (0, 0) => M00,
        (0, 1) => M01,
        (0, 2) => M02,
        (0, 3) => M03,
        (1, 0) => M10,
        (1, 1) => M11,
        (1, 2) => M12,
        (1, 3) => M13,
        (2, 0) => M20,
        (2, 1) => M21,
        (2, 2) => M22,
        (2, 3) => M23,
        (3, 0) => M30,
        (3, 1) => M31,
        (3, 2) => M32,
        (3, 3) => M33,
        _ => throw new IndexOutOfRangeException(),
    };

    public Matrix4 Transpose() => new(
        M00, M10, M20, M30,
        M01, M11, M21, M31,
        M02, M12, M22, M32,
        M03, M13, M23, M33);
}

internal static class MatrixMath
{
    public static double Dot(Vec4 lhs, Vec4 rhs)
    {
        double result = 0;
        for (int i = 0; i < 4; i++) result += lhs[i] * rhs[i];
        return result;
    }

    public static Matrix4 OuterProduct(Vec4 lhs, Vec4 rhs) => new(
        lhs.X * rhs.X, lhs.X * rhs.Y, lhs.X * rhs.Z, lhs.X * rhs.W,
        lhs.Y * rhs.X, lhs.Y * rhs.Y, lhs.Y * rhs.Z, lhs.Y * rhs.W,
        lhs.Z * rhs.X, lhs.Z * rhs.Y, lhs.Z * rhs.Z, lhs.Z * rhs.W,
        lhs.W * rhs.X, lhs.W * rhs.Y, lhs.W * rhs.Z, lhs.W * rhs.W);

    public static Matrix4 Add(Matrix4 lhs, Matrix4 rhs) => new(
        lhs.M00 + rhs.M00, lhs.M01 + rhs.M01, lhs.M02 + rhs.M02, lhs.M03 + rhs.M03,
        lhs.M10 + rhs.M10, lhs.M11 + rhs.M11, lhs.M12 + rhs.M12, lhs.M13 + rhs.M13,
        lhs.M20 + rhs.M20, lhs.M21 + rhs.M21, lhs.M22 + rhs.M22, lhs.M23 + rhs.M23,
        lhs.M30 + rhs.M30, lhs.M31 + rhs.M31, lhs.M32 + rhs.M32, lhs.M33 + rhs.M33);

    public static Matrix4 Subtract(Matrix4 lhs, Matrix4 rhs) => new(
        lhs.M00 - rhs.M00, lhs.M01 - rhs.M01, lhs.M02 - rhs.M02, lhs.M03 - rhs.M03,
        lhs.M10 - rhs.M10, lhs.M11 - rhs.M11, lhs.M12 - rhs.M12, lhs.M13 - rhs.M13,
        lhs.M20 - rhs.M20, lhs.M21 - rhs.M21, lhs.M22 - rhs.M22, lhs.M23 - rhs.M23,
        lhs.M30 - rhs.M30, lhs.M31 - rhs.M31, lhs.M32 - rhs.M32, lhs.M33 - rhs.M33);

    public static Matrix4 Multiply(Matrix4 m, double k) => new(
        m.M00 * k, m.M01 * k, m.M02 * k, m.M03 * k,
        m.M10 * k, m.M11 * k, m.M12 * k, m.M13 * k,
        m.M20 * k, m.M21 * k, m.M22 * k, m.M23 * k,
        m.M30 * k, m.M31 * k, m.M32 * k, m.M33 * k);

    public static Matrix4 Multiply(Matrix4 lhs, Matrix4 rhs)
    {
        double[] values = new double[16];
        for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++)
        for (int k = 0; k < 4; k++)
            values[i * 4 + j] += lhs.At(i, k) * rhs.At(k, j);

        return new Matrix4(
            values[0], values[1], values[2], values[3],
            values[4], values[5], values[6], values[7],
            values[8], values[9], values[10], values[11],
            values[12], values[13], values[14], values[15]);
    }

    public static Vec4 Multiply(Matrix4 m, Vec4 v)
    {
        double[] values = new double[4];
        for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++)
            values[i] += v[j] * m.At(i, j);
        return new Vec4(values[0], values[1], values[2], values[3]);
    }

    public static Vec4 Multiply(Vec4 v, Matrix4 m)
    {
        double[] values = new double[4];
        for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++)
            values[i] += v[j] * m.At(j, i);
        return new Vec4(values[0], values[1], values[2], values[3]);
    }
}
