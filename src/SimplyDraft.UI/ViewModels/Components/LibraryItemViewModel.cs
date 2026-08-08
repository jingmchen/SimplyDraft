// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using SimplyDraft.Core.Domains.Library;
using SimplyDraft.Core.Enums;
using SimplyDraft.UI.Common.MVVM;

namespace SimplyDraft.UI.ViewModels.Components;

public sealed class LibraryItemViewModel : ViewModelBase
{
    public LibraryItem Item {get;}
    public string Name => Item.Name;
    public string Detail
    {
        get => Item.Kind == LibraryItemKind.Template
            ? "Template"
            : Item.Baked
                ? (Item.TemplateRef is null ? "GeneratedChild" : $"Child of {Item.TemplateRef}")
                : "Child — missing template";
    }
    
    public string ModifiedText => Item.Modified == DateTime.MinValue
        ? ""
        : Item.Modified.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public LibraryItemViewModel(LibraryItem item)
        => Item = item ?? throw new ArgumentNullException(nameof(item));
}