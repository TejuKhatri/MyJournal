using SQLite;


namespace MyMauiApp.Models
{
    [Table("JournalEntries")]
    public class JournalEntry
    {
        [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    [Unique]
    public DateTime EntryDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [Indexed]
    public int PrimaryMoodId { get; set; }

    [Indexed]
    public int? SecondaryMood1Id { get; set; }

    [Indexed]
    public int? SecondaryMood2Id { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public int WordCount { get; set; }

    
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
