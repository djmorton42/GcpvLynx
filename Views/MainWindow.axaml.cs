using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Controls.Primitives;
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
    private List<CsvRaceData> _parsedRaces = new();

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

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        // Use a simple message box approach
        var messageBox = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        
        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 10)
        };
        okButton.Click += (s, e) => messageBox.Close();
        
        messageBox.Content = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    Margin = new Avalonia.Thickness(20),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                okButton
            }
        };
        
        await messageBox.ShowDialog(this);
    }

    private async Task<bool> ShowConfirmDialog(string title, string message)
    {
        bool result = false;
        var messageBox = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        
        var yesButton = new Button
        {
            Content = "Yes",
            Margin = new Avalonia.Thickness(0, 0, 10, 0)
        };
        yesButton.Click += (s, e) => { result = true; messageBox.Close(); };
        
        var noButton = new Button
        {
            Content = "No"
        };
        noButton.Click += (s, e) => { result = false; messageBox.Close(); };
        
        messageBox.Content = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    Margin = new Avalonia.Thickness(20),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 10),
                    Children = { yesButton, noButton }
                }
            }
        };
        
        await messageBox.ShowDialog(this);
        return result;
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
            // Create EvtUpdater and update EVT file
            var evtUpdater = new EvtUpdater(_finishLynxFilePath, _parsedRaces, createBackup: true);
            evtUpdater.UpdateEvtFile();
            
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
            
            // Populate race data display
            PopulateRaceDataDisplay();
            
            // Show results section
            ResultsSection.IsVisible = true;
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Error", $"Failed to parse CSV file: {ex.Message}");
        }
    }

    private void PopulateRaceDataDisplay()
    {
        RaceDataPanel.Children.Clear();
        
        if (_parsedRaces.Count == 0)
        {
            var noDataText = new TextBlock
            {
                Text = "No race data available",
                FontSize = 12,
                Foreground = Avalonia.Media.Brushes.Gray,
                Margin = new Avalonia.Thickness(0, 10)
            };
            RaceDataPanel.Children.Add(noDataText);
            return;
        }

        foreach (var race in _parsedRaces)
        {
            // Race header
            var raceHeader = new TextBlock
            {
                Text = $"Race {race.RaceNumber}: {race.GetEventName()}",
                FontSize = 14,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brushes.DarkBlue,
                Margin = new Avalonia.Thickness(0, 10, 0, 5)
            };
            RaceDataPanel.Children.Add(raceHeader);

               // Race details
               var detailsGrid = CreateRaceDetailsGrid(race);
               detailsGrid.Margin = new Avalonia.Thickness(10, 0, 0, 10);
               RaceDataPanel.Children.Add(detailsGrid);

            // Skaters table
            if (race.Skaters.Count > 0)
            {
                var table = CreateSkatersTable(race.Skaters.OrderBy(s => s.Lane).ToList());
                RaceDataPanel.Children.Add(table);
            }

            // Separator
            var separator = new Border
            {
                Height = 1,
                Background = Avalonia.Media.Brushes.LightGray,
                Margin = new Avalonia.Thickness(0, 15, 0, 15)
            };
            RaceDataPanel.Children.Add(separator);
        }
    }

    private Grid CreateRaceDetailsGrid(CsvRaceData race)
    {
        var grid = new Grid();
        
        // Define columns: Label column (fixed width) and Value column (remaining space)
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) }); // Label column
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Value column
        
        // Count how many rows we need
        var rowCount = 4; // Parameters, Group, Stage, Skaters
        if (race.Laps.HasValue) rowCount++;
        
        // Define rows
        for (int i = 0; i < rowCount; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
        }
        
        int currentRow = 0;
        
        // Parameters
        var parametersLabel = new TextBlock
        {
            Text = "Parameters:",
            FontSize = 11,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            Foreground = Avalonia.Media.Brushes.Black,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(parametersLabel, 0);
        Grid.SetRow(parametersLabel, currentRow);
        grid.Children.Add(parametersLabel);
        
        var parametersValue = new TextBlock
        {
            Text = race.RaceParameters,
            FontSize = 11,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            Foreground = Avalonia.Media.Brushes.Black,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(parametersValue, 1);
        Grid.SetRow(parametersValue, currentRow);
        grid.Children.Add(parametersValue);
        currentRow++;
        
        // Group
        var groupLabel = new TextBlock
        {
            Text = "Group:",
            FontSize = 11,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            Foreground = Avalonia.Media.Brushes.Black,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(groupLabel, 0);
        Grid.SetRow(groupLabel, currentRow);
        grid.Children.Add(groupLabel);
        
        var groupValue = new TextBlock
        {
            Text = race.RaceGroup,
            FontSize = 11,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            Foreground = Avalonia.Media.Brushes.Black,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(groupValue, 1);
        Grid.SetRow(groupValue, currentRow);
        grid.Children.Add(groupValue);
        currentRow++;
        
        // Stage
        var stageLabel = new TextBlock
        {
            Text = "Stage:",
            FontSize = 11,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            Foreground = Avalonia.Media.Brushes.Black,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(stageLabel, 0);
        Grid.SetRow(stageLabel, currentRow);
        grid.Children.Add(stageLabel);
        
        var stageValue = new TextBlock
        {
            Text = race.RaceStage,
            FontSize = 11,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            Foreground = Avalonia.Media.Brushes.Black,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(stageValue, 1);
        Grid.SetRow(stageValue, currentRow);
        grid.Children.Add(stageValue);
        currentRow++;
        
        // Laps (if present)
        if (race.Laps.HasValue)
        {
            var lapsLabel = new TextBlock
            {
                Text = "Laps:",
                FontSize = 11,
                FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                Foreground = Avalonia.Media.Brushes.Black,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(lapsLabel, 0);
            Grid.SetRow(lapsLabel, currentRow);
            grid.Children.Add(lapsLabel);
            
            var lapsValue = new TextBlock
            {
                Text = race.Laps.ToString(),
                FontSize = 11,
                FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                Foreground = Avalonia.Media.Brushes.Black,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(lapsValue, 1);
            Grid.SetRow(lapsValue, currentRow);
            grid.Children.Add(lapsValue);
            currentRow++;
        }
        
        // Skaters
        var skatersLabel = new TextBlock
        {
            Text = "Skaters:",
            FontSize = 11,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            Foreground = Avalonia.Media.Brushes.Black,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(skatersLabel, 0);
        Grid.SetRow(skatersLabel, currentRow);
        grid.Children.Add(skatersLabel);
        
        var skatersValue = new TextBlock
        {
            Text = race.Skaters.Count.ToString(),
            FontSize = 11,
            FontFamily = new Avalonia.Media.FontFamily("Consolas"),
            Foreground = Avalonia.Media.Brushes.Black,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(skatersValue, 1);
        Grid.SetRow(skatersValue, currentRow);
        grid.Children.Add(skatersValue);
        
        return grid;
    }

    private Grid CreateSkatersTable(List<CsvSkaterData> skaters)
    {
        var grid = new Grid();
        
        // Define columns
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) }); // Lane
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) }); // ID
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // Last Name
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // First Name
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Club

        // Define rows (1 for header + number of skaters)
        for (int i = 0; i <= skaters.Count; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25) });
        }

        // Headers
        var headers = new[] { "LANE", "ID", "LAST NAME", "FIRST NAME", "CLUB" };
        for (int i = 0; i < headers.Length; i++)
        {
            var header = new TextBlock
            {
                Text = headers[i],
                FontSize = 10,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = Avalonia.Media.Brushes.DarkGray,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(2)
            };
            Grid.SetColumn(header, i);
            Grid.SetRow(header, 0);
            grid.Children.Add(header);
        }

        // Data rows
        for (int row = 0; row < skaters.Count; row++)
        {
            var skater = skaters[row];
            var rowIndex = row + 1;

            var laneText = new TextBlock
            {
                Text = skater.Lane.ToString(),
                FontSize = 10,
                FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Avalonia.Media.Brushes.Black,
                Margin = new Avalonia.Thickness(2)
            };
            Grid.SetColumn(laneText, 0);
            Grid.SetRow(laneText, rowIndex);
            grid.Children.Add(laneText);

            var idText = new TextBlock
            {
                Text = skater.SkaterId,
                FontSize = 10,
                FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Avalonia.Media.Brushes.Black,
                Margin = new Avalonia.Thickness(2)
            };
            Grid.SetColumn(idText, 1);
            Grid.SetRow(idText, rowIndex);
            grid.Children.Add(idText);

            var lastNameText = new TextBlock
            {
                Text = skater.LastName,
                FontSize = 10,
                FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Avalonia.Media.Brushes.Black,
                Margin = new Avalonia.Thickness(2)
            };
            Grid.SetColumn(lastNameText, 2);
            Grid.SetRow(lastNameText, rowIndex);
            grid.Children.Add(lastNameText);

            var firstNameText = new TextBlock
            {
                Text = skater.FirstName,
                FontSize = 10,
                FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Avalonia.Media.Brushes.Black,
                Margin = new Avalonia.Thickness(2)
            };
            Grid.SetColumn(firstNameText, 3);
            Grid.SetRow(firstNameText, rowIndex);
            grid.Children.Add(firstNameText);

            var clubText = new TextBlock
            {
                Text = skater.Club,
                FontSize = 10,
                FontFamily = new Avalonia.Media.FontFamily("Consolas"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Avalonia.Media.Brushes.Black,
                Margin = new Avalonia.Thickness(2)
            };
            Grid.SetColumn(clubText, 4);
            Grid.SetRow(clubText, rowIndex);
            grid.Children.Add(clubText);
        }

        return grid;
    }
}
