#r "nuget: QuestPDF, 2026.9.0"
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;

try {
    QuestPDF.Settings.License = LicenseType.Community;
    QuestPDF.Settings.UseSystemFonts = true;
    QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;

    var document = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
            page.Content().Text("Hello World");
        });
    });

    document.GeneratePdf("test.pdf");
    Console.WriteLine("PDF generated successfully.");
} catch (Exception ex) {
    Console.WriteLine(ex.ToString());
}
