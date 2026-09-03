using NLog;
using NLog.Config;
using NLog.Targets;

namespace ServiceLib.Common;

public class Logging
{
    private static readonly Logger _logger1 = LogManager.GetLogger("Log1");
    private static readonly Logger _logger2 = LogManager.GetLogger("Log2");

    public static void Setup()
    {
        LoggingConfiguration config = new();
        FileTarget fileTarget = new();
        config.AddTarget("file", fileTarget);
        fileTarget.Layout = "${longdate}-${level:uppercase=true} ${message}";
        fileTarget.FileName = Utils.GetLogPath("${shortdate}.txt");
        config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, fileTarget));
        LogManager.Configuration = config;
    }

    public static void LoggingEnabled(bool enable)
    {
        if (!enable)
        {
            LogManager.SuspendLogging();
        }
        else if (!LogManager.IsLoggingEnabled())
        {
            LogManager.ResumeLogging();
        }
    }

    /// <summary>
    /// Remove userinfo/token/query credentials from a URL before logging.
    /// </summary>
    public static string SanitizeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }
        try
        {
            // Strip userinfo (user:pass@) which may carry subscription tokens.
            var at = url.IndexOf('@');
            var scheme = url.IndexOf("://", StringComparison.Ordinal);
            if (at > scheme && scheme >= 0)
            {
                url = url.Substring(0, scheme + 3) + url.Substring(at + 1);
            }
            // Do not log full query strings that may contain tokens.
            var q = url.IndexOf('?');
            if (q >= 0 && url.Length - q > 64)
            {
                url = url.Substring(0, q) + "?<redacted>";
            }
            else if (q >= 0 && (url.Contains("token", StringComparison.OrdinalIgnoreCase) || url.Contains("password", StringComparison.OrdinalIgnoreCase)))
            {
                url = url.Substring(0, q) + "?<redacted>";
            }
            return url;
        }
        catch
        {
            return "<unloggable-url>";
        }
    }

    public static void SaveLog(string strContent)
    {
        if (!LogManager.IsLoggingEnabled())
        {
            return;
        }

        _logger1.Info(strContent);
    }

    public static void SaveLog(string strTitle, Exception ex)
    {
        if (!LogManager.IsLoggingEnabled())
        {
            return;
        }

        _logger2.Debug($"{strTitle},{ex.Message}");
        _logger2.Debug(ex.StackTrace);
        if (ex?.InnerException != null)
        {
            _logger2.Error(ex.InnerException);
        }
    }
}
