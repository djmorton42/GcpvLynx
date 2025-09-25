using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GcpvLynx.Models;

namespace GcpvLynx.Parsers;

/// <summary>
/// Updates EVT files by appending race data from GCPV CSV parsing
/// </summary>
public class EvtUpdater
{
    private readonly string _evtFilePath;
    private readonly List<RaceData> _raceData;

    public EvtUpdater(string evtFilePath, List<RaceData> raceData)
    {
        _evtFilePath = evtFilePath ?? throw new ArgumentNullException(nameof(evtFilePath));
        _raceData = raceData ?? throw new ArgumentNullException(nameof(raceData));
    }

    /// <summary>
    /// Appends all race data to the EVT file
    /// </summary>
    public void AppendRacesToEvt()
    {
        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_evtFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Generate EVT content for all races
            var evtContent = GenerateEvtContent();
            
            // Append to the file (creates if doesn't exist)
            File.AppendAllText(_evtFilePath, evtContent);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to update EVT file '{_evtFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generates the complete EVT content for all races
    /// </summary>
    private string GenerateEvtContent()
    {
        var lines = new List<string>();

        foreach (var race in _raceData)
        {
            // Add race header line (13-field CSV)
            var headerLine = GenerateRaceHeaderLine(race);
            lines.Add(headerLine);

            // Add racer lines (3-field CSV)
            foreach (var skater in race.Skaters)
            {
                var racerLine = GenerateRacerLine(skater);
                lines.Add(racerLine);
            }
        }

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    /// <summary>
    /// Generates a race header line in 13-field CSV format
    /// Field 1: Race number, Field 4: Event name, Field 13: Number of laps
    /// </summary>
    private string GenerateRaceHeaderLine(RaceData race)
    {
        var fields = new string[13];
        
        // Field 1: Race number
        fields[0] = race.RaceNumber;
        
        // Fields 2-3: Empty (as per requirements)
        fields[1] = "";
        fields[2] = "";
        
        // Field 4: Event name (generated from race data)
        fields[3] = race.GetEventName();
        
        // Fields 5-12: Empty (as per requirements)
        for (int i = 4; i < 12; i++)
        {
            fields[i] = "";
        }
        
        // Field 13: Number of laps
        fields[12] = race.Laps?.ToString() ?? "";

        return string.Join(",", fields);
    }

    /// <summary>
    /// Generates a racer line in 3-field CSV format
    /// Field 1: Empty, Field 2: Skater ID, Field 3: Lane number
    /// </summary>
    private string GenerateRacerLine(SkaterData skater)
    {
        var fields = new string[3];
        
        // Field 1: Empty (as per requirements)
        fields[0] = "";
        
        // Field 2: Skater ID
        fields[1] = skater.SkaterId;
        
        // Field 3: Lane number
        fields[2] = skater.Lane.ToString();

        return string.Join(",", fields);
    }
}
