namespace InkStrokeModeler.Internal.Prediction.KalmanFilter;

internal readonly record struct Vec4(double X, double Y, double Z, double W)
{
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

    public Matrix4 Transpose() => new(
        M00, M10, M20, M30,
        M01, M11, M21, M31,
        M02, M12, M22, M32,
        M03, M13, M23, M33);
}

internal static class MatrixMath
{
    public static double Dot(Vec4 lhs, Vec4 rhs) =>
        lhs.X * rhs.X + lhs.Y * rhs.Y + lhs.Z * rhs.Z + lhs.W * rhs.W;

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
        return new Matrix4(
            lhs.M00 * rhs.M00 + lhs.M01 * rhs.M10 + lhs.M02 * rhs.M20 + lhs.M03 * rhs.M30,
            lhs.M00 * rhs.M01 + lhs.M01 * rhs.M11 + lhs.M02 * rhs.M21 + lhs.M03 * rhs.M31,
            lhs.M00 * rhs.M02 + lhs.M01 * rhs.M12 + lhs.M02 * rhs.M22 + lhs.M03 * rhs.M32,
            lhs.M00 * rhs.M03 + lhs.M01 * rhs.M13 + lhs.M02 * rhs.M23 + lhs.M03 * rhs.M33,
            lhs.M10 * rhs.M00 + lhs.M11 * rhs.M10 + lhs.M12 * rhs.M20 + lhs.M13 * rhs.M30,
            lhs.M10 * rhs.M01 + lhs.M11 * rhs.M11 + lhs.M12 * rhs.M21 + lhs.M13 * rhs.M31,
            lhs.M10 * rhs.M02 + lhs.M11 * rhs.M12 + lhs.M12 * rhs.M22 + lhs.M13 * rhs.M32,
            lhs.M10 * rhs.M03 + lhs.M11 * rhs.M13 + lhs.M12 * rhs.M23 + lhs.M13 * rhs.M33,
            lhs.M20 * rhs.M00 + lhs.M21 * rhs.M10 + lhs.M22 * rhs.M20 + lhs.M23 * rhs.M30,
            lhs.M20 * rhs.M01 + lhs.M21 * rhs.M11 + lhs.M22 * rhs.M21 + lhs.M23 * rhs.M31,
            lhs.M20 * rhs.M02 + lhs.M21 * rhs.M12 + lhs.M22 * rhs.M22 + lhs.M23 * rhs.M32,
            lhs.M20 * rhs.M03 + lhs.M21 * rhs.M13 + lhs.M22 * rhs.M23 + lhs.M23 * rhs.M33,
            lhs.M30 * rhs.M00 + lhs.M31 * rhs.M10 + lhs.M32 * rhs.M20 + lhs.M33 * rhs.M30,
            lhs.M30 * rhs.M01 + lhs.M31 * rhs.M11 + lhs.M32 * rhs.M21 + lhs.M33 * rhs.M31,
            lhs.M30 * rhs.M02 + lhs.M31 * rhs.M12 + lhs.M32 * rhs.M22 + lhs.M33 * rhs.M32,
            lhs.M30 * rhs.M03 + lhs.M31 * rhs.M13 + lhs.M32 * rhs.M23 + lhs.M33 * rhs.M33);
    }

    public static Vec4 Multiply(Matrix4 m, Vec4 v) => new(
        v.X * m.M00 + v.Y * m.M01 + v.Z * m.M02 + v.W * m.M03,
        v.X * m.M10 + v.Y * m.M11 + v.Z * m.M12 + v.W * m.M13,
        v.X * m.M20 + v.Y * m.M21 + v.Z * m.M22 + v.W * m.M23,
        v.X * m.M30 + v.Y * m.M31 + v.Z * m.M32 + v.W * m.M33);

    public static Vec4 Multiply(Vec4 v, Matrix4 m) => new(
        v.X * m.M00 + v.Y * m.M10 + v.Z * m.M20 + v.W * m.M30,
        v.X * m.M01 + v.Y * m.M11 + v.Z * m.M21 + v.W * m.M31,
        v.X * m.M02 + v.Y * m.M12 + v.Z * m.M22 + v.W * m.M32,
        v.X * m.M03 + v.Y * m.M13 + v.Z * m.M23 + v.W * m.M33);
}
