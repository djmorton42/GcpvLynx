using System.Collections.Generic;

namespace GcpvLynx.Models;

/// <summary>
/// Application configuration settings
/// </summary>
public class AppConfig
{
    /// <summary>
    /// List of suffixes to trim from the end of RaceGroup property
    /// </summary>
    public List<string> RaceGroupTrimSuffixes { get; set; } = new() { "male", "female" };

    /// <summary>
    /// Mapping of race distances to number of laps
    /// </summary>
    public Dictionary<string, double> DistanceLapMapping { get; set; } = new();

    /// <summary>
    /// EVT backup settings
    /// </summary>
    public EvtBackupSettings EvtBackupSettings { get; set; } = new();

    /// <summary>
    /// Output encoding for EVT files (utf-8, utf-16, or ascii)
    /// </summary>
    public string OutputEncoding { get; set; } = "ascii";

    /// <summary>
    /// Default configuration
    /// </summary>
    public static AppConfig Default => new();
}

/// <summary>
/// EVT backup configuration settings
/// </summary>
public class EvtBackupSettings
{
    /// <summary>
    /// Whether backups are enabled when updating EVT files
    /// </summary>
    public bool BackupsEnabled { get; set; } = true;

    /// <summary>
    /// Name of the backup directory (relative to EVT file location)
    /// </summary>
    public string BackupDirectoryName { get; set; } = "backups";
}
