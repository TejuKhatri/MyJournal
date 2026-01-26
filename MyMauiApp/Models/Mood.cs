using SQLite;


namespace MyMauiApp.Models
{
    [Table("Moods")]
    public class Mood
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(50), Unique]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Sentiment: Positive, Neutral, Negative
        /// </summary>
        [MaxLength(20), Indexed]
        public string Sentiment { get; set; } = string.Empty;

        /// <summary>
        /// Emoji representation
        /// </summary>
        [MaxLength(10)]
        public string Emoji { get; set; } = string.Empty;
    }
}
