using System;
using Ciallo.Geometry;
using Godot;

namespace Ciallo.Tool;

public class PolylineTransformSession : IInteractiveSession
{
    public void BeforeSrcEnd(IInteractiveSession session)
    {
        throw new NotImplementedException();
    }
    public void Start(CursorButtonData data)
    {
        throw new NotImplementedException();
    }
    public void Interacting(CursorMotionData data)
    {
        throw new NotImplementedException();
    }
    public void End(CursorButtonData data)
    {
        throw new NotImplementedException();
    }
    public void Cancel()
    {
        throw new NotImplementedException();
    }
    public bool OnKey(InputEventKey key, CursorButtonData data)
    {
        return true;
    }
}