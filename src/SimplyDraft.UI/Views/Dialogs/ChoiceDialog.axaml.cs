// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SimplyDraft.UI.Views.Dialogs;

public sealed partial class ChoiceDialog : Window
{
    public ChoiceDialog()
    {
        InitializeComponent();
    }

    public static Task<int> ShowAsync(Window owner, string title, string message, params string[] buttons)
    {
        var d = new ChoiceDialog { Title = title };

        d.MessageText.Text = message;

        for (int i = 0; i < buttons.Length; i++)
        {
            var b = new Button
            {
                Content = buttons[i],
                MinWidth = 80,
                Tag = i
            };

            if (i == buttons.Length - 1)
                b.IsDefault = true;
            
            b.Click += d.OnChoiceClick;
            d.ButtonsPanel.Children.Add(b);
        }

        return d.ShowDialog<int>(owner);
    }

    private void OnChoiceClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button {Tag: int idx})
            Close(idx);
    }
}