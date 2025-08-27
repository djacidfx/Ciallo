using System.Threading.Tasks;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace Tests;

[TestSuite, RequireGodotRuntime]
public class TestCurveEditor
{
    public Node Root;
    [Before]
    public void LoadScene()
    {
        ISceneRunner runner = ISceneRunner.Load("res://Tests/Objects/TestCurveEditor.tscn");
        runner.MaximizeView();
        Root = runner.Scene();
    }

    [TestCase]
    public void Run()
    {
        AssertObject(Root).IsNotNull();
    }
}