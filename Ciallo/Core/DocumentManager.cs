using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Arch.Core;
using Ciallo.Data;

namespace Ciallo.Core;

/// <summary>
/// Consider a document as a world object. This class manages the creation and storage of document worlds.
/// </summary>
public static class DocumentManager
{
    public static readonly List<World> DocumentWorlds = [];
    
    public static World CreateDocument([NotNull] DocumentSetting settings)
    {
        var world = World.Create();
        DocumentWorlds.Add(world);
        
        return world;
    }
    
    public static void RemoveDocument(World world)
    {
        if (!DocumentWorlds.Contains(world)) throw new KeyNotFoundException("The specified world does not exist in the document manager.");
        DocumentWorlds.Remove(world);
        world.Dispose();
    }
}