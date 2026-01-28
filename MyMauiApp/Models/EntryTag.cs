using SQLite;


namespace MyMauiApp.Models
{
    [Table("EntryTags")]
    public class EntryTag
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int JournalEntryId { get; set; }

        [Indexed]
        public int TagId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
