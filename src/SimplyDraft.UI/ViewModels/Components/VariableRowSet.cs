// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SimplyDraft.UI.ViewModels.Components;

public sealed class VariableRowSet
{
    public ObservableCollection<VariableRowViewModel> Rows {get;} = [];
    public event Action? ValueChanged;

    public bool Has(string name)
        => Rows.Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
    
    public VariableRowViewModel? Find(string name)
        => Rows.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));

    public void Add(VariableRowViewModel row)
    {
        row.PropertyChanged += OnRowChanged;
        Rows.Add(row);
    }

    public void RemoveAt(int index)
    {
        Rows[index].PropertyChanged -= OnRowChanged;
        Rows.RemoveAt(index);
    }

    public void Clear()
    {
        for (int i = Rows.Count - 1; i >= 0; i--)
            RemoveAt(i);
    }

    public void ApplyTypes(IReadOnlyDictionary<string, string> types)
    {
        foreach (var r in Rows)
            r.TypeName = types.TryGetValue(r.Name, out var t) ? t : "text";
    }

    public void ReconcileImplicit(IReadOnlyList<string> scanned)
    {
        var scanSet = new HashSet<string>(scanned, StringComparer.OrdinalIgnoreCase);

        foreach (var name in scanned)
            if (!Has(name))
                Add(new VariableRowViewModel(name, "", isImplicit: true));
        for (int i = Rows.Count - 1; i >= 0; i--)
        {
            var row = Rows[i];
            if (row.IsImplicit && !scanSet.Contains(row.Name) && string.IsNullOrEmpty(row.Value))
                RemoveAt(i);
        }
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(VariableRowViewModel.Value))
            return;
        if (sender is VariableRowViewModel row && row.IsImplicit && row.Value.Length > 0)
            row.IsImplicit = false;
        ValueChanged?.Invoke();
    }
}