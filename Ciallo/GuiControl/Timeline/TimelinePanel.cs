using System;
using Godot;
using R3;

namespace Ciallo.GuiControl.Timeline;

/// <summary>
/// Timeline panel
/// </summary>
[Instantiable]
public partial class TimelinePanel : Control
{
    public TimelinePanel BindPlayhead(ReactiveProperty<int> property)
    {
        throw new NotImplementedException();
        return this;
    }
}