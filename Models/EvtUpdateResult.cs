namespace GcpvLynx.Models;

/// <summary>
/// Represents the result of an EVT file update operation
/// </summary>
public class EvtUpdateResult
{
    public int RacesAdded { get; set; }
    public int RacesUpdated { get; set; }
    public int RacesUnchanged { get; set; }
    public int TotalRaces { get; set; }
    public bool BackupCreated { get; set; }
    public string? BackupPath { get; set; }

    /// <summary>
    /// Gets a summary message describing the update operation
    /// </summary>
    public string GetSummaryMessage()
    {
        var parts = new List<string>();
        
        if (RacesAdded > 0)
            parts.Add($"{RacesAdded} added");
        
        if (RacesUpdated > 0)
            parts.Add($"{RacesUpdated} updated");
        
        if (RacesUnchanged > 0)
            parts.Add($"{RacesUnchanged} unchanged");
        
        var summary = string.Join(", ", parts);
        if (string.IsNullOrEmpty(summary))
            summary = "No races processed";
        else
            summary = $"Races: {summary}";
        
        if (BackupCreated && !string.IsNullOrEmpty(BackupPath))
        {
            summary += $"\nBackup created: {Path.GetFileName(BackupPath)}";
        }
        
        return summary;
    }
}
