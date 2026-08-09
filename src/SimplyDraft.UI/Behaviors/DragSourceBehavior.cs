// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace SimplyDraft.UI.Behaviors;

public sealed class DragSourceBehavior : Behavior<Control>
{
    private Point _origin;
    private PointerPressedEventArgs? _pressedArgs; // Avalonia 12 DoDragDropAsync requires original press args
    public static readonly StyledProperty<string?> PayloadProperty =
        AvaloniaProperty.Register<DragSourceBehavior, string?>(nameof(Payload));
    
    public static readonly StyledProperty<double> ThresholdProperty =
        AvaloniaProperty.Register<DragSourceBehavior, double>(nameof(Threshold), 4.0);
    
    // Stable id to hand to the drop target
    public string? Payload
    {
        get => GetValue(PayloadProperty);
        set => SetValue(PayloadProperty, value);
    }

    // Distance in DIPs the pointer must travel before a drag starts
    public double Threshold
    {
        get => GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        
        if (AssociatedObject is { } c)
        {
            c.PointerPressed += OnPressed;
            c.PointerReleased += OnReleased;
            c.PointerCaptureLost += OnCaptureLost;
            c.PointerMoved += OnMoved;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is { } c)
        {
            c.PointerPressed -= OnPressed;
            c.PointerReleased -= OnReleased;
            c.PointerCaptureLost -= OnCaptureLost;
            c.PointerMoved -= OnMoved;
        }

        base.OnDetaching();
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed) return;

        _origin = PointerPosition(e);
        _pressedArgs = e;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
        => _pressedArgs = null;
    
    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        => _pressedArgs = null;
    
    private async void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_pressedArgs is not { } pressed || Payload is not { } payload)
            return;
        
        var delta = PointerPosition(e) - _origin;
        if (Math.Abs(delta.X) < Threshold && Math.Abs(delta.Y) < Threshold)
            return; // Still within click territory
        
        _pressedArgs = null;

        var item = new DataTransferItem();
        item.Set(DataFormat.Text, payload);
        var data = new DataTransfer();
        data.Add(item);
        
        try
        {
            await DragDrop.DoDragDropAsync(pressed, data, DragDropEffects.Move);
        }
        catch
        {
            // Nothing to clean up
        }
    }
    
    private Point PointerPosition(PointerEventArgs e)
    {
        Visual reference = TopLevel.GetTopLevel(AssociatedObject)
            ?? (Visual)AssociatedObject!;
        
        return e.GetPosition(reference);
    }
}