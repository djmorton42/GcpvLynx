using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GcpvLynx.Parsers;
using GcpvLynx.Models;
using GcpvLynx.Services;

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
                
                // Automatically parse the file after selection
                await ParseSelectedFile();
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

        await ParseSelectedFile();
    }

    private async void OnUpdateEvtClicked(object? sender, RoutedEventArgs e)
    {
        // Check if EVT file is selected
        if (string.IsNullOrEmpty(_finishLynxFilePath))
        {
            await ShowErrorDialog("Error", "Please select a FinishLynx EVT file first.");
            return;
        }

        // Check if we have parsed race data
        if (_parsedRaces.Count == 0)
        {
            await ShowErrorDialog("Error", "No race data available. Please parse a CSV file first.");
            return;
        }

        try
        {
            // Create EvtUpdater and append races to EVT file
            var evtUpdater = new EvtUpdater(_finishLynxFilePath, _parsedRaces);
            evtUpdater.AppendRacesToEvt();
            
            await ShowErrorDialog("Success", $"Successfully updated EVT file with {_parsedRaces.Count} races.\nFile: {_finishLynxFilePath}");
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Error", $"Failed to update EVT file: {ex.Message}");
        }
    }

    private async Task ParseSelectedFile()
    {
        if (string.IsNullOrEmpty(_gcpvFilePath))
        {
            return;
        }

        try
        {
            var configService = new ConfigurationService();
            var parser = new GcpvCsvParser(configService);
            _parsedRaces = parser.ParseFile(_gcpvFilePath);
            
            // Populate race selection dropdown
            PopulateRaceDropdown();
            
            // Show results section
            ResultsSection.IsVisible = true;
            
            // Select first race by default
            if (_parsedRaces.Count > 0)
            {
                RaceSelectionComboBox.SelectedIndex = 0;
            }
            
            await ShowErrorDialog("Success", $"Successfully parsed {_parsedRaces.Count} races. Use the dropdown to select a race and view details.");
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Error", $"Failed to parse CSV file: {ex.Message}");
        }
    }

    private void PopulateRaceDropdown()
    {
        RaceSelectionComboBox.Items.Clear();
        
        foreach (var race in _parsedRaces)
        {
            var displayText = $"Race {race.RaceNumber}: {race.RaceParameters} - {race.RaceGroup}";
            RaceSelectionComboBox.Items.Add(new ComboBoxItem 
            { 
                Content = displayText, 
                Tag = race 
            });
        }
    }

    private void OnRaceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (RaceSelectionComboBox.SelectedItem is ComboBoxItem selectedItem && 
            selectedItem.Tag is RaceData selectedRace)
        {
            // Update race information display
            UpdateRaceInfo(selectedRace);
            
            // Update skaters list box
            SkatersListBox.ItemsSource = selectedRace.Skaters;
        }
    }

    private void UpdateRaceInfo(RaceData race)
    {
        var infoText = $"Race {race.RaceNumber}: {race.RaceParameters} - {race.RaceGroup}\n";
        infoText += $"Stage: {race.RaceStage}\n";
        
        if (race.Laps.HasValue)
        {
            infoText += $"Laps: {race.Laps}\n";
        }
        
        infoText += $"Skaters: {race.Skaters.Count}";
        
        RaceInfoText.Text = infoText;
    }
}
