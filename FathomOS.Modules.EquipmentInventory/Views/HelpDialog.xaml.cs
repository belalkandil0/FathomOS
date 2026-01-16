using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Services;


namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class HelpDialog : MetroWindow
{
    public HelpDialog()
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        // Load initial content
        ShowGettingStarted();
    }
    
    private void HelpNav_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag)
        {
            switch (tag)
            {
                case "GettingStarted": ShowGettingStarted(); break;
                case "Dashboard": ShowDashboard(); break;
                case "Equipment": ShowEquipment(); break;
                case "Manifests": ShowManifests(); break;
                case "Locations": ShowLocations(); break;
                case "Suppliers": ShowSuppliers(); break;
                case "Maintenance": ShowMaintenance(); break;
                case "Certifications": ShowCertifications(); break;
                case "Reports": ShowReports(); break;
                case "Keyboard": ShowKeyboardShortcuts(); break;
            }
        }
    }
    
    private void ClearContent()
    {
        HelpContent.Children.Clear();
    }
    
    private void AddHeading(string text)
    {
        HelpContent.Children.Add(new TextBlock 
        { 
            Text = text, 
            Style = (Style)Resources["HelpHeading"] 
        });
    }
    
    private void AddSubheading(string text)
    {
        HelpContent.Children.Add(new TextBlock 
        { 
            Text = text, 
            Style = (Style)Resources["HelpSubheading"] 
        });
    }
    
    private void AddParagraph(string text)
    {
        HelpContent.Children.Add(new TextBlock 
        { 
            Text = text, 
            Style = (Style)Resources["HelpParagraph"] 
        });
    }
    
    private void AddBullet(string text)
    {
        HelpContent.Children.Add(new TextBlock 
        { 
            Text = "• " + text, 
            Style = (Style)Resources["HelpBullet"] 
        });
    }
    
    private void ShowGettingStarted()
    {
        ClearContent();
        AddHeading("Getting Started");
        
        AddParagraph("Welcome to the S7 Fathom Equipment & Inventory Management Module. This comprehensive system helps you track, manage, and maintain all your equipment across multiple locations, vessels, and projects.");
        
        AddSubheading("First Steps");
        AddBullet("Dashboard: View key metrics, recent activity, and quick actions at a glance");
        AddBullet("Equipment: Add and manage your equipment inventory with detailed tracking");
        AddBullet("Manifests: Create transfer manifests for moving equipment between locations");
        AddBullet("Locations: Set up your bases, vessels, and storage locations");
        
        AddSubheading("Navigation");
        AddParagraph("Use the sidebar on the left to navigate between different sections of the application. The top bar provides quick access to search, notifications, and account settings.");
        
        AddSubheading("Quick Actions");
        AddParagraph("The sidebar contains a '+ New Equipment' button for quickly adding new items. You can also access quick actions from the Dashboard.");
        
        AddSubheading("Theme");
        AddParagraph("Toggle between light and dark themes using the sun/moon toggle in the top right corner. Your preference will be saved automatically.");
    }
    
    private void ShowDashboard()
    {
        ClearContent();
        AddHeading("Dashboard Overview");
        
        AddParagraph("The Dashboard provides a comprehensive overview of your equipment status and recent activities.");
        
        AddSubheading("Statistics Cards");
        AddBullet("Total Equipment: Shows the total count of all equipment items");
        AddBullet("Active Items: Equipment currently in use or deployed");
        AddBullet("Pending Transfers: Manifests awaiting approval or completion");
        AddBullet("Alerts: Items requiring attention (expiring certs, maintenance due)");
        
        AddSubheading("Quick Actions");
        AddBullet("Add Equipment: Create a new equipment record");
        AddBullet("Create Manifest: Start a new inward or outward manifest");
        AddBullet("View Reports: Access reporting and export functions");
        
        AddSubheading("Recent Activity");
        AddParagraph("Shows the latest actions taken in the system, including equipment additions, manifest completions, and status changes.");
        
        AddSubheading("Alerts Section");
        AddParagraph("Displays items that need attention, such as expiring certifications, upcoming calibrations, and low stock warnings.");
    }
    
    private void ShowEquipment()
    {
        ClearContent();
        AddHeading("Managing Equipment");
        
        AddParagraph("The Equipment section is the heart of the inventory system. Here you can add, edit, and track all your equipment.");
        
        AddSubheading("Adding New Equipment");
        AddBullet("Click '+ Add Equipment' button");
        AddBullet("Fill in the required fields (Name, Category, Status)");
        AddBullet("The Asset Number is auto-generated using the format: CAT-LOC-NNNN");
        AddBullet("Add optional details like serial number, manufacturer, certifications");
        
        AddSubheading("Equipment Details");
        AddBullet("Basic Info: Name, category, status, location");
        AddBullet("Identification: Asset number, serial number, barcode");
        AddBullet("Technical: Model, manufacturer, specifications");
        AddBullet("Tracking: Purchase date, warranty, value");
        AddBullet("Certifications: Expiry dates, calibration schedules");
        
        AddSubheading("Searching & Filtering");
        AddParagraph("Use the search bar to find equipment by name, asset number, or serial number. Filter by category, status, or location using the dropdown filters.");
        
        AddSubheading("QR Codes");
        AddParagraph("Generate and print QR code labels for equipment. Scan these labels with the mobile app for quick lookup and updates.");
    }
    
    private void ShowManifests()
    {
        ClearContent();
        AddHeading("Manifests & Transfers");
        
        AddParagraph("Manifests track the movement of equipment between locations. Use them to document transfers, shipments, and mobilizations.");
        
        AddSubheading("Manifest Types");
        AddBullet("Inward Manifest: Equipment arriving at a location");
        AddBullet("Outward Manifest: Equipment leaving a location");
        
        AddSubheading("Creating a Manifest");
        AddBullet("Step 1: Select manifest type and basic details");
        AddBullet("Step 2: Choose source and destination locations");
        AddBullet("Step 3: Add equipment items to the manifest");
        AddBullet("Step 4: Review and submit for approval");
        
        AddSubheading("Manifest Workflow");
        AddBullet("Draft: Initial creation, can be edited");
        AddBullet("Submitted: Awaiting approval");
        AddBullet("Approved: Ready for transfer");
        AddBullet("In Transit: Equipment being moved");
        AddBullet("Completed: Transfer finished, equipment locations updated");
        
        AddSubheading("Tracking");
        AddParagraph("Each manifest has a unique number and full audit trail. Equipment locations are automatically updated when manifests are completed.");
    }
    
    private void ShowLocations()
    {
        ClearContent();
        AddHeading("Locations & Bases");
        
        AddParagraph("Manage all your operational locations including bases, vessels, warehouses, and project sites.");
        
        AddSubheading("Location Types");
        AddBullet("Base: Main operational facility");
        AddBullet("Vessel: Ships and offshore units");
        AddBullet("Warehouse: Storage facilities");
        AddBullet("Project Site: Temporary work locations");
        AddBullet("Workshop: Maintenance and repair facilities");
        
        AddSubheading("Adding Locations");
        AddBullet("Click '+ Add Location' button");
        AddBullet("Enter location name and code");
        AddBullet("Select location type");
        AddBullet("Add address and contact details");
        
        AddSubheading("Location Hierarchy");
        AddParagraph("Locations can be organized in a hierarchy. For example, a warehouse can contain multiple storage areas, and a vessel can have different decks or compartments.");
    }
    
    private void ShowSuppliers()
    {
        ClearContent();
        AddHeading("Supplier Management");
        
        AddParagraph("Manage your equipment suppliers, vendors, and service providers in a centralized directory.");
        
        AddSubheading("Adding Suppliers");
        AddBullet("Click '+ Add Supplier' button");
        AddBullet("Enter supplier name and code");
        AddBullet("Add contact person details");
        AddBullet("Include email, phone, and address");
        
        AddSubheading("Supplier Information");
        AddBullet("Name: Company or vendor name");
        AddBullet("Code: Short identifier for quick reference");
        AddBullet("Contact Person: Primary point of contact");
        AddBullet("Email & Phone: Communication details");
        AddBullet("Address: Physical or mailing address");
        
        AddSubheading("Linking to Equipment");
        AddParagraph("When adding or editing equipment, you can link it to a supplier. This helps track where equipment was purchased and who to contact for support or warranty claims.");
        
        AddSubheading("Supplier Status");
        AddParagraph("Suppliers can be marked as active or inactive. Inactive suppliers won't appear in dropdown lists when adding new equipment but historical records are preserved.");
    }
    
    private void ShowMaintenance()
    {
        ClearContent();
        AddHeading("Maintenance Tracking");
        
        AddParagraph("Track maintenance schedules, service history, and repairs for all your equipment.");
        
        AddSubheading("Maintenance Types");
        AddBullet("Scheduled: Regular preventive maintenance");
        AddBullet("Unscheduled: Repairs and breakdowns");
        AddBullet("Calibration: Instrument calibration");
        AddBullet("Inspection: Safety and condition checks");
        
        AddSubheading("Scheduling");
        AddParagraph("Set up maintenance schedules based on time intervals or usage hours. The system will automatically generate alerts when maintenance is due.");
        
        AddSubheading("Service Records");
        AddParagraph("Record all maintenance activities including work performed, parts used, costs, and technician notes. Build a complete service history for each piece of equipment.");
    }
    
    private void ShowCertifications()
    {
        ClearContent();
        AddHeading("Certifications & Calibrations");
        
        AddParagraph("Track certification expiry dates and calibration requirements to ensure compliance.");
        
        AddSubheading("Certification Tracking");
        AddBullet("Set expiry dates for each certification");
        AddBullet("Receive alerts before certifications expire");
        AddBullet("Upload certification documents");
        AddBullet("Track renewal history");
        
        AddSubheading("Calibration Management");
        AddBullet("Define calibration intervals");
        AddBullet("Record calibration results");
        AddBullet("Track calibration certificates");
        AddBullet("Monitor compliance status");
        
        AddSubheading("Alert Thresholds");
        AddParagraph("Configure when you want to be alerted about expiring certifications. Default is 30 days before expiry, but this can be customized in Settings.");
    }
    
    private void ShowReports()
    {
        ClearContent();
        AddHeading("Reports & Export");
        
        AddParagraph("Generate comprehensive reports and export data in various formats.");
        
        AddSubheading("Available Reports");
        AddBullet("Equipment Inventory: Full listing of all equipment");
        AddBullet("Location Summary: Equipment by location");
        AddBullet("Certification Status: Expiry overview");
        AddBullet("Maintenance Schedule: Upcoming maintenance");
        AddBullet("Transfer History: Manifest activity log");
        
        AddSubheading("Export Formats");
        AddBullet("Excel (.xlsx): Spreadsheet with full details");
        AddBullet("PDF: Formatted reports for printing");
        AddBullet("CSV: Data export for other systems");
        
        AddSubheading("Custom Reports");
        AddParagraph("Use the Report Builder to create custom reports with your selected fields, filters, and groupings.");
    }
    
    private void ShowKeyboardShortcuts()
    {
        ClearContent();
        AddHeading("Keyboard Shortcuts");
        
        AddParagraph("Use these keyboard shortcuts to navigate quickly through the application.");
        
        AddSubheading("Navigation");
        AddBullet("Ctrl+1: Go to Dashboard");
        AddBullet("Ctrl+2: Go to Equipment");
        AddBullet("Ctrl+3: Go to Manifests");
        AddBullet("Ctrl+4: Go to Locations");
        
        AddSubheading("Actions");
        AddBullet("Ctrl+N: New Equipment");
        AddBullet("Ctrl+M: New Manifest");
        AddBullet("Ctrl+F: Focus Search");
        AddBullet("F5: Refresh Data");
        
        AddSubheading("General");
        AddBullet("Ctrl+S: Save (in dialogs)");
        AddBullet("Escape: Close dialog / Cancel");
        AddBullet("F1: Open Help");
    }
}
