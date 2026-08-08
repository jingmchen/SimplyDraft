// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SimplyDraft.Core.Domains.UI;

namespace SimplyDraft.UI.Views.Dialogs;

public sealed partial class NewTemplateDialog : Window
{
    private const string BlankOption = "Blank template";
    private string _suggestedName = "";

    public NewTemplateDialog()
    {
        InitializeComponent();
    }

    public static async Task<NewTemplateSelection?> ShowAsync(Window owner, IReadOnlyList<string> seedTemplateNames)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(seedTemplateNames);
        
        var dialog = new NewTemplateDialog();
        var options = new List<string> { BlankOption };

        options.AddRange(seedTemplateNames);
        dialog.SourceList.ItemsSource = options;
        dialog.SourceList.SelectedIndex = 0;
        dialog.Opened += dialog.OnOpened;

        return await dialog.ShowDialog<NewTemplateSelection?>(owner);
    }

    private void OnOpened(object? sender, EventArgs e) => NameInput.Focus();

    private void OnSourcePicked(object? sender, SelectionChangedEventArgs e)
    {
        string currentName = NameInput.Text ?? "";
        if (currentName.Length != 0 && currentName != _suggestedName)
            return;   // user typed — keep it
        
        _suggestedName = SourceList.SelectedItem as string is { } picked && picked != BlankOption
            ? picked :
            "";
        
        NameInput.Text = _suggestedName;
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e) => Commit();
    private void OnCreate(object? sender, RoutedEventArgs e) => Commit();
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);

    private void Commit()
    {
        string name = (NameInput.Text ?? "").Trim();

        if (name.Length == 0)
        {
            ErrorText.Text = "Give the template a name.";
            ErrorText.IsVisible = true;
            NameInput.Focus();
            return;
        }

        string? seedTemplateName = SourceList.SelectedItem as string;
        Close(new NewTemplateSelection(name, seedTemplateName == BlankOption ? null : seedTemplateName));
    }
}