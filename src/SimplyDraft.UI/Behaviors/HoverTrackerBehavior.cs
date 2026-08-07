// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using SimplyDraft.Core.Abstractions.UI;

namespace SimplyDraft.UI.Behaviors;

public sealed class HoverTrackerBehavior : Behavior<Control>
{
    public static readonly StyledProperty<object?> ItemProperty =
        AvaloniaProperty.Register<HoverTrackerBehavior, object?>(nameof(Item));
    
    public static readonly StyledProperty<IHoverTracker?> TrackerProperty =
        AvaloniaProperty.Register<HoverTrackerBehavior, IHoverTracker?>(nameof(Tracker));
    
    public object? Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public IHoverTracker? Tracker
    {
        get => GetValue(TrackerProperty);
        set => SetValue(TrackerProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is { } c)
        {
            c.PointerEntered += OnEntered;
            c.PointerExited += OnExited;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is { } c)
        {
            c.PointerEntered -= OnEntered;
            c.PointerExited -= OnExited;
        }
        
        base.OnDetaching();
    }

    private void OnEntered(object? sender, PointerEventArgs e)
        => Tracker?.SetHovered(Item);
    
    private void OnExited(object? sender, PointerEventArgs e)
        => Tracker?.ClearHovered(Item);
}