using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ciallo.Misc;
using Godot;
using MessagePack;
using MessagePack.Resolvers;
using MessagePackGodot;
using R3;

namespace Ciallo.Data;

public partial class AutoloadData : Node
{
    public static MessagePackSerializerOptions DefaultOption;
    
    public override void _EnterTree()
    {
        GetTree().AutoAcceptQuit = false;
        // Message pack serializer setup
        var defaultResolver = CompositeResolver.Create(
            [EntityToIndexFormatter.Instance, TypeFormatter.Instance],
            [GodotResolver.Instance,
                AttributeFormatterResolver.Instance,
                ReactivePropertyResolver.Instance,
                StandardResolver.Instance]
        );
        MessagePackSerializer.DefaultOptions = MessagePackSerializer.DefaultOptions.WithResolver(defaultResolver);
        DefaultOption = MessagePackSerializer.DefaultOptions;

        // Preference and load brush library data
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

    public override void _Ready()
    {
        AppBrushLibrary.BindToGui();
    }

    public override void _Notification(int what)
    {
        if(what == NotificationWMCloseRequest)
        {
            AppBrushLibrary.Save();
            AppPreference.Save();
            AppWorldManager.Clear();
            GetTree().Quit();
            // Prevent default handler
            return;
        }
        // Force garbage collection makes godot memory leak warning disappear
        if (what != NotificationPredelete) return;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();
    }
}