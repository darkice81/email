using System.Diagnostics;
using System.Text.Json;

namespace FusionPortalClient.Services;

public interface IGameDetectionService
{
    string? DetectActiveGame();
}

/// <summary>
/// Matches currently-running processes against a process-name -> game-title
/// lookup table (Data/KnownGames.json). This is a starter list; extend it as
/// your organization's title pool grows. If multiple known games are
/// running, the first match is reported - FusionPortal's match record (which
/// game this match is for) should be used server-side to disambiguate.
/// </summary>
public class GameDetectionService : IGameDetectionService
{
    private readonly Dictionary<string, string> _knownGames;

    public GameDetectionService(string knownGamesJsonPath)
    {
        _knownGames = LoadKnownGames(knownGamesJsonPath);
    }

    private static Dictionary<string, string> LoadKnownGames(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var json = File.ReadAllText(path);
        var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                  ?? new Dictionary<string, string>();

        return new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase);
    }

    public string? DetectActiveGame()
    {
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var exeName = $"{process.ProcessName}.exe";
                if (_knownGames.TryGetValue(exeName, out var gameTitle))
                {
                    return gameTitle;
                }
            }
            catch (Exception)
            {
                // Access-denied on some system processes; ignore.
            }
            finally
            {
                process.Dispose();
            }
        }

        return null;
    }
}
