// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Domains.UI;

namespace SimplyDraft.UI.Views.Dialogs;

public sealed partial class TemplatePickerDialog : Window
{
    public TemplatePickerDialog()
    {
        InitializeComponent();
    }

    public static async Task<LibraryItem?> ShowAsync(Window owner, IEnumerable<LibraryItem> templates)
    {
        var d = new TemplatePickerDialog();
        d.List.ItemsSource = templates.Select(t => new TemplateChoice(t)).ToList();
        return await d.ShowDialog<LibraryItem?>(owner);
    }

    private void Commit()
        => Close((List.SelectedItem as TemplateChoice)?.Item);

    private void OnOk(object? sender, RoutedEventArgs e) => Commit();
    private void OnDoubleTapped(object? sender, TappedEventArgs e) => Commit();
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);
}