// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SimplyDraft.Core.Common;
using SimplyDraft.Core.Domains.UI;
using SimplyDraft.UI.ViewModels.Components;

namespace SimplyDraft.UI.Views.Dialogs;

public sealed partial class VariableManagerDialog : Window
{
    private readonly ObservableCollection<ManagerRowViewModel> _rows = [];

    public VariableManagerDialog(IReadOnlyList<VariableDeclaration> initial)
    {
        ArgumentNullException.ThrowIfNull(initial);

        InitializeComponent();
        
        foreach (var r in initial)
            _rows.Add(new ManagerRowViewModel(r.Name, r.Type));
        
        RowsList.ItemsSource = _rows;
    }

    public static Task<List<VariableDeclaration>?> ShowAsync(Window owner, IReadOnlyList<VariableDeclaration> initial)
        => new VariableManagerDialog(initial).ShowDialog<List<VariableDeclaration>?>(owner);

    private void Add_Click(object? s, RoutedEventArgs e)
    {
        int n = _rows.Count + 1;
        string name = "var" + n;

        while (_rows.Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
            name = "var" + ++n;
        
        var row = new ManagerRowViewModel(name, "text");
        _rows.Add(row);
        RowsList.SelectedItem = row;
    }

    private void Remove_Click(object? s, RoutedEventArgs e)
    {
        if (RowsList.SelectedItem is ManagerRowViewModel r) _rows.Remove(r);
    }

    private void Ok_Click(object? s, RoutedEventArgs e)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in _rows)
        {
            var name = (r.Name ?? "").Trim();

            if (name.Length == 0)
            {
                ErrorText.Text = "Every variable needs a name.";
                return;
            }

            if (!VariableNameChecker.IsValid(name))
            {
                ErrorText.Text = $"'{name}' is not a valid name (letters, digits, underscore).";
                return;
            }

            if (!seen.Add(name))
            {
                ErrorText.Text = $"Duplicate variable name: {name}";
                return;
            }
        }

        Close(_rows.Select(r => new VariableDeclaration(r.Name.Trim(), r.Type)).ToList());
    }

    private void Cancel_Click(object? s, RoutedEventArgs e) => Close(null);
}