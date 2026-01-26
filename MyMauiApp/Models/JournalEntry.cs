using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMauiApp.Models
{
    [Table("JournalEntries")]
    public class JournalEntry
    {
        [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Markdown or rich-text formatted content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Date of entry (unique per day)
    /// </summary>
    [Unique]
    public DateTime EntryDate { get; set; }

    /// <summary>
    /// System-generated creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// System-generated update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Required primary mood
    /// </summary>
    [Indexed]
    public int PrimaryMoodId { get; set; }

    /// <summary>
    /// Optional secondary mood 1
    /// </summary>
    [Indexed]
    public int? SecondaryMood1Id { get; set; }

    /// <summary>
    /// Optional secondary mood 2
    /// </summary>
    [Indexed]
    public int? SecondaryMood2Id { get; set; }

    /// <summary>
    /// Entry category for organization
    /// </summary>
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Calculated word count for analytics
    /// </summary>
    public int WordCount { get; set; }

    // Navigation properties (not stored in DB)
    [Ignore]
    public Mood? PrimaryMood { get; set; }

    [Ignore]
    public Mood? SecondaryMood1 { get; set; }

    [Ignore]
    public Mood? SecondaryMood2 { get; set; }

    [Ignore]
    public List<Tag> Tags { get; set; } = new();
}
}
