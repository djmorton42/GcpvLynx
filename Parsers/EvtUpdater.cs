using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using GcpvLynx.Models;
using GcpvLynx.Services;

namespace GcpvLynx.Parsers;

/// <summary>
/// Updates EVT files by intelligently merging race data from GCPV CSV parsing
/// </summary>
public class EvtUpdater
{
    private readonly string _evtFilePath;
    private readonly List<CsvRaceData> _raceData;
    private readonly bool _createBackup;
    private readonly ConfigurationService _configurationService;
    private readonly double? _lapOverride;

    public EvtUpdater(string evtFilePath, List<CsvRaceData> raceData, bool createBackup = false, ConfigurationService? configurationService = null, double? lapOverride = null)
    {
        _evtFilePath = evtFilePath ?? throw new ArgumentNullException(nameof(evtFilePath));
        _raceData = raceData ?? throw new ArgumentNullException(nameof(raceData));
        _createBackup = createBackup;
        _configurationService = configurationService ?? new ConfigurationService();
        _lapOverride = lapOverride;
    }

    /// <summary>
    /// Gets the effective lap count for a race, using override if provided
    /// </summary>
    private double? GetEffectiveLapCount(CsvRaceData race)
    {
        return _lapOverride ?? race.Laps;
    }

    /// <summary>
    /// Gets the encoding to use for writing EVT files based on configuration
    /// </summary>
    private Encoding GetOutputEncoding()
    {
        var config = _configurationService.GetConfiguration();
        return config.OutputEncoding.ToLowerInvariant() switch
        {
            "utf-16" => Encoding.Unicode,
            "utf-8" => Encoding.UTF8,
            "ascii" => Encoding.ASCII,
            _ => Encoding.ASCII // Default to ASCII for unknown values
        };
    }

