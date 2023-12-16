using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace IctBaden.Stonehenge.Extension;

public class TreeNode
{
    public string Id { get; }

    /// for example fa fa-folder, fa fa-folder-open
    public string Icon { get; set; }

    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public string Name { get; set; }
    public string Tooltip { get; set; } = string.Empty;

    public bool Checkbox { get; init; }

    public List<TreeNode> Children { get; set; }

    // ReSharper disable once UnusedMember.Global
    public bool IsVisible => Parent?.IsExpanded ?? true;
    public bool IsExpanded { get; internal set; }
    public bool IsSelected { get; internal set; }
    public bool IsChecked { get; private set; }

    // ReSharper disable once UnusedMember.Global
    public bool HasChildren => Children.Count > 0;
    public bool IsDraggable { get; internal set; }


    public string Class => IsSelected ? "tree-selected" : "";

    public string ExpandIcon => HasChildren
        ? (IsExpanded ? "fa fa-caret-down" : "fa fa-caret-right")
        : "fa";


    public readonly TreeNode? Parent;
    public readonly object? Item;
    private readonly IStateProvider? _stateProvider;

    public TreeNode(TreeNode? parentNode, object? item, IStateProvider? stateProvider = null)
        : this(null, parentNode, item, stateProvider)
    {
    }

    public TreeNode(string? id, TreeNode? parentNode, object? item, IStateProvider? stateProvider = null)
    {
        Item = item;
        _stateProvider = stateProvider;
        Id = id ?? GetItemProperty("Id") as string ?? Guid.NewGuid().ToString("N");
        Parent = parentNode;
        Children = new List<TreeNode>();

        IsExpanded = stateProvider?.GetExpanded(Id) ?? false;
        IsChecked = stateProvider?.GetChecked(Id) ?? false;
        IsDraggable = parentNode != null;

        Name = GetItemProperty("Name") as string ?? string.Empty;
        Icon = GetItemProperty("Icon") as string ?? string.Empty;
    }

    private object? GetItemProperty(string propertyName)
    {
        if (Item == null) return null;
        var prop = Item.GetType().GetProperty(propertyName);
        return prop == null ? null : prop.GetValue(Item);
    }

    public IEnumerable<TreeNode> AllNodes()
    {
        yield return this;
        foreach (var node in Children.SelectMany(child => child.AllNodes()))
        {
            yield return node;
        }
    }

    public void SetExpanded(bool isExpanded)
    {
        IsExpanded = isExpanded;
        _stateProvider?.SetExpanded(Id, isExpanded);
    }
    public void SetSelected(bool isSelected)
    {
        IsSelected = isSelected;
    }
    public void SetChecked(bool isChecked)
    {
        IsChecked = isChecked;
        _stateProvider?.SetChecked(Id, isChecked);
    }

    
}