using System.Linq;
using Ciallo.Geometry;
using Frent;
using Godot;

namespace Ciallo.Tool;

/// <summary>
/// Base for the classes that handle canvas interactions. Tools hold one or more of these interactive session.
/// The actual place implementing canvas interaction behaviors.
/// </summary>
/// <remarks>
/// Key design idea:
/// Separating interactive logic from how to trigger an interactive session。This allows us to support key remapping and more.
/// E.g. Stroke drag interactor should not know it's started by dragging left mouse button, or pressing the 'G' key(like Blender)
/// The tool script implement ITool is responsible for triggering the interactive session according to user input and tool state.
/// </remarks>
public abstract class InteractiveSessionBase
{
    public Entity Document { get; set; }
    public Entity[] WorkingLayers;
    public Entity WorkingLayer => WorkingLayers.Single();
    public virtual void BeforeSrcEnd(InteractiveSessionBase session) { }
    public virtual void AfterSrcEnd(InteractiveSessionBase session) { }
    public abstract void Start(CursorButtonData data);
    public abstract void Interacting(CursorMotionData data);
    public abstract void End(CursorButtonData data);
    public virtual void BeforeDstStart(InteractiveSessionBase session) { }
    public virtual void AfterDstStart(InteractiveSessionBase session) { }
    public abstract void Cancel();
    public abstract bool OnKey(InputEventKey key, CursorButtonData data);
}