# Report Template Customization Guide

## Overview
Fathom OS supports customizable report templates for PDF and Excel exports. You can add your company logo and customize header/footer text with dynamic placeholders.

## Adding Your Company Logo

1. Create a PNG image with your company logo
   - Recommended dimensions: **200x80 pixels**
   - File format: PNG (for quality and transparency support)
   
2. Save the logo as `company_logo.png` in the Assets folder:
   ```
   FathomOS.Modules.SurveyListing/Assets/company_logo.png
   ```

3. The logo will automatically appear in PDF reports and Excel print headers

## Customizing the Template

Edit the `report_template.json` file in the Assets folder:

```json
{
  "Version": 1,
  "Company": {
    "Name": "Your Company Name",
    "LogoFileName": "company_logo.png",
    "Address": "123 Street, City, Country",
    "Phone": "+1 234 567 8900",
    "Email": "info@company.com",
    "Website": "www.company.com"
  },
  "Header": {
    "ShowLogo": true,
    "LogoPosition": "Left",
    "LogoWidth": 80,
    "LogoHeight": 40,
    "Title": "SURVEY LISTING REPORT",
    "ShowCompanyName": true,
    "ShowProjectInfo": true,
    "ShowBorder": true
  },
  "Footer": {
    "LeftText": "{ProjectName} | {ClientName}",
    "CenterText": "Page {PageNumber} of {TotalPages}",
    "RightText": "Generated: {GeneratedDate}",
    "ShowBorder": true
  },
  "Colors": {
    "PrimaryColor": "#1E3A5F",
    "SecondaryColor": "#4A90D9",
    "AccentColor": "#FF6B35",
    "HeaderBackground": "#1E3A5F",
    "HeaderText": "#FFFFFF",
    "TableHeaderBackground": "#4A90D9",
    "TableHeaderText": "#FFFFFF",
    "TableAlternateRow": "#F0F7FF"
  }
}
```

## Available Placeholders

Use these placeholders in header/footer text - they will be replaced with actual values:

| Placeholder | Description |
|-------------|-------------|
| `{ProjectName}` | Project name from user entry |
| `{ClientName}` | Client name |
| `{VesselName}` | Vessel name |
| `{ProcessorName}` | Processor name |
| `{ProductName}` | Product name |
| `{RovName}` | ROV name |
| `{SurveyDate}` | Survey date (yyyy-MM-dd) |
| `{SurveyType}` | Survey type |
| `{CoordinateSystem}` | Coordinate system |
| `{GeneratedDate}` | Report generation date |
| `{GeneratedTime}` | Report generation time |
| `{GeneratedDateTime}` | Full date and time |
| `{PageNumber}` | Current page number |
| `{TotalPages}` | Total number of pages |
| `{Year}` | Current year |
| `{CompanyName}` | Company name from template |

## Color Customization

Colors should be specified in hex format (e.g., `#1E3A5F`):

- **PrimaryColor**: Main brand color (used for titles)
- **SecondaryColor**: Section headers
- **AccentColor**: Highlights and emphasis
- **HeaderBackground**: Page header background
- **HeaderText**: Page header text color
- **TableHeaderBackground**: Data table header background
- **TableHeaderText**: Data table header text
- **TableAlternateRow**: Alternating row color for readability

## Export Options

### PDF Report Options
- **Include Depth Profile Chart**: Adds a visual depth chart showing Z values along KP
- **Include Full Data Table**: Exports all survey points (can generate many pages)

### Additional Export Formats
- **Smoothed Comparison CSV**: Compares original vs smoothed positions with shift distances
- **Interval Points CSV**: Exports interval measure points separately

## File Locations

After building, ensure these files are in the output folder:
```
Assets/
├── company_logo.png      (your logo - add this)
├── report_template.json  (template configuration)
└── icon.png              (application icon)
```

## Tips

1. Keep logo file size small (< 100KB) for faster PDF generation
2. Use a transparent background PNG for best results
3. Test with a small dataset first when enabling "Full Data Table"
4. Back up your customized template before updates
