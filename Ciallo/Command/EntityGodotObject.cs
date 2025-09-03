using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Ciallo.Data;
using Godot;

namespace Ciallo.Command;

public partial class EntityGodotObject(List<Entity> entities) : GodotObject
{
    private bool _destroyEntities = true;
    
    public override void _Notification(int what)
    {
        if (what != NotificationPredelete || !_destroyEntities) return;
        if(entities == null || entities.Count == 0) return;
        var wId = entities.First().WorldId;
        var world = World.Worlds.First(w => w.Id == wId);
        entities.ForEach(world.Destroy);
    }
    
    public void FreeWithoutDestroyingEntities()
    {
        _destroyEntities = false;
        Free();
    }
}