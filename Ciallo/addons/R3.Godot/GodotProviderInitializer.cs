#nullable enable

using System;
using Ciallo.Diagnostics;
using Godot;

namespace R3;

public static class GodotProviderInitializer
{
    public static void SetDefaultObservableSystem()
    {
        SetDefaultObservableSystem(AppBugReport.Exception);
    }

    public static void SetDefaultObservableSystem(Action<Exception> unhandledExceptionHandler)
    {
        ObservableSystem.RegisterUnhandledExceptionHandler(unhandledExceptionHandler);
        ObservableSystem.DefaultTimeProvider = GodotTimeProvider.Process;
        ObservableSystem.DefaultFrameProvider = GodotFrameProvider.Process;
    }
}
