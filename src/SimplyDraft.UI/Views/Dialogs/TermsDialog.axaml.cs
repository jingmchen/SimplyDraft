// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SimplyDraft.UI.Views.Dialogs;

public sealed partial class TermsDialog : Window
{
    private readonly TaskCompletionSource<bool> _result = new();
    private bool _accepted;
    
    public TermsDialog()
    {
        InitializeComponent();
    }

    public static Task<bool> ShowStandaloneAsync(string termsText)
    {
        ArgumentNullException.ThrowIfNull(termsText);
        var dialog = new TermsDialog();
        dialog.TermsText.Text = termsText;
        dialog.Show();
        return dialog._result.Task;
    }

    protected override void OnClosed(EventArgs e)
    {
        _result.TrySetResult(_accepted);
        base.OnClosed(e);
    }

    private void OnDecline(object? sender, RoutedEventArgs e) => Close();
    private void OnAccept(object? sender, RoutedEventArgs e)
    {
        _accepted = true;
        Close();
    }
}