namespace InkStrokeModeler.Internal.Prediction.KalmanFilter;

internal sealed class AxisPredictor
{
    private const int PositionIndex = 0;
    private const int VelocityIndex = 1;
    private const int AccelerationIndex = 2;
    private const int JerkIndex = 3;

    private readonly KalmanFilter _kalmanFilter;

    public AxisPredictor(double processNoise, double measurementNoise, int minStableIteration)
    {
        _kalmanFilter = MakeKalmanFilter(processNoise, measurementNoise, minStableIteration);
    }

    private AxisPredictor(KalmanFilter kalmanFilter)
    {
        _kalmanFilter = kalmanFilter;
    }

    public bool Stable => _kalmanFilter.Stable;
    public int IterationCount => _kalmanFilter.IterationCount;
    public double Position => _kalmanFilter.StateEstimation[PositionIndex];
    public double Velocity => _kalmanFilter.StateEstimation[VelocityIndex];
    public double Acceleration => _kalmanFilter.StateEstimation[AccelerationIndex];
    public double Jerk => _kalmanFilter.StateEstimation[JerkIndex];

    public void Reset() => _kalmanFilter.Reset();

    public void Update(double observation) => _kalmanFilter.Update(observation);

    public AxisPredictor Clone() => new(_kalmanFilter.Clone());

    private static KalmanFilter MakeKalmanFilter(double processNoise, double measurementNoise, int minStableIteration)
    {
        const double dt = 1.0;
        const double dtSquared = dt * dt;
        const double dtCubed = dtSquared * dt;

        Matrix4 stateTransition = new(
            1, dt, .5 * dtSquared, 1.0 / 6 * dtCubed,
            0, 1, dt, .5 * dtSquared,
            0, 0, 1, dt,
            0, 0, 0, 1);
        Vec4 processNoiseVector = new(1.0 / 6 * dtCubed, 0.5 * dtSquared, dt, 1.0);
        Matrix4 processNoiseCovariance = MatrixMath.Multiply(MatrixMath.OuterProduct(processNoiseVector, processNoiseVector), processNoise);
        Vec4 measurementVector = new(1.0, 0.0, 0.0, 0.0);

        return new KalmanFilter(stateTransition, processNoiseCovariance, measurementVector, measurementNoise, minStableIteration);
    }
}
