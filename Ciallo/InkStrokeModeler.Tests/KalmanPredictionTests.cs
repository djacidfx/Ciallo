using System.Numerics;

namespace InkStrokeModeler.Tests;

public sealed class KalmanPredictionTests
{
    [Fact]
    public void TunedKalmanPredictorCanProduceFinitePrediction()
    {
        StrokeModelParams parameters = StrokeModelParams.CreateRnoteDefault() with
        {
            Prediction = new PredictionParams.Kalman
            {
                ProcessNoise = .00026458,
                MeasurementNoise = .026458,
                MinCatchupVelocity = .01f,
                PredictionInterval = TimeSpan.FromSeconds(1.0 / 60),
                ConfidenceParams = new PredictionParams.Kalman.Confidence
                {
                    MaxEstimationDistance = .04f,
                    MinTravelSpeed = 3,
                    MaxTravelSpeed = 15,
                    MaxLinearDeviation = .2f,
                },
            },
        };

        StrokeModeler modeler = new();
        modeler.Reset(parameters);
        modeler.Update(new ModelerInput(InputEventType.Down, new Vector2(0, 0), TimeSpan.Zero, .5f));
        modeler.Update(new ModelerInput(InputEventType.Move, new Vector2(.1f, 0), TimeSpan.FromSeconds(.01), .5f));
        modeler.Update(new ModelerInput(InputEventType.Move, new Vector2(.2f, 0), TimeSpan.FromSeconds(.02), .5f));
        modeler.Update(new ModelerInput(InputEventType.Move, new Vector2(.3f, 0), TimeSpan.FromSeconds(.03), .5f));
        modeler.Update(new ModelerInput(InputEventType.Move, new Vector2(.5f, .1f), TimeSpan.FromSeconds(.04), .5f));

        List<ModelerResult> prediction = modeler.Predict();

        Assert.NotEmpty(prediction);
        Assert.All(prediction, result => Assert.True(result.Position.IsFinite()));
    }
}
