// LicensingSystem.LicenseGeneratorUI/Services/LicenseCertificateGenerator.cs
// Premium Certificate-Style License PDF Generator
// Ocean Blue Theme | Decorative Borders | Seal | Small QR Code

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace LicenseGeneratorUI.Services;

public class LicenseCertificateGenerator
{
    public static bool IsAvailable => App.QuestPdfAvailable;

    // ═══════════════════════════════════════════════════════════════════════════
    // OCEAN BLUE THEME COLORS
    // ═══════════════════════════════════════════════════════════════════════════
    private const string OceanPrimary = "#0369A1";    // Deep ocean blue
    private const string OceanLight = "#0EA5E9";      // Sky blue accent
    private const string OceanDark = "#075985";       // Dark blue
    private const string Navy = "#0C4A6E";            // Navy blue
    private const string Gold = "#B8860B";            // Dark gold
    private const string GoldLight = "#D4A84B";       // Light gold
    private const string TextDark = "#1E293B";        // Dark text
    private const string TextMuted = "#64748B";       // Muted text
    private const string BorderLight = "#CBD5E1";     // Light border
    private const string Background = "#F8FAFC";      // Light background

    // ═══════════════════════════════════════════════════════════════════════════
    // LIGHT MODE CERTIFICATE
    // ═══════════════════════════════════════════════════════════════════════════
    public static void GenerateCertificate(string outputPath, LicenseCertificateData data)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("PDF generation is not available.");

