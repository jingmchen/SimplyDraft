// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace SimplyDraft.UI.Behaviors;

public sealed class WheelRouterBehavior : Behavior<Control>
{
    public static readonly StyledProperty<ICommand?> CtrlWheelCommandProperty =
        AvaloniaProperty.Register<WheelRouterBehavior, ICommand?>(nameof(CtrlWheelCommand));
    
    public static readonly StyledProperty<ICommand?> ShiftWheelCommandProperty =
        AvaloniaProperty.Register<WheelRouterBehavior, ICommand?>(nameof(ShiftWheelCommand));
    
    public ICommand? CtrlWheelCommand
    {
        get => GetValue(CtrlWheelCommandProperty);
        set => SetValue(CtrlWheelCommandProperty, value);
    }

    public ICommand? ShiftWheelCommand
    {
        get => GetValue(ShiftWheelCommandProperty);
        set => SetValue(ShiftWheelCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject?.AddHandler(InputElement.PointerWheelChangedEvent, OnWheel, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        AssociatedObject?.RemoveHandler(InputElement.PointerWheelChangedEvent, OnWheel);
        base.OnDetaching();
    }

    private void OnWheel(object? sender, PointerWheelEventArgs e)
    {
        ICommand? command = null;
        var delta = e.Delta.Y;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            command = CtrlWheelCommand;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            command = ShiftWheelCommand;
            
            if (delta == 0)
                delta = e.Delta.X; // Some platforms report Shift + Wheel as horizontal
        }

        if (command?.CanExecute(delta) == true)
        {
            command.Execute(delta);
            e.Handled = true;
        }
    }
}