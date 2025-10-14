using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using GcpvLynx.Models;

namespace GcpvLynx.Services;

/// <summary>
/// Service for managing application configuration
/// </summary>
public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    private AppConfig? _appConfig;

    public ConfigurationService()
    {
        var currentDir = Directory.GetCurrentDirectory();
        
        // Look for appsettings.json in the current directory first, then in the parent directory
        var appSettingsPath = Path.Combine(currentDir, "appsettings.json");
        if (!File.Exists(appSettingsPath))
        {
            appSettingsPath = Path.Combine(currentDir, "..", "GcpvLynx", "appsettings.json");
        }
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(appSettingsPath) ?? currentDir)
            .AddJsonFile(Path.GetFileName(appSettingsPath), optional: true, reloadOnChange: true);

        _configuration = builder.Build();
    }

    /// <summary>
    /// Gets the application configuration
    /// </summary>
    public AppConfig GetConfiguration()
    {
        if (_appConfig == null)
        {
            _appConfig = new AppConfig();
            _configuration.Bind(_appConfig);
            
            // Validate OutputEncoding
            ValidateOutputEncoding(_appConfig.OutputEncoding);
        }
        return _appConfig;
    }

    /// <summary>
    /// Validates the OutputEncoding property and exits with error if invalid
    /// </summary>
    private static void ValidateOutputEncoding(string outputEncoding)
    {
        var validEncodings = new[] { "utf-8", "utf-16", "ascii" };
        var normalizedEncoding = outputEncoding?.ToLowerInvariant();
        
        if (string.IsNullOrEmpty(normalizedEncoding) || !validEncodings.Contains(normalizedEncoding))
        {
            Console.WriteLine("❌ Error: Invalid OutputEncoding value in appsettings.json");
            Console.WriteLine($"   Current value: '{outputEncoding}'");
            Console.WriteLine($"   Valid values are: {string.Join(", ", validEncodings)}");
            Console.WriteLine();
            Console.WriteLine("Please update the OutputEncoding property in appsettings.json and try again.");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Reloads the configuration from file
    /// </summary>
    public void ReloadConfiguration()
    {
        _appConfig = null;
    }
}
