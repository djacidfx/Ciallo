using System.Numerics;

namespace InkStrokeModeler.Tests;

public sealed class StrokeModelerLifecycleTests
{
    [Fact]
    public void DownMoveUpProducesFiniteMonotonicResults()
    {
        StrokeModeler modeler = NewModeler();
        List<ModelerResult> results = [];

        modeler.Update(new ModelerInput(InputEventType.Down, new Vector2(0, 0), TimeSpan.Zero, .5f), results);
        modeler.Update(new ModelerInput(InputEventType.Move, new Vector2(.2f, 0), TimeSpan.FromSeconds(.02), .7f), results);
        modeler.Update(new ModelerInput(InputEventType.Move, new Vector2(.4f, .1f), TimeSpan.FromSeconds(.04), .8f), results);
        modeler.Update(new ModelerInput(InputEventType.Up, new Vector2(.5f, .1f), TimeSpan.FromSeconds(.05), .6f), results);

        Assert.NotEmpty(results);
        Assert.All(results, result =>
        {
            Assert.True(result.Position.IsFinite());
            Assert.InRange(result.Pressure, 0, 1);
        });
        Assert.True(results.Zip(results.Skip(1)).All(pair => pair.First.Time <= pair.Second.Time));
    }

    [Fact]
    public void PredictionIsAvailableDuringStrokeAndInvalidAfterUp()
    {
        StrokeModeler modeler = NewModeler();
        modeler.Update(new ModelerInput(InputEventType.Down, new Vector2(0, 0), TimeSpan.Zero, .5f));
        modeler.Update(new ModelerInput(InputEventType.Move, new Vector2(.5f, 0), TimeSpan.FromSeconds(.03), .6f));

        List<ModelerResult> prediction = modeler.Predict();

        Assert.All(prediction, result => Assert.True(result.Position.IsFinite()));

        modeler.Update(new ModelerInput(InputEventType.Up, new Vector2(.6f, 0), TimeSpan.FromSeconds(.04), .6f));
        Assert.Throws<InvalidOperationException>(() => modeler.Predict());
    }

    [Fact]
    public void DuplicateInputThrows()
    {
        StrokeModeler modeler = NewModeler();
        ModelerInput input = new(InputEventType.Down, new Vector2(0, 0), TimeSpan.Zero, .5f);

        modeler.Update(input);

        Assert.Throws<ArgumentException>(() => modeler.Update(input));
    }

    [Fact]
    public void PredictDoesNotMutateFutureUpdateResults()
    {
        ModelerInput down = new(InputEventType.Down, new Vector2(0, 0), TimeSpan.Zero, .5f);
        ModelerInput move1 = new(InputEventType.Move, new Vector2(.2f, 0), TimeSpan.FromSeconds(.02), .5f);
        ModelerInput move2 = new(InputEventType.Move, new Vector2(.3f, .1f), TimeSpan.FromSeconds(.04), .5f);

        StrokeModeler withPredict = NewModeler();
        withPredict.Update(down);
        withPredict.Update(move1);
        _ = withPredict.Predict();
        List<ModelerResult> afterPredict = withPredict.Update(move2);

        StrokeModeler withoutPredict = NewModeler();
        withoutPredict.Update(down);
        withoutPredict.Update(move1);
        List<ModelerResult> direct = withoutPredict.Update(move2);

        Assert.Equal(direct, afterPredict);
    }

    private static StrokeModeler NewModeler()
    {
        StrokeModeler modeler = new();
        modeler.Reset(StrokeModelParams.CreateRnoteDefault());
        return modeler;
    }
}
