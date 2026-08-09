// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia.Controls;
using Avalonia.Interactivity;
using SimplyDraft.Core.Domains.UI;
using SimplyDraft.UI.Common;

namespace SimplyDraft.UI.Views.Dialogs;

public sealed partial class HelpDialog : Window
{
    private IReadOnlyList<HelpTopic> _topics = [];

    public HelpDialog()
    {
        InitializeComponent();
    }

    public static void ShowMarkup(Window owner)
        => Show(owner, "Markup reference — LaTeX-style", HelpContent.Markup);

    public static void ShowScript(Window owner)
        => Show(owner, "Script reference — Python-style", HelpContent.Script);

    private static void Show(Window owner, string title, IReadOnlyList<HelpTopic> topics)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var dialog = new HelpDialog();
        dialog.Load(title, topics);
        dialog.Show(owner);
    }

    private void Load(string title, IReadOnlyList<HelpTopic> topics)
    {
        Title = title;
        _topics = topics;
        TopicsList.ItemsSource = topics.Select(SelectTitle).ToList();
        TopicsList.SelectedIndex = 0; // fires OnTopicSelected → renders the first topic
    }

    private void OnTopicSelected(object? sender, SelectionChangedEventArgs e)
    {
        int index = TopicsList.SelectedIndex;
        if (index < 0 || index >= _topics.Count) return;
        ShowTopic(_topics[index]);
    }

    private void ShowTopic(HelpTopic topic)
    {
        TopicTitle.Text = topic.Title;
        TopicIntro.Text = topic.Intro;
        TopicIntro.IsVisible = !string.IsNullOrEmpty(topic.Intro);
        EntriesList.ItemsSource = topic.Entries;
        TopicNote.Text = topic.Note;
        TopicNote.IsVisible = !string.IsNullOrEmpty(topic.Note);
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
    private static string SelectTitle(HelpTopic topic) => topic.Title;
}
