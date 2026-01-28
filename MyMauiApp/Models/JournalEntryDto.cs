

namespace MyMauiApp.Models
{
    public class JournalEntryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Category { get; set; } = string.Empty;
        public int WordCount { get; set; }

        public Mood? PrimaryMood { get; set; }
        public Mood? SecondaryMood1 { get; set; }
        public Mood? SecondaryMood2 { get; set; }
        public List<Tag> Tags { get; set; } = new();
    }
}
