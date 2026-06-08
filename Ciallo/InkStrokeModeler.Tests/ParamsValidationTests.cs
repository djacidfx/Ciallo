namespace InkStrokeModeler.Tests;

public sealed class ParamsValidationTests
{
    [Fact]
    public void RnoteDefaultValidates()
    {
        StrokeModelParams.CreateRnoteDefault().Validate();
    }

    [Fact]
    public void LoopMitigationRequiresNormalProjection()
    {
        StrokeModelParams parameters = StrokeModelParams.CreateRnoteDefault() with
        {
            PositionModeler = StrokeModelParams.CreateRnoteDefault().PositionModeler with
            {
                LoopContractionMitigation = new LoopContractionMitigationParams
                {
                    IsEnabled = true,
                    SpeedLowerBound = 1,
                    SpeedUpperBound = 2,
                    InterpolationStrengthAtSpeedLowerBound = 1,
                    InterpolationStrengthAtSpeedUpperBound = 0,
                    MinSpeedSamplingWindow = TimeSpan.FromSeconds(.1),
                },
            },
        };

        Assert.Throws<ArgumentException>(parameters.Validate);
    }

    [Fact]
    public void KalmanParamsMustBeTunedBeforeUse()
    {
        StrokeModelParams parameters = StrokeModelParams.CreateRnoteDefault() with
        {
            Prediction = new PredictionParams.Kalman(),
        };

        Assert.Throws<ArgumentException>(parameters.Validate);
    }
}
