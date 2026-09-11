using System.Text.RegularExpressions;

namespace yt_dlp_wrapper;

public static partial class YoutubeLinkParser
{
    private const int YouTubeVideoIdLength = 11;

    public static bool TryParseVideoId(string input, out string videoId)
    {
        videoId = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            if (TryGetIdFromAbsoluteUri(uri, out videoId))
            {
                return true;
            }
        }

        var match = YouTubeVideoIdRegex().Match(trimmed);
        if (match.Success)
        {
            var candidate = match.Value;
            if (candidate.Length == YouTubeVideoIdLength)
            {
                videoId = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetIdFromAbsoluteUri(Uri uri, out string videoId)
    {
        videoId = string.Empty;

        var host = uri.Host.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase);
        var path = uri.AbsolutePath.Trim('/');

        if (host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseCandidate(path, out videoId);
        }

        if (host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) || host.Equals("m.youtube.com", StringComparison.OrdinalIgnoreCase))
        {
            var queryValues = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var queryVideoId = queryValues["v"];
            if (!string.IsNullOrWhiteSpace(queryVideoId))
            {
                return TryParseCandidate(queryVideoId, out videoId);
            }

            if (!string.IsNullOrWhiteSpace(path))
            {
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (segments.Length > 0)
                {
                    return TryParseCandidate(segments[^1], out videoId);
                }
            }
        }

        return false;
    }

    private static bool TryParseCandidate(string candidate, out string videoId)
    {
        videoId = string.Empty;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var cleaned = candidate.Trim();
        if (cleaned.Length != YouTubeVideoIdLength)
        {
            return false;
        }

        if (!Regex.IsMatch(cleaned, "^[A-Za-z0-9_-]+$"))
        {
            return false;
        }

        videoId = cleaned;
        return true;
    }

    [GeneratedRegex("[A-Za-z0-9_-]{11}")]
    private static partial Regex YouTubeVideoIdRegex();
}
