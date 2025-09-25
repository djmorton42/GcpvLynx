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
    /// Default configuration
    /// </summary>
    public static AppConfig Default => new();
}
