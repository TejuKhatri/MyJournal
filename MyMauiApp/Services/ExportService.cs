using MyMauiApp.Data;
using MyMauiApp.Models;
using MyMauiApp.Services;   
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestContainer = QuestPDF.Infrastructure.IContainer;
using QuestColors = QuestPDF.Helpers.Colors;

namespace MyMauiApp.Services
{
    /// <summary>
    /// Handles exporting journal entries to PDF format
    /// Uses QuestPDF library for PDF generation
    /// </summary>
    public class ExportService
    {
        private readonly DatabaseService _databaseService;
        private readonly JournalService _journalService;

        public ExportService(
            DatabaseService databaseService,
            JournalService journalService)
        {
            _databaseService = databaseService;
            _journalService = journalService;
        }

        /// <summary>
        /// Exports journal entries within a date range to PDF
        /// </summary>
        public async Task<string> ExportToPdfAsync(
            DateTime startDate,
            DateTime endDate,
            string? outputPath = null)
        {
            // Set QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;

            // Get entries in date range
            var entries = await _journalService
                .GetEntriesByDateRangeAsync(startDate, endDate);

            if (!entries.Any())
            {
                throw new InvalidOperationException("No entries found in the specified date range.");
            }

            // Generate output path if not provided
            if (string.IsNullOrEmpty(outputPath))
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var fileName = $"Journal_Export_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                outputPath = Path.Combine(desktopPath, fileName);
            }

            // Generate PDF
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);

                    // Header
                    page.Header().Element(c => ComposeHeader(c));

                    // Content
                    page.Content().Element(content => ComposeContent(content, entries, startDate, endDate));

                    // Footer
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf(outputPath);

