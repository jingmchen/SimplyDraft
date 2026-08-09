// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Avalonia.Platform;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.UI.Constants;
using SimplyDraft.UI.Views.Dialogs;

namespace SimplyDraft.UI.Services;

public sealed partial class TermsService : ITermsService
{
    private readonly IAppSettingsProvider _settings;
    private readonly ILogger<TermsService> _logger;
    private string? _termsText;
    private string? _termsHash;
    private bool _loadAttempted;
    public bool IsAcceptanceRequired
        => TryGetTerms(out _, out var hash)
           && !string.Equals(_settings.Current.TermsSection.AcceptedTermsHash, hash, StringComparison.Ordinal);
    
    public TermsService(IAppSettingsProvider settings, ILogger<TermsService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> EnsureAcceptedAsync()
    {
        if (!TryGetTerms(out var terms, out var hash))
            return true;
        
        if (string.Equals(_settings.Current.TermsSection.AcceptedTermsHash, hash, StringComparison.Ordinal))
            return true;
        
        // Standalone: this gate runs BEFORE the main window exists — the dialog is the first window.
        bool accepted = await TermsDialog.ShowStandaloneAsync(terms);

        if (!accepted)
        {
            LogTermsDeclined(hash);
            return false;
        }

        var section = _settings.Current.TermsSection;
        section.AcceptedTermsHash = hash;
        section.AcceptedAtUtc = DateTime.UtcNow;
        section.AcceptedBy = Environment.UserName;

        try
        {
            _settings.Save();
        }
        catch (Exception ex)
        {
            // The acceptance still holds for this session and is in the audit log below;
            // without the persisted hash the user is simply asked again next launch.
            LogUnableToPersistAcceptance(ex);
        }

        LogTermsAccepted(hash, Environment.UserName, Environment.MachineName);
        return true;
    }

    private bool TryGetTerms(out string terms, out string hash)
    {
        if (!_loadAttempted)
        {
            _loadAttempted = true;
            _termsText = LoadBundledTerms();
            if (_termsText is null)
                LogTermsUnavailable(TermsUri());
            else
                _termsHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_termsText)));
        }

        terms = _termsText ?? "";
        hash = _termsHash ?? "";
        return _termsText is not null;
    }

    private static string? LoadBundledTerms()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(TermsUri()));
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch
        {
            return null;
        }
    }

    private static string TermsUri()
        => $"avares://{typeof(TermsService).Assembly.GetName().Name}/" +
           $"{UIConstants.Bundled.FolderName.Assets}/{UIConstants.Bundled.FolderName.Markdowns}/" +
           $"{UIConstants.Bundled.FileName.TermsConditions}";
    
    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Information,
        Message = "Terms and Conditions accepted (version {TermsHash}) by {User} on {Machine}.")]
    private partial void LogTermsAccepted(string termsHash, string user, string machine);

    [LoggerMessage(
        EventId = 7002,
        Level = LogLevel.Warning,
        Message = "Terms and Conditions (version {TermsHash}) were declined — shutting down.")]
    private partial void LogTermsDeclined(string termsHash);

    [LoggerMessage(
        EventId = 7003,
        Level = LogLevel.Error,
        Message = "Bundled Terms and Conditions could not be loaded from {Uri} — continuing without the gate.")]
    private partial void LogTermsUnavailable(string uri);
    
    [LoggerMessage(
        EventId = 7004,
        Level = LogLevel.Error,
        Message = "Could not persist the Terms and Conditions acceptance — the user will be asked again next launch.")]
    private partial void LogUnableToPersistAcceptance(Exception ex);
}