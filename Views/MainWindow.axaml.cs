using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GcpvLynx.Parsers;
using GcpvLynx.Models;

namespace GcpvLynx.Views;

public partial class MainWindow : Window
{
    private string? _gcpvFilePath;
    private string? _finishLynxFilePath;
    private List<RaceData> _parsedRaces = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnGcpvFileClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select GCPV Export CSV File",
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("CSV Files") { Patterns = new[] { "*.csv" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            };

            var result = await StorageProvider.OpenFilePickerAsync(options);
            
            if (result.Count > 0)
            {
                _gcpvFilePath = result[0].Path.LocalPath;
                GcpvFileLabel.Text = $"Selected: {_gcpvFilePath}";
                GcpvFileLabel.Foreground = Avalonia.Media.Brushes.Green;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Error", $"Failed to select GCPV file: {ex.Message}");
        }
    }

    private async void OnFinishLynxFileClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select FinishLynx EVT File",
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("EVT Files") { Patterns = new[] { "*.evt" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
                }
            };

            var result = await StorageProvider.OpenFilePickerAsync(options);
            
            if (result.Count > 0)
            {
                _finishLynxFilePath = result[0].Path.LocalPath;
                FinishLynxFileLabel.Text = $"Selected: {_finishLynxFilePath}";
                FinishLynxFileLabel.Foreground = Avalonia.Media.Brushes.Green;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Error", $"Failed to select FinishLynx file: {ex.Message}");
        }
    }

    private async void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        var result = await ShowConfirmDialog("Exit Application", 
            "Are you sure you want to close the application?");
            
        if (result)
        {
            Close();
        }
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        // Simple console output for now
        Console.WriteLine($"ERROR: {title} - {message}");
    }

    private async Task<bool> ShowConfirmDialog(string title, string message)
    {
        // Simple console output for now - always return true
        Console.WriteLine($"CONFIRM: {title} - {message}");
        return true;
    }

    private async void OnProcessClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_gcpvFilePath))
        {
            await ShowErrorDialog("Error", "Please select a GCPV CSV file first.");
            return;
        }

        try
        {
            var parser = new GcpvCsvParser();
            _parsedRaces = parser.ParseFile(_gcpvFilePath);
            
            // Display results
            var message = $"Successfully parsed {_parsedRaces.Count} races:\n\n";
            foreach (var race in _parsedRaces)
            {
                message += $"Race {race.RaceNumber}: {race.RaceParameters} - {race.RaceGroup}\n";
                message += $"  Stage: {race.RaceStage}\n";
                message += $"  Skaters: {race.Skaters.Count}\n";
                foreach (var skater in race.Skaters)
                {
                    message += $"    Lane {skater.Lane}: {skater.FullName} ({skater.Club})\n";
                }
                message += "\n";
            }
            
            await ShowErrorDialog("Parse Results", message);
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Error", $"Failed to parse CSV file: {ex.Message}");
        }
    }
}