            return outputPath;
        }

        /// <summary>
        /// Composes the PDF header
        /// </summary>
        private void ComposeHeader(QuestContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("My Journal").FontSize(20).Bold();
                    column.Item().Text($"Export Date: {DateTime.Now:MMMM dd, yyyy}").FontSize(10);
                });
            });
        }

        /// <summary>
        /// Composes the PDF content with all entries
        /// </summary>
        private void ComposeContent(
            QuestContainer container,
            List<JournalEntryDto> entries,
            DateTime startDate,
            DateTime endDate)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(10);

                // Summary section
                column.Item().Text($"Journal Entries: {startDate:MMMM dd, yyyy} - {endDate:MMMM dd, yyyy}")
                    .FontSize(14).Bold();

                column.Item().Text($"Total Entries: {entries.Count}")
                    .FontSize(10);

                column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(QuestColors.Grey.Lighten2);

                // Entry sections
                foreach (var entry in entries)
                {
                    column.Item().Element(c => ComposeEntry(c, entry));
                    column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(QuestColors.Grey.Lighten3);
                }
            });
        }

        /// <summary>
        /// Composes a single entry in the PDF
        /// </summary>
        /// <summary>
        /// Composes a single entry in the PDF
        /// </summary>
        private void ComposeEntry(QuestContainer container, JournalEntryDto entry)
        {
            container.Column(column =>
            {
                column.Spacing(5);

                // Date and Title
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(entry.EntryDate.ToString("dddd, MMMM dd, yyyy"))
                        .FontSize(12).Bold();
                });

                if (!string.IsNullOrWhiteSpace(entry.Title))
                {
                    column.Item().Text(entry.Title).FontSize(11).Italic();
                }

                // Moods
                column.Item().Row(row =>
                {
                    var moodText = $"Mood: {entry.PrimaryMood?.Emoji} {entry.PrimaryMood?.Name}";

                    if (entry.SecondaryMood1 != null)
                    {
                        moodText += $", {entry.SecondaryMood1.Emoji} {entry.SecondaryMood1.Name}";
                    }

                    if (entry.SecondaryMood2 != null)
                    {
                        moodText += $", {entry.SecondaryMood2.Emoji} {entry.SecondaryMood2.Name}";
                    }

                    row.RelativeItem().Text(moodText).FontSize(9).FontColor(QuestColors.Grey.Darken1);
                });

                // Tags
                if (entry.Tags.Any())
                {
                    var tagsText = "Tags: " + string.Join(", ", entry.Tags.Select(t => t.Name));
                    column.Item().Text(tagsText).FontSize(9).FontColor(QuestColors.Grey.Darken1);
                }

                // Category
                if (!string.IsNullOrWhiteSpace(entry.Category))
                {
                    column.Item().Text($"Category: {entry.Category}")
                        .FontSize(9).FontColor(QuestColors.Grey.Darken1);
                }

                // Content
                column.Item().PaddingTop(5).Text(FormatContentForPdf(entry.Content))
                    .FontSize(10).LineHeight(1.4f);

                // Metadata - THIS IS THE CORRECTED SECTION
                column.Item().PaddingTop(5).Text(text =>
                {
                    // Styles are applied to the 'text' descriptor inside the lambda
                    text.Span($"Word Count: {entry.WordCount} | ").FontSize(8).FontColor(QuestColors.Grey.Medium);
                    text.Span($"Created: {entry.CreatedAt:MMM dd, yyyy h:mm tt}")
                        .FontSize(8).FontColor(QuestColors.Grey.Medium);

                    if (entry.UpdatedAt != entry.CreatedAt)
                    {
                        text.Span($" | Updated: {entry.UpdatedAt:MMM dd, yyyy h:mm tt}")
                            .FontSize(8).FontColor(QuestColors.Grey.Medium);
                    }
                });
            });
        }

        /// <summary>
        /// Formats content for PDF (strips Markdown for basic rendering)
        /// </summary>
        private string FormatContentForPdf(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // Basic Markdown removal for PDF
            // In production, you might want to use a Markdown parser
            content = System.Text.RegularExpressions.Regex.Replace(content, @"^#+\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            content = content.Replace("**", "").Replace("__", "");
            content = content.Replace("*", "").Replace("_", "");
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\[([^\]]+)\]\([^\)]+\)", "$1");

            return content;
        }

        /// <summary>
        /// Exports a single entry to PDF
        /// </summary>
        public async Task<string> ExportSingleEntryToPdfAsync(int entryId, string? outputPath = null)
        {
            var entry = await _journalService.GetEntryByIdAsync(entryId);

            if (entry == null)
            {
                throw new KeyNotFoundException($"Entry with ID {entryId} not found.");
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var fileName = $"Journal_Entry_{entry.EntryDate:yyyyMMdd}.pdf";
                outputPath = Path.Combine(desktopPath, fileName);
            }

            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);

                    page.Header().Element(c => ComposeHeader(c));

                    page.Content().Element(content =>
                        ComposeContent(content, new List<JournalEntryDto> { entry },
                        entry.EntryDate, entry.EntryDate));

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.CurrentPageNumber();
                    });
                });
            }).GeneratePdf(outputPath);

            return outputPath;
        }

        /// <summary>
        /// Gets export statistics
        /// </summary>
        public async Task<ExportStats> GetExportStatsAsync(DateTime startDate, DateTime endDate)
        {
            var entries = await _journalService.GetEntriesByDateRangeAsync(startDate, endDate);

            return new ExportStats
            {
                TotalEntries = entries.Count,
                TotalWords = entries.Sum(e => e.WordCount),
                DateRange = $"{startDate:MMM dd, yyyy} - {endDate:MMM dd, yyyy}",
                FirstEntryDate = entries.Any() ? entries.Min(e => e.EntryDate) : (DateTime?)null,
                LastEntryDate = entries.Any() ? entries.Max(e => e.EntryDate) : (DateTime?)null
            };
        }
    }

    /// <summary>
    /// Export statistics model
    /// </summary>
    public class ExportStats
    {
        public int TotalEntries { get; set; }
        public int TotalWords { get; set; }
        public string DateRange { get; set; } = string.Empty;
        public DateTime? FirstEntryDate { get; set; }
        public DateTime? LastEntryDate { get; set; }
    }
}