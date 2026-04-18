using System;
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
        Input.UseAccumulatedInput = false;
        GetTree().AutoAcceptQuit = false;

        // Message pack serializer setup
        var defaultResolver = CompositeResolver.Create(
            [
                EntityToIndexFormatter.Instance,
                TypeFormatter.Instance,
                ImageTextureFormatter.Instance,
                ImageFormatter.Instance
            ],
            [
                GodotResolver.Instance,
                AttributeFormatterResolver.Instance,
                ReactivePropertyResolver.Instance,
                StandardResolverAllowPrivate.Instance
            ]
        );
        MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(defaultResolver);
        DefaultOption = MessagePackSerializer.DefaultOptions;

        // Preference and load brush library data
        bool preferenceFileExists = AppPreference.TryLoad();
        if (!preferenceFileExists)
        {
            var idx = Preference.SupportedLanguages.IndexOf(OS.GetLocale(), LanguageComparer.Instance);
            if (idx != -1)
                AppPreference.Language.Value = Preference.SupportedLanguages[idx];
        }
        AppPreference.Language.Subscribe(TranslationServer.SetLocale).AddTo(this);

        if (preferenceFileExists)
        {
            GetWindow().Mode = AppPreference.WindowMode;
            if (AppPreference.WindowMode == Window.ModeEnum.Windowed)
            {
                GetWindow().SetPosition(AppPreference.WindowPosition);
                GetWindow().SetSize(AppPreference.WindowSize);
            }
        }

        GetWindow().SizeChanged += () =>
        {
            var window = GetWindow();
            AppPreference.WindowMode = window.GetMode();

            if (window.GetMode() == Window.ModeEnum.Windowed)
            {
                AppPreference.WindowPosition = GetWindow().Position;
                AppPreference.WindowSize = GetWindow().Size;
            }
        };

        bool brushFilesExists = AppStrokeBrushLibrary.TryLoad();
        if (!brushFilesExists) AppStrokeBrushLibrary.ResetBuiltInBrushes();
    }

    public override void _Ready()
    {
        AppStrokeBrushLibrary.BindToGui();
    }

    // ReSharper disable once AsyncVoidMethod
    public override async void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            var result = await AppDocumentManager.UserCloseWorkingDocument();
            if (!result) return;

            AppStrokeBrushLibrary.Save();
            AppPreference.Save();
            AppDocumentManager.Clear();
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