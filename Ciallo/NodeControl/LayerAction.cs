using Godot;
using System;
using Ciallo.Core;

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
