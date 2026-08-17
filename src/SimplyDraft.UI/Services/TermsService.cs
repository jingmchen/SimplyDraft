// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Avalonia.Platform;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Configuration.UserStateSettings;
using SimplyDraft.Core.Logging;
using SimplyDraft.UI.Views.Dialogs;

namespace SimplyDraft.UI.Services;

public sealed partial class TermsService : ITermsService
{
    private readonly ISettingsProvider<UserStateSettings> _settings;
    private readonly IUriPaths _paths;
    private readonly ILogger<TermsService> _logger;
    
    public TermsService(ISettingsProvider<UserStateSettings> settings, IUriPaths paths, ILogger<TermsService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─── PUBLIC METHODS ────────────────────────
    public async Task<bool> EnsureAcceptedAsync()
    {
        var text = LoadBundledTermsConditions()
            ?? throw new InvalidDataException($"Bundled Terms Conditions invalid or not found.");

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

        if (string.Equals(_settings.Current.Terms.AcceptedTermsHash, hash, StringComparison.Ordinal))
            return true;
        
        bool accepted = await TermsDialog.ShowStandaloneAsync(text);

        if (!accepted)
        {
            LogTermsDeclined(hash);
            return false;
        }
        
        _settings.Current.Terms.AcceptedTermsHash = hash;
        _settings.Current.Terms.AcceptedAtUtc = DateTime.UtcNow;
        _settings.Current.Terms.AcceptedBy = Environment.UserName;

        try
        {
            _settings.Save();
        }
        catch (Exception ex)
        {
            LogUnableToPersistAcceptance(ex);
        }

        LogTermsAccepted(hash);
        return true;
    }

    // ─── PRIVATE METHODS ───────────────────────
    private string? LoadBundledTermsConditions()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri(_paths.TermsCondition));
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            LogTermsUnavailable(ex, _paths.TermsCondition);
            return null;
        }
    }
    
    [LoggerMessage(
        EventId = LogEventIDs.UI.TermsService.TermsAccepted,
        Level = LogLevel.Information,
        Message = "Terms and Conditions (version {TermsHash}) accepted.")]
    private partial void LogTermsAccepted(string termsHash);

    [LoggerMessage(
        EventId = LogEventIDs.UI.TermsService.TermsDeclined,
        Level = LogLevel.Warning,
        Message = "Terms and Conditions (version {TermsHash}) declined — shutting down application.")]
    private partial void LogTermsDeclined(string termsHash);

    [LoggerMessage(
        EventId = LogEventIDs.UI.TermsService.TermsUnavailable,
        Level = LogLevel.Error,
        Message = "Bundled Terms and Conditions could not be loaded from {Uri} — shutting down application.")]
    private partial void LogTermsUnavailable(Exception ex, string uri);

    [LoggerMessage(
        EventId = LogEventIDs.UI.TermsService.UnableToPersistAcceptance,
        Level = LogLevel.Error,
        Message = "Could not persist the Terms and Conditions acceptance — the user will be asked again next launch.")]
    private partial void LogUnableToPersistAcceptance(Exception ex);
}