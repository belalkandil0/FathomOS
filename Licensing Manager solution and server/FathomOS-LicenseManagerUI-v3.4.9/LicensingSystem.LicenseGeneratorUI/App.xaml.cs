using System.Windows;
using System.Windows.Threading;

namespace LicenseGeneratorUI;

public partial class App : Application
{
    // Static flag to track if QuestPDF is available
    public static bool QuestPdfAvailable { get; private set; } = false;
    public static string? QuestPdfError { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Set up global exception handling FIRST
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"Fatal error: {ex?.Message}\n\n{ex?.StackTrace}", 
                "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Error: {args.Exception.Message}\n\n{args.Exception.StackTrace}", 
                "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // Initialize QuestPDF
        InitializeQuestPdf();

        // Show warning if QuestPDF failed (non-blocking)
        if (!QuestPdfAvailable)
        {
            var message = "PDF certificate generation is unavailable.\n\n" +
                "The application will work normally, but you won't be able to generate PDF certificates.\n\n";
            
            if (!string.IsNullOrEmpty(QuestPdfError))
            {
                message += $"Error: {QuestPdfError}\n\n";
            }
            
            message += "To fix this:\n" +
                "1. Close Visual Studio completely\n" +
                "2. Delete the bin and obj folders in LicensingSystem.LicenseGeneratorUI\n" +
                "3. Reopen the solution\n" +
                "4. Right-click Solution → Restore NuGet Packages\n" +
                "5. Build → Clean Solution, then Build → Rebuild Solution";
            
            MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        
        base.OnStartup(e);
    }

    private static void InitializeQuestPdf()
    {
        try
        {
            // Configure QuestPDF license
            // Community license is free for revenue < $1M USD annually
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            
            // Test that QuestPDF can actually work by accessing a basic type
            var _ = QuestPDF.Helpers.PageSizes.A4;
            
            QuestPdfAvailable = true;
            QuestPdfError = null;
        }
        catch (TypeInitializationException ex)
        {
            // This usually means SkiaSharp native binaries are missing
            QuestPdfError = $"Native library error: {ex.InnerException?.Message ?? ex.Message}";
            QuestPdfAvailable = false;
        }
        catch (DllNotFoundException ex)
        {
            QuestPdfError = $"Missing DLL: {ex.Message}";
            QuestPdfAvailable = false;
        }
        catch (BadImageFormatException ex)
        {
            QuestPdfError = $"Architecture mismatch (32/64-bit): {ex.Message}";
            QuestPdfAvailable = false;
        }
        catch (Exception ex)
        {
            QuestPdfError = ex.Message;
            QuestPdfAvailable = false;
        }
    }
}
