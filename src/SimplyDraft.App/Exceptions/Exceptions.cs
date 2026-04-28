namespace SimplyDraft.App.Exceptions;

public enum ErrorCode
{
    // 1000 - Errors relating to configuration
    ConfigMissingKey = 1001,
    ConfigInvalidValue = 1002,
    ConfigMissingSection = 1003,

    // 2000 - Operational exception
    InvalidThreadState = 2001,

    // 9000 - Unhandled exceptions
    Unexpected = 9000
}

public class SimplyDraftException : Exception
{
    public ErrorCode ErrorCode {get;}

    public SimplyDraftException(ErrorCode errorCode, string message, Exception? inner = null) : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}

public class ConfigException : SimplyDraftException
{
    public ConfigException(ErrorCode errorCode, string message, Exception? inner = null) : base(errorCode, message, inner) { }

    public static ConfigException MissingKey(string key)
    {
        return new ConfigException(
            errorCode: ErrorCode.ConfigMissingKey,
            message: $"Required configuration key '{key}' is missing."
        );
    }

    public static ConfigException InvalidValue(string key)
    {
        return new ConfigException(
            errorCode: ErrorCode.ConfigInvalidValue,
            message: $"Value for configuration key '{key}' is invalid or missing."
        );
    }

    public static ConfigException MissingSection(string section)
    {
        return new ConfigException(
            errorCode: ErrorCode.ConfigMissingSection,
            message: $"Required section '{section}' is missing in config."
        );
    }
}

public class OperationalException : SimplyDraftException
{
    public OperationalException(ErrorCode errorCode, string message, Exception? inner = null) : base(errorCode, message, inner) { }

    public static OperationalException InvalidThreadState(string thread)
    {
        return new OperationalException(
            errorCode: ErrorCode.InvalidThreadState,
            message: $"Operation not called on the proper thread: '{thread}'."
        );
    }
}