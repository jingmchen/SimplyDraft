// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimplyDraft.UI.ViewModels.Components;

public sealed partial class VariableRowViewModel : ObservableObject
{
    private static readonly string[] TimeFormats = ["h\\:mm", "hh\\:mm", "h\\:mm\\:ss", "hh\\:mm\\:ss"];
    private string _typeName = "text";
    public string Name {get;}
    public string DisplayName => IsImplicit ? Name + " *" : Name;
    public bool IsFormula => Value.Length > 1 && Value[0] == '=';
    public bool ShowTextBox => IsFormula || _typeName is not ("yesno" or "date" or "time");
    public bool ShowYesNo => !IsFormula && _typeName == "yesno";
    public bool ShowDate => !IsFormula && _typeName == "date";
    public bool ShowTime => !IsFormula && _typeName == "time";
    public string TypeName
    {
        get => _typeName;
        set
        {
            var norm = string.IsNullOrWhiteSpace(value) ? "text" : value.Trim().ToLowerInvariant();
            if (SetProperty(ref _typeName, norm)) RaiseEditorState();
        }
    }
    public bool BoolValue
    {
        get => Value.Trim().ToLowerInvariant() is "yes" or "true" or "1";
        set
        {
            var s = value ? "yes" : "no";
            if (s != Value) Value = s;
        }
    }
    public DateTimeOffset? DateValue
    {
        get => DateTime.TryParseExact(Value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
               ? new DateTimeOffset(d, TimeSpan.Zero)
               : null;
        set
        {
            var s = value is DateTimeOffset dto ? dto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "";
            if (s != Value) Value = s;
        }
    }
    public TimeSpan? TimeValue
    {
        get => TimeSpan.TryParseExact(Value.Trim(), TimeFormats, CultureInfo.InvariantCulture, out var t)
               ? t
               : null;
        set
        {
            var s = value is TimeSpan ts ? ts.ToString("hh\\:mm", CultureInfo.InvariantCulture) : "";
            if (s != Value) Value = s;
        }
    }

    [ObservableProperty]
    public partial string Value {get; set;}

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayName))]
    public partial bool IsImplicit {get; set;}

    public VariableRowViewModel(string name, string value, bool isImplicit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Value = value ?? throw new ArgumentNullException(nameof(value));
        IsImplicit = isImplicit;
    }

    partial void OnValueChanged(string value) => RaiseEditorState();

    private void RaiseEditorState()
    {
        OnPropertyChanged(nameof(IsFormula));
        OnPropertyChanged(nameof(ShowTextBox));
        OnPropertyChanged(nameof(ShowYesNo));
        OnPropertyChanged(nameof(ShowDate));
        OnPropertyChanged(nameof(ShowTime));
        OnPropertyChanged(nameof(BoolValue));
        OnPropertyChanged(nameof(DateValue));
        OnPropertyChanged(nameof(TimeValue));
    }
}