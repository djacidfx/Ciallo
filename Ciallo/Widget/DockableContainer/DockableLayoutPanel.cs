using System;
using System.Collections.Generic;
using Godot;

namespace Ciallo.Widget.DockableContainer;

[Tool, GlobalClass]
public partial class DockableLayoutPanel : DockableLayoutNode
{
    private string[] _names = [];
    private int _currentTab;

    [Export]
    public string[] Names
    {
        get => _names;
        set
        {
            _names = value;
            EmitTreeChanged();
        }
    }

    [Export]
    public int CurrentTab
    {
        get => Math.Clamp(_currentTab, 0, Math.Max(0, _names.Length - 1));
        set
        {
            if (value == _currentTab) return;
            _currentTab = value;
            EmitTreeChanged();
        }
    }

    public DockableLayoutPanel()
    {
        ResourceName = "Tabs";
    }

    public override bool IsEmpty() => _names.Length == 0;

    public override string[] GetNames() => _names;

    public void PushName(string name)
    {
        var names = new List<string>(_names) { name };
        _names = names.ToArray();
        EmitTreeChanged();
    }

    public void InsertNode(int position, Node node)
    {
        var names = new List<string>(_names);
        names.Insert(position, node.Name);
        _names = names.ToArray();
        EmitTreeChanged();
    }

    public int FindName(string nodeName) => Array.IndexOf(_names, nodeName);

    public int FindChild(Node node) => FindName(node.Name);

    public void RemoveNode(Node node)
    {
        int index = FindChild(node);
        if (index < 0)
        {
            GD.PushWarning($"Remove failed, node '{node}' was not found");
            return;
        }

        var names = new List<string>(_names);
        names.RemoveAt(index);
        _names = names.ToArray();
        EmitTreeChanged();
    }

    public void RenameNode(string previousName, string newName)
    {
        int index = FindName(previousName);
        if (index < 0)
        {
            GD.PushWarning($"Rename failed, name '{previousName}' was not found");
            return;
        }

        _names[index] = newName;
        EmitTreeChanged();
    }

    public void UpdateNodes(HashSet<string> nodeNames, Dictionary<string, DockableLayoutPanel> leafByNodeName)
    {
        var names = new List<string>(_names);
        bool removedAny = false;

        for (int i = names.Count - 1; i >= 0; i--)
        {
            string current = names[i];
            if (!nodeNames.Contains(current) || leafByNodeName.ContainsKey(current))
            {
                names.RemoveAt(i);
                removedAny = true;
            }
            else
            {
                leafByNodeName[current] = this;
            }
        }

        if (!removedAny) return;
        _names = names.ToArray();
        EmitTreeChanged();
    }
}
