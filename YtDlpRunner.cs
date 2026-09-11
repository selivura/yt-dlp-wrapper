using System.Diagnostics;

namespace yt_dlp_wrapper;

public class YtDlpRunner
{
    private const string DefaultExecutableName = "yt-dlp";
    private const string DefaultFfmpegExecutableName = "ffmpeg";

    public static async Task<string> RunAsync(string videoId, string downloadDirectory, IReadOnlyCollection<string> arguments)
    {
        var executablePath = ResolveExecutablePath();

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("yt-dlp executable was not found in configured path, PATH or local project folders.");
        }

        var ffmpegPath = ResolveExecutablePath("FFMPEG_PATH", DefaultFfmpegExecutableName);

        using var process = new Process();
        process.StartInfo.FileName = executablePath;

        foreach (var arg in arguments)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        if (!string.IsNullOrWhiteSpace(ffmpegPath) && !ContainsFfmpegLocationArgument(arguments))
        {
            process.StartInfo.ArgumentList.Add("--ffmpeg-location");
            process.StartInfo.ArgumentList.Add(ffmpegPath);
        }

        process.StartInfo.ArgumentList.Add("-o");
        process.StartInfo.ArgumentList.Add(Path.Combine(downloadDirectory, $"{videoId}.%(ext)s"));
        process.StartInfo.ArgumentList.Add(videoId);

        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;

        process.Start();

        var stdOut = await process.StandardOutput.ReadToEndAsync();
        var stdErr = await process.StandardError.ReadToEndAsync();
        Console.WriteLine(stdOut);

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"yt-dlp failed:\n{stdErr}");
        }

        var filePath = Directory
            .GetFiles(downloadDirectory)
            .FirstOrDefault(file => Path.GetFileName(file).StartsWith(videoId, StringComparison.Ordinal));

        if (filePath is null)
        {
            throw new FileNotFoundException("yt-dlp did not produce a file.");
        }

        return filePath;
    }

    private static string ResolveExecutablePath(string envVarName = null, string defaultName = null)
    {
        if (!string.IsNullOrWhiteSpace(envVarName))
        {
            foreach (var variableName in GetEnvironmentVariableNames(envVarName))
            {
                var configured = Environment.GetEnvironmentVariable(variableName);
                if (TryResolveExecutablePath(configured, out var resolvedConfigured))
                {
                    return resolvedConfigured;
                }
            }
        }

        if (defaultName is not null && TryResolveFromPath(defaultName, out var fromPath))
        {
            return fromPath;
        }

        var localCandidates = GetLocalCandidates(defaultName ?? DefaultExecutableName);

        foreach (var candidate in localCandidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> GetEnvironmentVariableNames(string baseName)
    {
        if (string.Equals(baseName, "YT-DLP_PATH", StringComparison.OrdinalIgnoreCase))
        {
            yield return "YT-DLP_PATH";
            yield return "YT_DLP_PATH";
            yield break;
        }

        if (string.Equals(baseName, "FFMPEG_PATH", StringComparison.OrdinalIgnoreCase))
        {
            yield return "FFMPEG_PATH";
            yield break;
        }

        yield return baseName;
    }

    private static IEnumerable<string> GetLocalCandidates(string executableName)
    {
        var suffixes = new[]
        {
            Path.Combine("external", "yt-dlp", "bin", $"{executableName}.exe"),
            Path.Combine("external", "yt-dlp", "bin", executableName),
            Path.Combine("external", "ffmpeg", "bin", $"{executableName}.exe"),
            Path.Combine("external", "ffmpeg", "bin", executableName),
            $"{executableName}.exe",
            executableName
        };

        foreach (var suffix in suffixes)
        {
            yield return Path.Combine(AppContext.BaseDirectory, suffix);
            yield return Path.Combine(Directory.GetCurrentDirectory(), suffix);
        }

        if (string.Equals(executableName, DefaultExecutableName, StringComparison.OrdinalIgnoreCase))
        {
            yield return Path.Combine(AppContext.BaseDirectory, "yt-dlp");
            yield return Path.Combine(AppContext.BaseDirectory, "external", "yt-dlp", "bin", "yt-dlp.exe");
            yield return Path.Combine(Directory.GetCurrentDirectory(), "external", "yt-dlp", "bin", "yt-dlp.exe");
            yield return Path.Combine(AppContext.BaseDirectory, "external", "yt-dlp", "bin", "yt-dlp");
        }
    }

    private static bool TryResolveExecutablePath(string configuredPath, out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return false;
        }

        var expanded = Environment.ExpandEnvironmentVariables(configuredPath).Trim();
        var candidates = new[]
        {
            expanded,
            Path.GetFullPath(expanded),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expanded)),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), expanded))
        };

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                fullPath = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool ContainsFfmpegLocationArgument(IEnumerable<string> arguments)
    {
        return arguments.Any(arg =>
            arg.StartsWith("--ffmpeg-location", StringComparison.OrdinalIgnoreCase)
            || string.Equals(arg, "--ffmpeg-location", StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryResolveFromPath(string fileName, out string fullPath)
    {
        fullPath = string.Empty;

        foreach (var pathEntry in Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? [])
        {
            if (string.IsNullOrWhiteSpace(pathEntry))
            {
                continue;
            }

            var candidate = Path.Combine(pathEntry, fileName);
            if (File.Exists(candidate))
            {
                fullPath = candidate;
                return true;
            }
        }

        return false;
    }
}
