using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Ciallo.Command;
using Godot;
using Environment = System.Environment;

namespace Ciallo.GuiControl;

public partial class MenuHelp : PopupMenu
{
    public static readonly OrderedDictionary<string, AppAction> MenuItems = new()
    {
        { "User manual", null },
        { "About Ciallo", null },
        { "Copy system info", null },
        { "Bug report", null },
    };

    public override void _Ready()
    {
        foreach (var (i, item) in MenuItems.Index())
        {
            if (item.Key.StartsWith('-'))
            {
                AddSeparator();
                continue;
            }
            AddItem(Tr(item.Key));
            if (item.Value != null) SetItemShortcut(i, item.Value.Shortcut);
        }

        IndexPressed += id => OnIndexPressed((int)id);
    }

    private void OnIndexPressed(int id)
    {
        switch (id)
        {
            case 0:
                OS.ShellOpen("https://www.patreon.com/posts/143863276");
                break;
            case 1:
                GetTree().GetNodesInGroup("Dialog").OfType<AcceptDialog>().First(n => n.Name == "AboutCiallo").Popup();
                break;
            case 2:
                DisplayServer.ClipboardSet(CollectSystemInfo());
                break;
            case 3:
                OS.ShellOpen("https://github.com/ShenCiao/Ciallo/issues/new");
                break;
        }
    }

    public static string CollectSystemInfo()
    {
        var builder = new StringBuilder();
        var device = RenderingServer.GetRenderingDevice();

        builder.AppendLine($"OS: {Environment.OSVersion}");
        var cpuNames = new List<string>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
            foreach (var item in searcher.Get())
            {
                var name = item["Name"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    cpuNames.Add(name);
                }
            }
        }
        if (cpuNames.Count == 0)
        {
            cpuNames.Add(RuntimeInformation.ProcessArchitecture.ToString());
        }
        builder.AppendLine($"CPU: {string.Join("|", cpuNames)}");
        builder.AppendLine($"GPU: {device.GetDeviceName()}");
        var driverInfo = OS.GetVideoAdapterDriverInfo();
        builder.AppendLine($"Driver: {driverInfo[0]} {driverInfo[1]}");

        return builder.ToString().TrimEnd();
    }
}