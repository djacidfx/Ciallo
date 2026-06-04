namespace InkStrokeModeler.Internal.Prediction.KalmanFilter;

internal sealed class KalmanFilter
{
    private readonly Matrix4 _stateTransitionMatrix;
    private readonly Matrix4 _processNoiseCovarianceMatrix;
    private readonly Vec4 _measurementVector;
    private readonly double _measurementNoiseVariance;
    private readonly int _minStableIteration;

    private Vec4 _stateEstimation;
    private Matrix4 _errorCovarianceMatrix = Matrix4.Identity;
    private int _iterationCount;

    public KalmanFilter(
        Matrix4 stateTransition,
        Matrix4 processNoiseCovariance,
        Vec4 measurementVector,
        double measurementNoiseVariance,
        int minStableIteration)
    {
        _stateTransitionMatrix = stateTransition;
        _processNoiseCovarianceMatrix = processNoiseCovariance;
        _measurementVector = measurementVector;
        _measurementNoiseVariance = measurementNoiseVariance;
        _minStableIteration = minStableIteration;
    }

    public Vec4 StateEstimation => _stateEstimation;
    public bool Stable => _iterationCount >= _minStableIteration;
    public int IterationCount => _iterationCount;

    public KalmanFilter Clone() => new(
        _stateTransitionMatrix,
        _processNoiseCovarianceMatrix,
        _measurementVector,
        _measurementNoiseVariance,
        _minStableIteration)
    {
        _stateEstimation = _stateEstimation,
        _errorCovarianceMatrix = _errorCovarianceMatrix,
        _iterationCount = _iterationCount,
    };

    public void Update(double observation)
    {
        if (_iterationCount++ == 0)
        {
            _stateEstimation = new Vec4(observation, 0, 0, 0);
            return;
        }

        Predict();
        double y = observation - MatrixMath.Dot(_measurementVector, _stateEstimation);
        double s = MatrixMath.Dot(MatrixMath.Multiply(_measurementVector, _errorCovarianceMatrix), _measurementVector) +
                   _measurementNoiseVariance;
        Vec4 kalmanGain = MatrixMath.Multiply(_measurementVector, _errorCovarianceMatrix) / s;

        _stateEstimation += kalmanGain * y;

        Matrix4 iKh = MatrixMath.Subtract(Matrix4.Identity, MatrixMath.OuterProduct(kalmanGain, _measurementVector));
        _errorCovarianceMatrix = MatrixMath.Add(
            MatrixMath.Multiply(MatrixMath.Multiply(iKh, _errorCovarianceMatrix), iKh.Transpose()),
            MatrixMath.Multiply(MatrixMath.OuterProduct(kalmanGain, kalmanGain), _measurementNoiseVariance));
    }

    public void Reset()
    {
        _stateEstimation = new Vec4(0, 0, 0, 0);
        _errorCovarianceMatrix = Matrix4.Identity;
        _iterationCount = 0;
    }

    private void Predict()
    {
        _stateEstimation = MatrixMath.Multiply(_stateTransitionMatrix, _stateEstimation);
        _errorCovarianceMatrix = MatrixMath.Add(
            MatrixMath.Multiply(MatrixMath.Multiply(_stateTransitionMatrix, _errorCovarianceMatrix), _stateTransitionMatrix.Transpose()),
            _processNoiseCovarianceMatrix);
    }
}
