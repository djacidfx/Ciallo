using Ciallo.Data;
using Frent;

namespace Ciallo;

public static class EntityExtension
{
    extension(Entity self)
    {
        /// <summary>
        /// Check if entity has been deleted by user. It may or may not has been deleted by undo stack.
        /// </summary>
        public bool IsDyingOrDead => !self.IsAlive || !self.Tagged<ToSerializeTag>();

        public bool IsDocument => self.World.Document() == self; // If entity is the singleton document entity.
        public Entity Document => self.World.Document();

        /// <summary>
        /// Return null if entity is null or do not have T component.
        /// </summary>
        public T TryGet<T>() where T : class => self.IsNull || !self.TryHas<T>() ? null : self.Get<T>();

        public static bool IsNotNull(Entity e) => !e.IsNull;
    }
}