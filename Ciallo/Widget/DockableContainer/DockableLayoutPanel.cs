using System;
using System.Collections.Generic;
using Godot;

namespace Ciallo.Widget;

[Tool, GlobalClass]
public partial class DockableLayoutPanel : DockableLayoutNode
{
    [Export]
    public string[] Names
    {
        get;
        set
        {
            value ??= [];
            if (field == value) return;

            // Preserve selection by panel identity when tabs are reordered or removed.
            string currentName = field.Length == 0 ? null : field[CurrentTab];
            field = value;
            if (currentName != null)
            {
                int currentIndex = Array.IndexOf(field, currentName);
                if (currentIndex >= 0)
                    CurrentTab = currentIndex;
            }
            EmitTreeChanged();
        }
    } = [];

    [Export]
    public int CurrentTab
    {
        // Names can shrink independently; callers must never receive an index outside the leaf.
        get => Math.Clamp(field, 0, Math.Max(0, Names.Length - 1));
        set
        {
            if (value == field) return;
            field = value;
            EmitTreeChanged();
        }
    }

    public DockableLayoutPanel()
    {
        ResourceName = "Tabs";
    }

    public override bool IsEmpty() => Names.Length == 0;

    public override string[] GetNames() => Names;

    public void PushName(string name)
    {
        var names = new List<string>(Names) { name };
        Names = names.ToArray();
    }

    public void InsertNode(int position, Node node)
    {
        var names = new List<string>(Names);
        names.Insert(position, node.Name);
        Names = names.ToArray();
    }

    public int FindName(string nodeName) => Array.IndexOf(Names, nodeName);

    public int FindChild(Node node) => FindName(node.Name);

    public void RemoveNode(Node node)
    {
        int index = FindChild(node);
        if (index < 0)
            throw new InvalidOperationException($"Layout node '{node.Name}' was not found");

        var names = new List<string>(Names);
        names.RemoveAt(index);
        Names = names.ToArray();
    }

    public void RenameNode(string previousName, string newName)
    {
        int index = FindName(previousName);
        if (index < 0)
            throw new InvalidOperationException($"Layout node '{previousName}' was not found");

        string[] names = [.. Names];
        names[index] = newName;
        Names = names;
    }

    public void UpdateNodes(HashSet<string> nodeNames, Dictionary<string, DockableLayoutPanel> leafByNodeName)
    {
        var names = new List<string>(Names);
        bool removedAny = false;

        // The traversal-wide map gives the first leaf ownership of each panel name.
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
        Names = names.ToArray();
    }
}
