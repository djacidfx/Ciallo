using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.VectorFill)]
public class VectorFillLayerCreationTool : ToolBase
{
    public enum CelCreationStrategy
    {
        None, // Do not create cel, just create one vector fill layer like Illustation

        // Try to create many vector fill layers based on exposed cels 
        WithinCelFolder, // Put new vector fill layers into the same cel folder as exposed cels.
        // If exposed cels are already regular folders, put new layers into corresponding regular folders.
        // If not, wrap a new layer and their reference layers into a new regular folder, named by the layer current tool click on.
        NewCelFolder, // Create a cel folder and put new vector fill layers into it
    }
    public readonly ReactiveProperty<CelCreationStrategy> Strategy = new(CelCreationStrategy.WithinCelFolder);
    public readonly VectorFillLayerCreationHover Hover = new();
    public readonly PaintVectorFillMarkerInteractor Left = new();

    protected override void ConfigureStateMachine()
    {
        ConfigureInitial(Hover)
            .Permit(Press(MouseButton.Left), Left);
        Configure(Left)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(Press(AppActions.ConfirmInteraction), Hover);
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        if (layerEs.Length != 1) return false;
        var e = layerEs.Single();
        return e.Has<ShapeLayerSetting>();
    }

    public override void DrawProperty(PropertyContainer container)
    {
        base.DrawProperty(container);

        container.AddChild(new Label
        {
            Text = "[Vector Fill On Shape Layer Hint]".Tr(),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });

        var list = new ItemList()
        {
            AutoHeight = true,
            AutoWidth = true,
        }.BindEnum(Strategy);
        list.VisibleIf(Document.Get<SelectionManager>().WorkingCelFolder, e => !e.IsNull);
        container.AddProperty("Cel creation method", list);
    }

}

public class VectorFillLayerCreationHover : InteractiveSessionBase
{
    public override void Start(CursorButtonData data)
    {
        Document.Get<WorldBody>().DefaultCursorShape = Control.CursorShape.Cross;
    }

    public override void Moving(CursorMotionData data) { }

    public override void End(CursorButtonData data) => Cancel();
    public override void Cancel()
    {
        Document.Get<WorldBody>().DefaultCursorShape = default;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => false;
}
