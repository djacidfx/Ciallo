using Godot;
using R3;

namespace Ciallo;

public partial class TestNode : Node
{
    [Signal]
    public delegate void MySignalEventHandler(string myString);
    
    public override void _Ready()
    {
        
    }
}