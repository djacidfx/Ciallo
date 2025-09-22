using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ciallo.Geometry;
using Ciallo.Misc;
using Ciallo.NodeControl;
using Godot;
using MessagePack;
using MessagePack.Resolvers;
using MessagePackGodot;
using R3;

namespace Ciallo.Data;

public partial class DataAutoload : Node
{
    public static MessagePackSerializerOptions DefaultOption;
    
    public override void _EnterTree()
    {
        var defaultResolver = CompositeResolver.Create(
            GodotResolver.Instance,
            AttributeFormatterResolver.Instance,
            ReactivePropertyResolver.Instance,
            StandardResolver.Instance
        );
        MessagePackSerializer.DefaultOptions = MessagePackSerializer.DefaultOptions.WithResolver(defaultResolver);
        DefaultOption = MessagePackSerializer.DefaultOptions;

        bool preferenceFileExists = AppPreference.TryLoad();
        if (!preferenceFileExists)
        {
            var idx = AppPreference.SupportedLanguages.IndexOf(OS.GetLocale(), LanguageComparer.Instance);
            if(idx != -1)
                AppPreference.Language.Value = AppPreference.SupportedLanguages[idx];
        }
        AppPreference.Language.Subscribe(TranslationServer.SetLocale).AddTo(this);

        bool brushesFileExists = AppBrushLibrary.TryLoad();
        if (!brushesFileExists) AppBrushLibrary.ResetBuiltInBrushes();
    }

    public override void _ExitTree()
    {
        AppBrushLibrary.Save();
        AppPreference.Save();
        AppWorldManager.Clear();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationPredelete) return;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();
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