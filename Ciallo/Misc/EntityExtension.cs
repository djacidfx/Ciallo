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

    public static bool IsNull(this Entity self) => self.IsNull;
    public static bool IsNotNull(this Entity self) => !self.IsNull;
}