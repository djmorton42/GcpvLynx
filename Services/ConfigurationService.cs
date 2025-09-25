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
        }
        return _appConfig;
    }

    /// <summary>
    /// Reloads the configuration from file
    /// </summary>
    public void ReloadConfiguration()
    {
        _appConfig = null;
    }
}
