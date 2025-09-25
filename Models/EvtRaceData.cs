using System.Collections.Generic;

namespace GcpvLynx.Models;

/// <summary>
/// Represents a race stored in EVT files
/// </summary>
public class EvtRaceData
{
    public string RaceNumber { get; set; } = string.Empty;
    public string FullEventName { get; set; } = string.Empty;
    public double? Laps { get; set; }
    public List<EvtSkaterData> Skaters { get; set; } = new();

    public override string ToString()
    {
        var result = $"Race {RaceNumber}: {FullEventName}\n";
        
        if (Laps.HasValue)
        {
            result += $"  Laps: {Laps}\n";
        }
        
        result += $"  Skaters ({Skaters.Count}):\n";
        
        foreach (var skater in Skaters)
        {
            result += $"    {skater}\n";
        }
        
        return result;
    }
}

/// <summary>
/// Represents a skater stored in EVT files
/// </summary>
public class EvtSkaterData
{
    public int Lane { get; set; }
    public string SkaterId { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"Lane {Lane}: ID={SkaterId}";
    }
}
