using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ciallo.Geometry;
using Ciallo.Misc;
using Godot;
using MessagePack;
using MessagePack.Resolvers;
using MessagePackGodot;
using R3;

namespace Ciallo.Data;

public partial class DataAutoload : Node
{
    public IFormatterResolver DefaultResolver;
    
    public override void _EnterTree()
    {
        DefaultResolver = CompositeResolver.Create(
            GodotResolver.Instance,
            AttributeFormatterResolver.Instance,
            ReactivePropertyResolver.Instance,
            StandardResolver.Instance
        );
        MessagePackSerializer.DefaultOptions = MessagePackSerializer.DefaultOptions.WithResolver(DefaultResolver);

        bool preferenceFileExists = AppPreferences.TryLoad();
        if (!preferenceFileExists)
        {
            var idx = AppPreferences.SupportedLanguages.IndexOf(OS.GetLocale(), LanguageComparer.Instance);
            if(idx != -1)
                AppPreferences.Language.Value = AppPreferences.SupportedLanguages[idx];
        }
        AppPreferences.Language.Subscribe(TranslationServer.SetLocale).AddTo(this);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete) return;
        AppWorldManager.Clear();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();
            
        AppPreferences.Save();
    }
    
    public static IEnumerable<Type> GetSerializableTypes()
    {
        var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
        {
            try
            {
                return a.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }).Where(t => t is { IsAbstract: false });
        
        return allTypes.Where(t => t!.GetCustomAttributes(typeof(ToSerializeAttribute), false).Length > 0);
    }
}