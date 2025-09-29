using System;
using System.Runtime.Serialization;
using Arch.Core;
using Arch.Core.Extensions;

namespace Ciallo.Data;

[DataContract, ToSerialize] class BrushEntity : EntityWrapper;

[DataContract]
public class EntityWrapper
{
    [DataMember] public Entity E;

    public EntityWrapper(Entity e = default)
    {
        E = e;
    }

    public static implicit operator Entity(EntityWrapper wrapper) => wrapper.E;

    // public void Add<T>() where T : new() => E.Add<T>();
    // public void Add<T>(in T component) => E.Add(component);
    // public void Set<T>() where T : new() => E.Set<T>();
    // public void Set<T>(in T component) => E.Set(component);
    // public void Remove<T>() => E.Remove<T>();
    public bool Has<T>() => E.Has<T>();
    public T Get<T>() => E.Get<T>();
    public bool TryGet<T>(out T component) => E.TryGet(out component);
    
    public World World => World.Worlds[E.WorldId];
    public bool IsAlive() => World.IsAlive(E);
}

public static class EntityExtension
{
    public static void Add<TWrapper>(this Entity self, Entity e) where TWrapper : EntityWrapper
    {
        var wrapper = (TWrapper)Activator.CreateInstance(typeof(TWrapper));
        wrapper!.E = e;
        self.Add(wrapper);
    }
}