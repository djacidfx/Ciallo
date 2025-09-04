using Godot;
using System;
using Ciallo.Command;

public partial class LayerAction : Control
{
    public void OnNewLayer()
    {
        new NewVectorLayerCmd([0]).Combine(new ChangeWorkingLayerCmd([0])).Commit();
    }

    public void OnRemoveLayer()
    {
        
    }
}
