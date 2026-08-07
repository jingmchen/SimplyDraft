// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace SimplyDraft.UI.Behaviors;

public class DropTargetBehavior : Behavior<Control>
{
    public const string DropOkClass = "drop-ok";
    public static readonly StyledProperty<ICommand?> DropCommandProperty =
        AvaloniaProperty.Register<DropTargetBehavior, ICommand?>(nameof(DropCommand));
    
    public ICommand? DropCommand
    {
        get => GetValue(DropCommandProperty);
        set => SetValue(DropCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is { } c)
        {
            DragDrop.SetAllowDrop(c, true);
            c.AddHandler(DragDrop.DragOverEvent, OnDragOver);
            c.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            c.AddHandler(DragDrop.DropEvent, OnDrop);
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is { } c)
        {
            c.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
            c.RemoveHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            c.RemoveHandler(DragDrop.DropEvent, OnDrop);
            DragDrop.SetAllowDrop(c, false);
        }

        OnDragOverEnded();
        base.OnDetaching();
    }

    protected virtual object? BuildCommandParameter(DragEventArgs e)
        => e.DataTransfer.TryGetText();

    protected virtual void OnValidDragOver() { }

    protected virtual void OnDragOverEnded() { }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var parameter = BuildCommandParameter(e);
        var ok = parameter is not null && DropCommand?.CanExecute(parameter) == true;
        e.DragEffects = ok ? DragDropEffects.Move : DragDropEffects.None;
        AssociatedObject?.Classes.Set(DropOkClass, ok);

        if (ok)
            OnValidDragOver();
        else
            OnDragOverEnded();
        
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, RoutedEventArgs e)
    {
        AssociatedObject?.Classes.Set(DropOkClass, false);
        OnDragOverEnded();
    }
    
    private void OnDrop(object? sender, DragEventArgs e)
    {
        AssociatedObject?.Classes.Set(DropOkClass, false);
        OnDragOverEnded();

        var parameter = BuildCommandParameter(e);
        if (parameter is not null && DropCommand?.CanExecute(parameter) == true)
        {
            DropCommand.Execute(parameter);
            e.Handled = true;
        }
    }
    
    private static string? TryGetPayload(DragEventArgs e)
        => e.DataTransfer.TryGetText();
}