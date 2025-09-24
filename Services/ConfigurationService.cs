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
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

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
