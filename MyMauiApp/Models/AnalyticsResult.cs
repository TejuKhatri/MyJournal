

namespace MyMauiApp.Models
{
    public class AnalyticsResult
    {
  
        public Dictionary<string, int> MoodDistribution { get; set; } = new();
        public Dictionary<string, double> MoodPercentages { get; set; } = new();
        public string? MostFrequentMood { get; set; }
        public int MostFrequentMoodCount { get; set; }

        
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public List<DateTime> MissedDays { get; set; } = new();


        public Dictionary<string, int> MostUsedTags { get; set; } = new();
        public Dictionary<string, double> TagBreakdown { get; set; } = new();

        
        public Dictionary<DateTime, double> WordCountTrend { get; set; } = new();
        public double AverageWordCount { get; set; }

        public int TotalEntries { get; set; }
        public DateTime? FirstEntryDate { get; set; }
        public DateTime? LastEntryDate { get; set; }
    }
}
