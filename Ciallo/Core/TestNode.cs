using System;
using System.Text;
using Godot;
using Newtonsoft.Json;
using Ciallo.Misc;

namespace Ciallo.Core;

public partial class TestNode : Node
{
    public override void _Ready()
    {
        IntPtr handle = Native.GetActiveWindow();
        const int nChars = 256;
        StringBuilder Buff = new StringBuilder(nChars);
        Native.GetWindowText(handle, Buff, nChars);
        GD.Print(Buff.ToString());
        
        GD.Print("Test");
    }
}