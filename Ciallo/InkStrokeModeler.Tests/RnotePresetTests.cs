namespace InkStrokeModeler.Tests;

public sealed class RnotePresetTests
{
    [Fact]
    public void DefaultUsesRnotePreset()
    {
        StrokeModelParams parameters = StrokeModelParams.CreateDefault();

        Assert.Equal(120, parameters.Sampling.MinOutputRate);
        Assert.Equal(.01f, parameters.Sampling.EndOfStrokeStoppingDistance);
        Assert.Equal(20, parameters.Sampling.EndOfStrokeMaxIterations);
        Assert.Equal(200, parameters.Sampling.MaxOutputsPerCall);
        Assert.Equal(20, parameters.StylusStateModeler.MaxInputSamples);
        Assert.IsType<PredictionParams.StrokeEnd>(parameters.Prediction);
    }

    [Fact]
    public void RnotePresetKeepsSuggestedPhysicsDefaults()
    {
        StrokeModelParams parameters = StrokeModelParams.CreateRnoteDefault();

        Assert.Equal(.04, parameters.WobbleSmoother.Timeout.TotalSeconds);
        Assert.Equal(1.31f, parameters.WobbleSmoother.SpeedFloor);
        Assert.Equal(1.44f, parameters.WobbleSmoother.SpeedCeiling);
        Assert.Equal(11f / 32400f, parameters.PositionModeler.SpringMassConstant);
        Assert.Equal(72f, parameters.PositionModeler.DragConstant);
        Assert.False(parameters.PositionModeler.LoopContractionMitigation.IsEnabled);
    }
}
