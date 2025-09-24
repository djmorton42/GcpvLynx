using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GcpvLynx.Models;

namespace GcpvLynx.Parsers;

/// <summary>
/// Parser for GCPV CSV files with the specific format described in parser_specs.md
/// </summary>
public class GcpvCsvParser
{
    /// <summary>
    /// Parses a GCPV CSV file and returns a list of race data
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <returns>List of RaceData objects</returns>
    public List<RaceData> ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var lines = File.ReadAllLines(filePath);
        var racesByNumber = new Dictionary<string, RaceData>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = ParseCsvLine(line);
            if (columns.Count == 0)
                continue;

            // Each line contains both race header info and skater data
            // Extract race information
            var raceNumber = GetRaceNumber(columns);
            if (string.IsNullOrEmpty(raceNumber))
            {
                continue;
            }

            // Get or create race
            if (!racesByNumber.ContainsKey(raceNumber))
            {
                racesByNumber[raceNumber] = ParseRaceHeader(columns);
            }

            // Extract skater data
            if (IsSkaterRow(columns))
            {
                var skater = ParseSkaterData(columns);
                if (skater != null)
                {
                    racesByNumber[raceNumber].Skaters.Add(skater);
                }
            }
        }

        return racesByNumber.Values.ToList();
    }

    /// <summary>
    /// Parses a single CSV line, handling quoted fields and commas within quotes
    /// </summary>
    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;
        var i = 0;

        while (i < line.Length)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    current += '"';
                    i += 2;
                }
                else
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                    i++;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
                i++;
            }
            else
            {
                current += c;
                i++;
            }
        }

        // Add the last field
        result.Add(current.Trim());

        return result;
    }

    /// <summary>
    /// Extracts the race number from a row
    /// </summary>
    private string GetRaceNumber(List<string> columns)
    {
        // Find "Race" and extract the next column value
        for (int i = 0; i < columns.Count - 1; i++)
        {
            if (columns[i].Trim().Equals("Race", StringComparison.OrdinalIgnoreCase))
            {
                return columns[i + 1].Trim();
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Checks if this row contains skater data
    /// </summary>
    private bool IsSkaterRow(List<string> columns)
    {
        // Look for the pattern: "Lane", "Skaters", "Club" followed by data
        for (int i = 0; i < columns.Count - 3; i++)
        {
            if (columns[i].Trim().Equals("Lane", StringComparison.OrdinalIgnoreCase) &&
                columns[i + 1].Trim().Equals("Skaters", StringComparison.OrdinalIgnoreCase) &&
                columns[i + 2].Trim().Equals("Club", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Parses race header information from a row
    /// </summary>
    private RaceData ParseRaceHeader(List<string> columns)
    {
        var race = new RaceData();

        // Find "Event :" and extract race parameters and group
        for (int i = 0; i < columns.Count - 2; i++)
        {
            if (columns[i].Equals("Event :", StringComparison.OrdinalIgnoreCase))
            {
                race.RaceParameters = columns[i + 1];
                if (i + 2 < columns.Count)
                {
                    race.RaceGroup = columns[i + 2];
                }
                break;
            }
        }

        // Find "Stage :" and extract race stage
        for (int i = 0; i < columns.Count - 1; i++)
        {
            if (columns[i].Equals("Stage :", StringComparison.OrdinalIgnoreCase))
            {
                race.RaceStage = columns[i + 1];
                break;
            }
        }

        // Find "Race" and extract race number
        for (int i = 0; i < columns.Count - 1; i++)
        {
            if (columns[i].Equals("Race", StringComparison.OrdinalIgnoreCase))
            {
                race.RaceNumber = columns[i + 1];
                break;
            }
        }

        return race;
    }

    /// <summary>
    /// Parses skater data from a row
    /// </summary>
    private SkaterData? ParseSkaterData(List<string> columns)
    {
        // Find the "Lane", "Skaters", "Club" pattern
        for (int i = 0; i < columns.Count - 3; i++)
        {
            if (columns[i].Equals("Lane", StringComparison.OrdinalIgnoreCase) &&
                columns[i + 1].Equals("Skaters", StringComparison.OrdinalIgnoreCase) &&
                columns[i + 2].Equals("Club", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the data that follows
                if (i + 3 < columns.Count)
                {
                    var laneStr = columns[i + 3];
                    var skaterName = i + 4 < columns.Count ? columns[i + 4] : "";
                    var club = i + 5 < columns.Count ? columns[i + 5] : "";

                    if (int.TryParse(laneStr, out int lane))
                    {
                        var (skaterId, lastName, firstName) = ParseSkaterName(skaterName);
                        
                        return new SkaterData
                        {
                            Lane = lane,
                            SkaterId = skaterId,
                            LastName = lastName,
                            FirstName = firstName,
                            Club = club
                        };
                    }
                }
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a skater name string in the format "ID LASTNAME, FIRSTNAME" into separate components
    /// </summary>
    /// <param name="skaterName">The full skater name string</param>
    /// <returns>A tuple containing (skaterId, lastName, firstName)</returns>
    private (string skaterId, string lastName, string firstName) ParseSkaterName(string skaterName)
    {
        if (string.IsNullOrWhiteSpace(skaterName))
        {
            return (string.Empty, string.Empty, string.Empty);
        }

        // Split by comma to separate last name and first name
        var parts = skaterName.Split(',', 2);
        if (parts.Length != 2)
        {
            // If no comma found, treat the whole string as the name
            return (string.Empty, skaterName.Trim(), string.Empty);
        }

        var lastNamePart = parts[0].Trim();
        var firstNamePart = parts[1].Trim();

        // Extract skater ID from the last name part
        var lastSpaceIndex = lastNamePart.LastIndexOf(' ');
        if (lastSpaceIndex > 0)
        {
            var skaterId = lastNamePart.Substring(0, lastSpaceIndex).Trim();
            var lastName = lastNamePart.Substring(lastSpaceIndex + 1).Trim();
            return (skaterId, lastName, firstNamePart);
        }

        // If no space found in last name part, treat it as just the last name
        return (string.Empty, lastNamePart, firstNamePart);
    }
}
