using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMauiApp.Models
{
    public class AnalyticsResult
    {
        // Mood analytics
        public Dictionary<string, int> MoodDistribution { get; set; } = new();
        public Dictionary<string, double> MoodPercentages { get; set; } = new();
        public string? MostFrequentMood { get; set; }
        public int MostFrequentMoodCount { get; set; }

        // Streak analytics
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public List<DateTime> MissedDays { get; set; } = new();

        // Tag analytics
        public Dictionary<string, int> MostUsedTags { get; set; } = new();
        public Dictionary<string, double> TagBreakdown { get; set; } = new();

        // Word count analytics
        public Dictionary<DateTime, double> WordCountTrend { get; set; } = new();
        public double AverageWordCount { get; set; }

        // Metadata
        public int TotalEntries { get; set; }
        public DateTime? FirstEntryDate { get; set; }
        public DateTime? LastEntryDate { get; set; }
    }
}
