using System.Linq;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Sanitization;

internal static class LocalVariableKeyUtility
{
    private const int DefaultMaxSuffixLength = 32;

    public static string BuildKey(string prefix, string rawSuffix, int maxSuffixLength = DefaultMaxSuffixLength)
    {
        string sanitizedSuffix = SanitizeSuffix(rawSuffix, maxSuffixLength);
        return prefix + sanitizedSuffix;
    }

    public static string SanitizeSuffix(string rawSuffix, int maxSuffixLength = DefaultMaxSuffixLength)
    {
        string suffix = string.IsNullOrWhiteSpace(rawSuffix) ? "unknown" : rawSuffix.Trim();
        suffix = new string(suffix.Select(static ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

        if (suffix.Length > maxSuffixLength)
        {
            suffix = suffix[..maxSuffixLength];
        }

        if (suffix.Length == 0)
        {
            suffix = "unknown";
        }

        return suffix;
    }
}
