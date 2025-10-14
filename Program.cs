using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GcpvLynx.Views;
using GcpvLynx.Services;
using System.IO;

namespace GcpvLynx;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            
            // Set the application title with version if available
            var appTitle = GetApplicationTitle();
            mainWindow.Title = appTitle;
            
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string GetApplicationTitle()
    {
        const string baseTitle = "GcpvLynx";
        const string versionFileName = "VERSION.txt";
        
        try
        {
            // Look for VERSION.txt in the application directory
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var versionFilePath = Path.Combine(appDirectory, versionFileName);
            
            if (File.Exists(versionFilePath))
            {
                var version = File.ReadAllText(versionFilePath).Trim();
                if (!string.IsNullOrEmpty(version))
                {
                    return $"{baseTitle} v{version}";
                }
            }
        }
        catch
        {
            // If there's any error reading the version file, just use the base title
        }
        
        return baseTitle;
    }
}

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Display startup information
        DisplayStartupInfo();
        
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void DisplayStartupInfo()
    {
        try
        {
            var configService = new ConfigurationService();
            var config = configService.GetConfiguration();
            
            Console.WriteLine("🏁 GcpvLynx Starting...");
            Console.WriteLine($"📝 Output Encoding: {config.OutputEncoding.ToUpperInvariant()}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            // If there's an error loading config, the validation will handle it
            Console.WriteLine("🏁 GcpvLynx Starting...");
            Console.WriteLine("⚠️  Warning: Could not load configuration");
            Console.WriteLine();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
