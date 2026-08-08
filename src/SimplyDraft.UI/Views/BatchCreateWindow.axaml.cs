// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Controls;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.UI.ViewModels;

namespace SimplyDraft.UI.Views;

public sealed partial class BatchCreateWindow : Window
{
    private readonly BatchCreateWindowViewModel _viewModel;

    public BatchCreateWindow(BatchCreateWindowViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
    }

    public void Load(TemplateDocument template)
    {
        _viewModel.Load(template);
        DataContext = _viewModel;
    }
}