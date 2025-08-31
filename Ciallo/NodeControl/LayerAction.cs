using Godot;
using System;
using Ciallo.Command;

public partial class LayerAction : Control
{
    public void OnNewLayer()
    {
        new NewVectorLayerCmd().Commit();
    }

    public void OnRemoveLayer()
    {
        
    }
}
