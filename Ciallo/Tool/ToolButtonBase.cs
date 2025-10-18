using Ciallo.NodeControl;
using Ciallo.Tool;
using Ciallo.Widget;
using Frent;
using Godot;
using Humanizer;

public abstract partial class ToolButtonBase : Button, ITool
{
    public virtual string ToolName => GetType().Name.Humanize();
    public Entity Document { get; set; }
    /// <remark>
    /// Shen: I guess this is the only design to violate the "who create who delete" rule
    /// </remark>
    public abstract void DrawProperty(PropertyContainer container);

    public override string ToString() => ToolName;
    public abstract void OnLeftClick(CursorButtonData data);
    public abstract void OnLeftRelease(CursorButtonData data);
    public abstract void OnRightClick(CursorButtonData data);
    public abstract void OnRightRelease(CursorButtonData data);
    public abstract void OnMoving(CursorMotionData data);
    public abstract void OnKey(InputEventKey key);

    public abstract void OnActivate();
    public abstract void OnDeactivate();
}