        byte[]? qrBytes = GenerateQrCode(data, false);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10).FontColor(TextDark));

                // Outer decorative border
                page.Content().Border(4).BorderColor(OceanPrimary).Padding(4)
                    .Border(1).BorderColor(OceanLight).Padding(2)
                    .Border(1).BorderColor(OceanPrimary).Padding(15)
                    .Column(main =>
                {
                    // ───────────────────────────────────────────────────────────
                    // HEADER
                    // ───────────────────────────────────────────────────────────
                    main.Item().AlignCenter().Column(h =>
                    {
                        h.Item().Text("════════════════════════════════════════════")
                            .FontSize(10).FontColor(OceanLight);
                        
                        h.Item().PaddingTop(12).Text("FATHOM OS")
                            .FontSize(36).Bold().FontColor(OceanPrimary);
                        
                        h.Item().Text("SURVEY PROCESSING SOFTWARE")
                            .FontSize(9).FontColor(TextMuted).LetterSpacing(0.15f);
                        
                        h.Item().PaddingTop(20).PaddingBottom(5)
                            .Text("CERTIFICATE OF LICENSE")
                            .FontSize(20).SemiBold().FontColor(Navy);
                        
                        h.Item().Text("════════════════════════════════════════════")
                            .FontSize(10).FontColor(OceanLight);
                    });

                    main.Item().PaddingVertical(15);

                    // ───────────────────────────────────────────────────────────
                    // CERTIFICATE BODY
                    // ───────────────────────────────────────────────────────────
                    main.Item().AlignCenter().Text("This is to certify that")
                        .FontSize(12).FontColor(TextMuted);
                    
                    main.Item().PaddingTop(8).AlignCenter()
                        .Text(data.CustomerName)
                        .FontSize(28).Bold().FontColor(OceanPrimary);
                    
                    main.Item().PaddingTop(8).AlignCenter().Text(text =>
                    {
                        text.Span("has been granted an authorized license for ").FontColor(TextMuted);
                        text.Span($"Fathom OS {data.Edition}").Bold().FontColor(TextDark);
                    });

                    main.Item().PaddingVertical(15);

                    // ───────────────────────────────────────────────────────────
                    // LICENSE DETAILS BOX
                    // ───────────────────────────────────────────────────────────
                    main.Item().Border(1).BorderColor(BorderLight).Column(box =>
                    {
                        // Header
                        box.Item().Background(OceanPrimary).Padding(10).AlignCenter()
                            .Text("LICENSE DETAILS")
                            .FontSize(11).Bold().FontColor("#FFFFFF");
                        
                        // Content
                        box.Item().Background(Background).Padding(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });

                            AddDetailRow(table, "License ID:", data.LicenseId);
                            AddDetailRow(table, "Email:", data.CustomerEmail);
                            AddDetailRow(table, "Organization:", data.Brand ?? "—");
                            AddDetailRow(table, "Licensee Code:", data.LicenseeCode ?? "—");
                            AddDetailRow(table, "Edition:", data.Edition);
                            AddDetailRow(table, "Subscription:", data.SubscriptionType);
                            AddDetailRow(table, "License Type:", data.LicenseType);
                            AddDetailRow(table, "Issue Date:", data.IssuedAt.ToString("MMMM dd, yyyy"));
                            AddDetailRow(table, "Expiry Date:", data.ExpiresAt.ToString("MMMM dd, yyyy"));
                            AddDetailRow(table, "Status:", GetStatusText(data.DaysRemaining));
                        });
                    });

                    main.Item().PaddingVertical(12);

                    // ───────────────────────────────────────────────────────────
                    // LICENSED MODULES
                    // ───────────────────────────────────────────────────────────
                    main.Item().Border(1).BorderColor(BorderLight).Column(mod =>
                    {
                        mod.Item().Background(OceanDark).Padding(8).AlignCenter()
                            .Text("LICENSED MODULES")
                            .FontSize(10).Bold().FontColor("#FFFFFF");
                        
                        var moduleText = data.Modules.Any() 
                            ? string.Join("   ·   ", data.Modules) 
                            : "All modules included with " + data.Edition + " edition";
                        
                        mod.Item().Background(Background).Padding(12).AlignCenter()
                            .Text(moduleText).FontSize(10).FontColor(TextMuted);
                    });

                    main.Item().PaddingVertical(15);

                    // ───────────────────────────────────────────────────────────
                    // SEAL + SUPPORT CODE + QR CODE ROW
                    // ───────────────────────────────────────────────────────────
                    main.Item().Row(row =>
                    {
                        // SEAL (left)
                        row.RelativeItem().AlignCenter().AlignMiddle().PaddingHorizontal(10).Column(seal =>
                        {
                            seal.Item().AlignCenter().Text("⚓").FontSize(52).FontColor(OceanPrimary);
                            seal.Item().AlignCenter().Text("AUTHORIZED").FontSize(8).Bold()
                                .FontColor(OceanDark).LetterSpacing(0.1f);
                            seal.Item().AlignCenter().Text("LICENSE").FontSize(8).Bold()
                                .FontColor(OceanDark).LetterSpacing(0.1f);
                        });

                        // SUPPORT CODE (center)
                        row.RelativeItem(2).Border(2).BorderColor(Gold).Column(sup =>
                        {
                            sup.Item().Background(Gold).Padding(8).AlignCenter()
                                .Text("SUPPORT CODE")
                                .FontSize(10).Bold().FontColor("#FFFFFF");
                            
                            sup.Item().Background("#FFFBEB").Padding(15).AlignCenter()
                                .Text(data.SupportCode ?? "N/A")
                                .FontSize(26).Bold().FontColor(Gold).FontFamily("Consolas");
                            
                            sup.Item().Background("#FFFBEB").PaddingBottom(10).AlignCenter()
                                .Text("Keep this code for technical support")
                                .FontSize(8).FontColor(TextMuted);
                        });

                        // QR CODE (right - small)
                        if (qrBytes != null)
                        {
                            row.RelativeItem().AlignCenter().AlignMiddle().PaddingHorizontal(10).Column(qr =>
                            {
                                qr.Item().AlignCenter().Width(55).Height(55).Image(qrBytes);
                                qr.Item().PaddingTop(4).AlignCenter()
                                    .Text("Scan to Verify").FontSize(7).FontColor(TextMuted);
                            });
                        }
                    });

                    // ───────────────────────────────────────────────────────────
                    // ACTIVATION INFO (different for online vs offline)
                    // ───────────────────────────────────────────────────────────
                    if (!string.IsNullOrEmpty(data.LicenseKey))
                    {
                        // OFFLINE LICENSE - Show license key and instructions
                        main.Item().PaddingTop(15).Border(1).BorderColor(OceanLight).Column(key =>
                        {
                            key.Item().Background(OceanLight).Padding(8).Row(kr =>
                            {
                                kr.RelativeItem().Text("LICENSE KEY")
                                    .FontSize(10).Bold().FontColor("#FFFFFF");
                                kr.ConstantItem(100).AlignRight()
                                    .Text("OFFLINE LICENSE").FontSize(9).FontColor("#FFFFFF");
                            });
                            
                            key.Item().Background("#F0F9FF").Padding(12).Column(kc =>
                            {
                                kc.Item().AlignCenter()
                                    .Text(data.LicenseKey)
                                    .FontSize(9).FontFamily("Consolas").FontColor(OceanDark);
                                kc.Item().PaddingTop(6).AlignCenter()
                                    .Text("Activation: Fathom OS → Settings → License → Activate Offline → Import this file")
                                    .FontSize(8).FontColor(TextMuted);
                            });
                        });
                    }
                    else
                    {
                        // ONLINE LICENSE - Show automatic activation info
                        main.Item().PaddingTop(15).Border(1).BorderColor("#22C55E").Column(act =>
                        {
                            act.Item().Background("#22C55E").Padding(8).Row(ar =>
                            {
                                ar.RelativeItem().Text("ACTIVATION")
                                    .FontSize(10).Bold().FontColor("#FFFFFF");
                                ar.ConstantItem(100).AlignRight()
                                    .Text("ONLINE LICENSE").FontSize(9).FontColor("#FFFFFF");
                            });
                            
                            act.Item().Background("#F0FDF4").Padding(12).Column(ac =>
                            {
                                ac.Item().AlignCenter()
                                    .Text("✓ Automatic Online Activation")
                                    .FontSize(11).Bold().FontColor("#166534");
                                ac.Item().PaddingTop(6).AlignCenter()
                                    .Text("Enter your License ID in: Fathom OS → Settings → License → Activate Online")
                                    .FontSize(8).FontColor(TextMuted);
                                ac.Item().PaddingTop(4).AlignCenter()
                                    .Text($"License ID: {data.LicenseId}")
                                    .FontSize(9).FontFamily("Consolas").FontColor(OceanDark);
                            });
                        });
                    }

                    main.Item().PaddingTop(20);

                    // ───────────────────────────────────────────────────────────
                    // FOOTER
                    // ───────────────────────────────────────────────────────────
                    main.Item().AlignCenter().Text("════════════════════════════════════════════")
                        .FontSize(10).FontColor(OceanLight);
                    
                    main.Item().PaddingTop(10).Row(f =>
                    {
                        f.RelativeItem().AlignLeft()
                            .Text($"Issued: {DateTime.Now:MMMM dd, yyyy}")
                            .FontSize(8).FontColor(TextMuted);
                        f.RelativeItem().AlignCenter()
                            .Text("www.fathomos.com")
                            .FontSize(8).FontColor(OceanPrimary);
                        f.RelativeItem().AlignRight()
                            .Text("support@fathomos.com")
                            .FontSize(8).FontColor(TextMuted);
                    });
                });
            });
        }).GeneratePdf(outputPath);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DARK MODE CERTIFICATE
    // ═══════════════════════════════════════════════════════════════════════════
    public static void GenerateDarkModeCertificate(string outputPath, LicenseCertificateData data)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("PDF generation is not available.");

        byte[]? qrBytes = GenerateQrCode(data, true);

        const string DarkBg = "#0F172A";
        const string DarkCard = "#1E293B";
        const string DarkBorder = "#334155";
        const string TextLight = "#F1F5F9";
        const string TextDim = "#94A3B8";

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(DarkBg);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10).FontColor(TextLight));

                page.Content().Border(4).BorderColor(OceanPrimary).Padding(4)
                    .Border(1).BorderColor(OceanLight).Padding(2)
                    .Border(1).BorderColor(OceanPrimary).Padding(15)
                    .Column(main =>
                {
                    // Header
                    main.Item().AlignCenter().Column(h =>
                    {
                        h.Item().Text("════════════════════════════════════════════")
                            .FontSize(10).FontColor(OceanLight);
                        
                        h.Item().PaddingTop(12).Text("FATHOM OS")
                            .FontSize(36).Bold().FontColor(OceanLight);
                        
                        h.Item().Text("SURVEY PROCESSING SOFTWARE")
                            .FontSize(9).FontColor(TextDim).LetterSpacing(0.15f);
                        
                        h.Item().PaddingTop(20).PaddingBottom(5)
                            .Text("CERTIFICATE OF LICENSE")
                            .FontSize(20).SemiBold().FontColor(OceanLight);
                        
                        h.Item().Text("════════════════════════════════════════════")
                            .FontSize(10).FontColor(OceanLight);
                    });

                    main.Item().PaddingVertical(15);

                    // Body
                    main.Item().AlignCenter().Text("This is to certify that")
                        .FontSize(12).FontColor(TextDim);
                    
                    main.Item().PaddingTop(8).AlignCenter()
                        .Text(data.CustomerName)
                        .FontSize(28).Bold().FontColor(OceanLight);
                    
                    main.Item().PaddingTop(8).AlignCenter().Text(text =>
                    {
                        text.Span("has been granted an authorized license for ").FontColor(TextDim);
                        text.Span($"Fathom OS {data.Edition}").Bold().FontColor(TextLight);
                    });

                    main.Item().PaddingVertical(15);

                    // Details Box
                    main.Item().Border(1).BorderColor(DarkBorder).Column(box =>
                    {
                        box.Item().Background(OceanPrimary).Padding(10).AlignCenter()
                            .Text("LICENSE DETAILS")
                            .FontSize(11).Bold().FontColor("#FFFFFF");
                        
                        box.Item().Background(DarkCard).Padding(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });

                            AddDarkDetailRow(table, "License ID:", data.LicenseId);
                            AddDarkDetailRow(table, "Email:", data.CustomerEmail);
                            AddDarkDetailRow(table, "Organization:", data.Brand ?? "—");
                            AddDarkDetailRow(table, "Licensee Code:", data.LicenseeCode ?? "—");
                            AddDarkDetailRow(table, "Edition:", data.Edition);
                            AddDarkDetailRow(table, "Subscription:", data.SubscriptionType);
                            AddDarkDetailRow(table, "License Type:", data.LicenseType);
                            AddDarkDetailRow(table, "Issue Date:", data.IssuedAt.ToString("MMM dd, yyyy"));
                            AddDarkDetailRow(table, "Expiry Date:", data.ExpiresAt.ToString("MMM dd, yyyy"));
                            AddDarkDetailRow(table, "Status:", GetStatusText(data.DaysRemaining));
                        });
                    });

                    main.Item().PaddingVertical(12);

                    // Modules
                    main.Item().Border(1).BorderColor(DarkBorder).Column(mod =>
                    {
                        mod.Item().Background(OceanDark).Padding(8).AlignCenter()
                            .Text("LICENSED MODULES")
                            .FontSize(10).Bold().FontColor("#FFFFFF");
                        
                        var moduleText = data.Modules.Any() 
                            ? string.Join("   ·   ", data.Modules) 
                            : "All modules included";
                        
                        mod.Item().Background(DarkCard).Padding(12).AlignCenter()
                            .Text(moduleText).FontSize(10).FontColor(TextDim);
                    });

                    main.Item().PaddingVertical(15);

                    // Seal + Support + QR
                    main.Item().Row(row =>
                    {
                        row.RelativeItem().AlignCenter().AlignMiddle().Column(seal =>
                        {
                            seal.Item().AlignCenter().Text("⚓").FontSize(52).FontColor(OceanLight);
                            seal.Item().AlignCenter().Text("AUTHORIZED").FontSize(8).Bold()
                                .FontColor(OceanLight).LetterSpacing(0.1f);
                        });

                        row.RelativeItem(2).Border(2).BorderColor(Gold).Column(sup =>
                        {
                            sup.Item().Background(Gold).Padding(8).AlignCenter()
                                .Text("SUPPORT CODE")
                                .FontSize(10).Bold().FontColor("#FFFFFF");
                            
                            sup.Item().Background("#292524").Padding(15).AlignCenter()
                                .Text(data.SupportCode ?? "N/A")
                                .FontSize(26).Bold().FontColor(GoldLight).FontFamily("Consolas");
                            
                            sup.Item().Background("#292524").PaddingBottom(10).AlignCenter()
                                .Text("Keep for support")
                                .FontSize(8).FontColor(TextDim);
                        });

                        if (qrBytes != null)
                        {
                            row.RelativeItem().AlignCenter().AlignMiddle().Column(qr =>
                            {
                                qr.Item().AlignCenter().Width(55).Height(55).Image(qrBytes);
                                qr.Item().PaddingTop(4).AlignCenter()
                                    .Text("Verify").FontSize(7).FontColor(TextDim);
                            });
                        }
                    });

                    // Activation Info (different for online vs offline)
                    if (!string.IsNullOrEmpty(data.LicenseKey))
                    {
                        // OFFLINE LICENSE - Show license key
                        main.Item().PaddingTop(15).Border(1).BorderColor(OceanDark).Column(key =>
                        {
                            key.Item().Background(OceanDark).Padding(8).Row(kr =>
                            {
                                kr.RelativeItem().Text("LICENSE KEY")
                                    .FontSize(10).Bold().FontColor("#FFFFFF");
                                kr.ConstantItem(100).AlignRight()
                                    .Text("OFFLINE LICENSE").FontSize(9).FontColor(OceanLight);
                            });
                            
                            key.Item().Background(DarkCard).Padding(12).Column(kc =>
                            {
                                kc.Item().AlignCenter()
                                    .Text(data.LicenseKey)
                                    .FontSize(9).FontFamily("Consolas").FontColor(OceanLight);
                                kc.Item().PaddingTop(6).AlignCenter()
                                    .Text("Activation: Fathom OS → Settings → License → Activate Offline → Import this file")
                                    .FontSize(8).FontColor(TextDim);
                            });
                        });
                    }
                    else
                    {
                        // ONLINE LICENSE - Show automatic activation info
                        main.Item().PaddingTop(15).Border(1).BorderColor("#22C55E").Column(act =>
                        {
                            act.Item().Background("#166534").Padding(8).Row(ar =>
                            {
                                ar.RelativeItem().Text("ACTIVATION")
                                    .FontSize(10).Bold().FontColor("#FFFFFF");
                                ar.ConstantItem(100).AlignRight()
                                    .Text("ONLINE LICENSE").FontSize(9).FontColor("#86EFAC");
                            });
                            
                            act.Item().Background("#14532D").Padding(12).Column(ac =>
                            {
                                ac.Item().AlignCenter()
                                    .Text("✓ Automatic Online Activation")
                                    .FontSize(11).Bold().FontColor("#86EFAC");
                                ac.Item().PaddingTop(6).AlignCenter()
                                    .Text("Enter your License ID in: Fathom OS → Settings → License → Activate Online")
                                    .FontSize(8).FontColor(TextDim);
                                ac.Item().PaddingTop(4).AlignCenter()
                                    .Text($"License ID: {data.LicenseId}")
                                    .FontSize(9).FontFamily("Consolas").FontColor(OceanLight);
                            });
                        });
                    }

                    main.Item().PaddingTop(20);

                    // Footer
                    main.Item().AlignCenter().Text("════════════════════════════════════════════")
                        .FontSize(10).FontColor(OceanLight);
                    
                    main.Item().PaddingTop(10).AlignCenter()
                        .Text("www.fathomos.com")
                        .FontSize(8).FontColor(OceanLight);
                });
            });
        }).GeneratePdf(outputPath);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════════

    private static void AddDetailRow(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingVertical(4).Text(label).FontSize(9).FontColor(TextMuted);
        table.Cell().PaddingVertical(4).Text(value ?? "—").FontSize(9).Bold().FontColor(TextDark);
    }

    private static void AddDarkDetailRow(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingVertical(4).Text(label).FontSize(9).FontColor("#94A3B8");
        table.Cell().PaddingVertical(4).Text(value ?? "—").FontSize(9).Bold().FontColor("#F1F5F9");
    }

    private static string GetStatusText(int daysRemaining)
    {
        if (daysRemaining > 30) return $"Active ({daysRemaining} days remaining)";
        if (daysRemaining > 0) return $"⚠ Expiring soon ({daysRemaining} days)";
        return "⛔ Expired";
    }

    private static byte[]? GenerateQrCode(LicenseCertificateData data, bool darkMode)
    {
        try
        {
            var qrData = $"FATHOM-LICENSE:{data.LicenseId}:{data.SupportCode ?? "X"}:{data.ExpiresAt:yyyyMMdd}";
            
            using var generator = new QRCodeGenerator();
            var qrCodeData = generator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            
            // Smaller QR code (pixel size 8 instead of 20)
            byte[] foreground = darkMode ? new byte[] { 14, 165, 233 } : new byte[] { 3, 105, 161 };
            byte[] background = darkMode ? new byte[] { 30, 41, 59 } : new byte[] { 255, 255, 255 };
            
            return qrCode.GetGraphic(8, foreground, background);
        }
        catch
        {
            return null;
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DATA MODEL
// ═══════════════════════════════════════════════════════════════════════════════
public class LicenseCertificateData
{
    public string LicenseId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string Edition { get; set; } = "Professional";
    public string SubscriptionType { get; set; } = "Yearly";
    public string LicenseType { get; set; } = "Online";
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int DaysRemaining => Math.Max(0, (int)(ExpiresAt - DateTime.Now).TotalDays);
    public string? SupportCode { get; set; }
    public string? LicenseKey { get; set; }
    public List<string> Modules { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public string? QrCodeBase64 { get; set; }
}
