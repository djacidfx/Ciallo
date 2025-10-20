using Ciallo.Data;

namespace Frent;

public static class EntityExtension
{
    /// <summary>
    /// Helper function to quickly set wrapper of an entity
    /// </summary>
    public static void Add<TWrapper>(this Entity self, Entity e) where TWrapper : EntityWrapper, new()
    {
        var wrapper = new TWrapper
        {
            Value = e
        };
        self.Add(wrapper);
    }
}