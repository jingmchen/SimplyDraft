// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SimplyDraft.UI.Views.Dialogs;

public sealed partial class InputDialog : Window
{
    public InputDialog()
    {
        InitializeComponent();
    }

    public static async Task<string?> ShowAsync(Window owner, string title, string prompt, string initial = "")
    {
        var d = new InputDialog
        {
            Title = title
        };

        d.PromptText.Text = prompt;
        d.Input.Text = initial;
        d.Opened += d.OnOpened;

        return await d.ShowDialog<string?>(owner);
    }

    private void OnOpened(object? sender, EventArgs e) => Input.Focus();
    private void OnOk(object? sender, RoutedEventArgs e) => Close(Input.Text ?? "");
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);
}