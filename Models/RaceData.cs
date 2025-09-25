using System.Collections.Generic;

namespace GcpvLynx.Models;

/// <summary>
/// Represents a race with its parameters and skaters
/// </summary>
public class RaceData
{
    public string RaceParameters { get; set; } = string.Empty;
    public string RaceGroup { get; set; } = string.Empty;
    public string RaceStage { get; set; } = string.Empty;
    public string RaceNumber { get; set; } = string.Empty;
    public double? Laps { get; set; }
    public List<SkaterData> Skaters { get; set; } = new();

    /// <summary>
    /// Generates the event name in the format: "RaceGroup (RaceParameters) RaceStage"
    /// Example: "Open Men A (1500 111m) Final" or "Open Women B (1000 111m) Heat, 2 +2"
    /// </summary>
    /// <returns>The formatted event name</returns>
    public string GetEventName()
    {
        var parts = new List<string>();
        
        // Add race group if present
        if (!string.IsNullOrWhiteSpace(RaceGroup))
        {
            parts.Add(RaceGroup.Trim());
        }
        
        // Add race parameters in parentheses if present
        if (!string.IsNullOrWhiteSpace(RaceParameters))
        {
            parts.Add($"({RaceParameters.Trim()})");
        }
        
        // Add race stage if present
        if (!string.IsNullOrWhiteSpace(RaceStage))
        {
            parts.Add(RaceStage.Trim());
        }
        
        return string.Join(" ", parts);
    }

    public override string ToString()
    {
        var result = $"Race {RaceNumber}: {RaceParameters} - {RaceGroup}\n";
        result += $"  Stage: {RaceStage}\n";
        
        if (Laps.HasValue)
        {
            result += $"  Laps: {Laps}\n";
        }
        
        result += $"  Skaters ({Skaters.Count}) [parsed into separate fields]:\n";
        
        foreach (var skater in Skaters)
        {
            result += $"    {skater}\n";
        }
        
        return result;
    }
}

/// <summary>
/// Represents a skater in a race
/// </summary>
public class SkaterData
{
    public int Lane { get; set; }
    public string SkaterId { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Club { get; set; } = string.Empty;

    /// <summary>
    /// Gets the full skater name in the format "ID LASTNAME, FIRSTNAME"
    /// </summary>
    public string FullName => $"{SkaterId} {LastName}, {FirstName}";

    public override string ToString()
    {
        return $"Lane {Lane}: ID={SkaterId}, LastName={LastName}, FirstName={FirstName} ({Club})";
    }
}
