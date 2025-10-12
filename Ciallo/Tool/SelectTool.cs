using Massive;
using Ciallo.Widget;
using Stateless;
using R3;

namespace Ciallo.Tool;

using EntityParameterEvent = StateMachine<SelectTool.State,SelectTool.Event>.TriggerWithParameters<Entity>;

public partial class SelectTool : CommonToolBase
{
    public readonly StrokeSelectionHintInteractor HintInteractor = new();

    public readonly StateMachine<State, Event> ToolStateMachine = new(State.Idle);

    public new enum State
    {
        Idle,
        ImageLayerEdit,
    }

    public new enum Event
    {
        WorkingLayerSwitch,
        Cancel,
    }
    
    private readonly EntityParameterEvent _etWorkingLayerSwitch;


    public SelectTool() : base()
    {
        
    }

    public override InteractorBase HoveringInteractor => HintInteractor;
    public override void DrawProperty(PropertyContainer container)
    {
        
    }

    public override void _Ready()
    {
        
    }
}