    /// <summary>
    /// Intelligently updates the EVT file by merging new race data with existing data
    /// </summary>
    public EvtUpdateResult UpdateEvtFile()
    {
        try
        {
            var result = new EvtUpdateResult();
            
            // Determine if backup should be created based on configuration and parameter
            var config = _configurationService.GetConfiguration();
            var shouldCreateBackup = _createBackup || config.EvtBackupSettings.BackupsEnabled;
            
            // Create backup if requested and file exists (BEFORE any modifications)
            if (shouldCreateBackup && File.Exists(_evtFilePath))
            {
                var backupPath = CreateBackup();
                result.BackupCreated = true;
                result.BackupPath = backupPath;
            }

            // Load existing race data from EVT file
            var existingRaces = LoadExistingRacesFromEvt();

            // Merge new race data with existing data and get statistics
            var (mergedRaces, stats) = MergeRaceDataWithStats(existingRaces, _raceData);
            result.RacesAdded = stats.RacesAdded;
            result.RacesUpdated = stats.RacesUpdated;
            result.RacesUnchanged = stats.RacesUnchanged;
            result.TotalRaces = mergedRaces.Count;

            // Generate complete EVT content
            var evtContent = GenerateEvtContent(mergedRaces);

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_evtFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write the complete content to the file using the specified encoding
            var encoding = GetOutputEncoding();
            File.WriteAllText(_evtFilePath, evtContent, encoding);
            
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to update EVT file '{_evtFilePath}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates a timestamped backup of the EVT file
    /// </summary>
    private string CreateBackup()
    {
        var config = _configurationService.GetConfiguration();
        var backupDir = Path.Combine(Path.GetDirectoryName(_evtFilePath) ?? "", config.EvtBackupSettings.BackupDirectoryName);
        Directory.CreateDirectory(backupDir);

        var fileName = Path.GetFileNameWithoutExtension(_evtFilePath);
        var extension = Path.GetExtension(_evtFilePath);
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var backupFileName = $"{fileName}.{timestamp}{extension}";
        var backupPath = Path.Combine(backupDir, backupFileName);

        File.Copy(_evtFilePath, backupPath);
        return backupPath;
    }

    /// <summary>
    /// Loads existing race data from the EVT file using CsvHelper
    /// </summary>
    private List<EvtRaceData> LoadExistingRacesFromEvt()
    {
        var races = new List<EvtRaceData>();

        if (!File.Exists(_evtFilePath))
        {
            return races;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StringReader(File.ReadAllText(_evtFilePath));
        using var csv = new CsvReader(reader, config);
        
        var currentRace = (EvtRaceData?)null;

        while (csv.Read())
        {
            var record = csv.Parser.Record;
            if (record == null || record.Length == 0)
                continue;

            // Check if this is a race header line (13 fields, first field is race number)
            if (record.Length == 13 && !string.IsNullOrWhiteSpace(record[0]) && string.IsNullOrWhiteSpace(record[1]) && string.IsNullOrWhiteSpace(record[2]))
            {
                // Save previous race if exists
                if (currentRace != null)
                {
                    races.Add(currentRace);
                }

                // Start new race
                var fullEventName = record[3].Trim('"'); // Field 4 contains quoted full event name
                
                currentRace = new EvtRaceData
                {
                    RaceNumber = record[0],
                    FullEventName = fullEventName,
                    Laps = double.TryParse(record[12], out var laps) ? laps : null
                };
            }
            // Check if this is a skater line (3 fields, first field empty, second field is skater ID)
            else if (record.Length == 3 && string.IsNullOrWhiteSpace(record[0]) && !string.IsNullOrWhiteSpace(record[1]) && int.TryParse(record[2], out var lane))
            {
                if (currentRace != null)
                {
                    currentRace.Skaters.Add(new EvtSkaterData
                    {
                        SkaterId = record[1],
                        Lane = lane
                    });
                }
            }
        }

        // Add the last race if exists
        if (currentRace != null)
        {
            races.Add(currentRace);
        }

        return races;
    }


    /// <summary>
    /// Merges existing race data with new race data and returns statistics
    /// </summary>
    private (List<EvtRaceData> MergedRaces, (int RacesAdded, int RacesUpdated, int RacesUnchanged) Stats) MergeRaceDataWithStats(List<EvtRaceData> existingRaces, List<CsvRaceData> newRaces)
    {
        var mergedRaces = new List<EvtRaceData>(existingRaces);
        var stats = (RacesAdded: 0, RacesUpdated: 0, RacesUnchanged: 0);

        foreach (var newRace in newRaces)
        {
            var existingRace = mergedRaces.FirstOrDefault(r => r.RaceNumber == newRace.RaceNumber);
            
            if (existingRace != null)
            {
                // Check if the race data has actually changed
                var hasChanged = HasRaceDataChanged(existingRace, newRace);
                
                if (hasChanged)
                {
                    // Update existing race
                    existingRace.FullEventName = newRace.GetEventName();
                    existingRace.Laps = GetEffectiveLapCount(newRace);
                    existingRace.Skaters = newRace.Skaters.Select(s => new EvtSkaterData
                    {
                        Lane = s.Lane,
                        SkaterId = s.SkaterId
                    }).ToList();
                    stats.RacesUpdated++;
                }
                else
                {
                    stats.RacesUnchanged++;
                }
            }
            else
            {
                // Add new race
                mergedRaces.Add(new EvtRaceData
                {
                    RaceNumber = newRace.RaceNumber,
                    FullEventName = newRace.GetEventName(),
                    Laps = GetEffectiveLapCount(newRace),
                    Skaters = newRace.Skaters.Select(s => new EvtSkaterData
                    {
                        Lane = s.Lane,
                        SkaterId = s.SkaterId
                    }).ToList()
                });
                stats.RacesAdded++;
            }
        }

        // Count unchanged existing races (races that weren't in the new data)
        var newRaceNumbers = newRaces.Select(r => r.RaceNumber).ToHashSet();
        stats.RacesUnchanged += existingRaces.Count(r => !newRaceNumbers.Contains(r.RaceNumber));

        return (mergedRaces, stats);
    }

    /// <summary>
    /// Checks if race data has changed between existing and new race
    /// </summary>
    private bool HasRaceDataChanged(EvtRaceData existing, CsvRaceData newRace)
    {
        // Compare event names
        if (existing.FullEventName != newRace.GetEventName())
            return true;
        
        // Compare laps (using effective lap count to account for override)
        if (existing.Laps != GetEffectiveLapCount(newRace))
            return true;
        
        // Compare skaters
        if (existing.Skaters.Count != newRace.Skaters.Count)
            return true;
        
        var existingSkaters = existing.Skaters.OrderBy(s => s.Lane).ToList();
        var newSkaters = newRace.Skaters.OrderBy(s => s.Lane).ToList();
        
        for (int i = 0; i < existingSkaters.Count; i++)
        {
            if (existingSkaters[i].Lane != newSkaters[i].Lane ||
                existingSkaters[i].SkaterId != newSkaters[i].SkaterId)
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Generates the complete EVT content for all races
    /// </summary>
    private string GenerateEvtContent(List<EvtRaceData> races)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.None
        });

        // Sort races by race number (e.g., 3A, 25B, etc.)
        var sortedRaces = races.OrderBy(r => r.RaceNumber, new RaceNumberComparer()).ToList();

        foreach (var race in sortedRaces)
        {
            // Write race header line (13-field CSV)
            WriteRaceHeaderLine(csv, race);

            // Write racer lines (3-field CSV) - sorted by lane number
            foreach (var skater in race.Skaters.OrderBy(s => s.Lane))
            {
                WriteRacerLine(csv, skater);
            }
        }

        return writer.ToString();
    }

    /// <summary>
    /// Writes a race header line in 13-field CSV format
    /// Field 1: Race number, Field 4: Event name, Field 13: Number of laps
    /// </summary>
    private void WriteRaceHeaderLine(CsvWriter csv, EvtRaceData race)
    {
        // Field 1: Race number
        csv.WriteField(race.RaceNumber);
        
        // Fields 2-3: Empty (as per requirements)
        csv.WriteField("");
        csv.WriteField("");
        
        // Field 4: Event name (full constructed name, quoted to handle commas)
        csv.WriteField(race.FullEventName);
        
        // Fields 5-12: Empty (as per requirements)
        for (int i = 4; i < 12; i++)
        {
            csv.WriteField("");
        }
        
        // Field 13: Number of laps (format to avoid decimal precision issues)
        csv.WriteField(race.Laps?.ToString("0.#") ?? "");
        
        csv.NextRecord();
    }

    /// <summary>
    /// Writes a racer line in 3-field CSV format
    /// Field 1: Empty, Field 2: Skater ID, Field 3: Lane number
    /// </summary>
    private void WriteRacerLine(CsvWriter csv, EvtSkaterData skater)
    {
        // Field 1: Empty (as per requirements)
        csv.WriteField("");
        
        // Field 2: Skater ID
        csv.WriteField(skater.SkaterId);
        
        // Field 3: Lane number
        csv.WriteField(skater.Lane.ToString());
        
        csv.NextRecord();
    }
}

/// <summary>
/// Comparer for race numbers that handles alphanumeric sorting (e.g., 3A, 25B, etc.)
/// </summary>
public class RaceNumberComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        // Extract numeric and alphabetic parts
        var xParts = ParseRaceNumber(x);
        var yParts = ParseRaceNumber(y);

        // First compare numeric parts
        int numericComparison = xParts.Number.CompareTo(yParts.Number);
        if (numericComparison != 0)
            return numericComparison;

        // If numeric parts are equal, compare alphabetic parts
        return string.Compare(xParts.Suffix, yParts.Suffix, StringComparison.OrdinalIgnoreCase);
    }

    private (int Number, string Suffix) ParseRaceNumber(string raceNumber)
    {
        if (string.IsNullOrEmpty(raceNumber))
            return (0, "");

        // Find where the number ends and the suffix begins
        int numberEnd = 0;
        while (numberEnd < raceNumber.Length && char.IsDigit(raceNumber[numberEnd]))
        {
            numberEnd++;
        }

        if (numberEnd == 0)
            return (0, raceNumber); // No number part, treat as suffix only

        var numberPart = raceNumber.Substring(0, numberEnd);
        var suffixPart = raceNumber.Substring(numberEnd);

        if (int.TryParse(numberPart, out int number))
            return (number, suffixPart);
        else
            return (0, raceNumber); // Fallback if parsing fails
    }
}
