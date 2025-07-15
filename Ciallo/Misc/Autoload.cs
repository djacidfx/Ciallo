using System;
using System.Runtime.InteropServices;
using Godot;
using Humanizer;
using MemoryPack;
using ObservableCollections;
using R3;


namespace Ciallo.Misc;

public partial class Autoload : Node
{
    public override void _EnterTree()
    {
        MemoryPackFormatterProvider.RegisterGenericType(typeof(ReactiveProperty<>), typeof(ReactivePropertyFormatter<>));
        MemoryPackFormatterProvider.RegisterGenericType(typeof(ObservableList<>), typeof(ObservableListFormatter<>));
        
        // Handle quit manually (to save unsaved file)
        // GetTree().AutoAcceptQuit = false;
    }
    
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            Godot.Autoload.Configurations.Save();
    }

    public override void _Ready()
    {
#if OS_WINDOWS
        ushort HID_USAGE_PAGE_GENERIC = 0x01;
        ushort HID_USAGE_GENERIC_MOUSE = 0x02;
        uint RIDEV_INPUTSINK = 0x00000100;
        
        IntPtr hWnd = Native.GetActiveWindow();
        
        Native.RAWINPUTDEVICE[] rid = new Native.RAWINPUTDEVICE[1];
        rid[0].usUsagePage = HID_USAGE_PAGE_GENERIC;
        rid[0].usUsage = HID_USAGE_GENERIC_MOUSE;
        rid[0].dwFlags = RIDEV_INPUTSINK;
        rid[0].hwndTarget = hWnd;
        
        if (!Native.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(typeof(Native.RAWINPUTDEVICE))))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
#endif
    }
}