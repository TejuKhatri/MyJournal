using MyMauiApp.Data;
using MyMauiApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMauiApp.Services
{
    public class AnalyticsService
    {
        private readonly DatabaseService _databaseService;
        private readonly MoodService _moodService;
        private readonly TagService _tagService;

        public AnalyticsService(
            DatabaseService databaseService,
            MoodService moodService,
            TagService tagService)
        {
            _databaseService = databaseService;
            _moodService = moodService;
            _tagService = tagService;
        }

        /// <summary>
        /// Gets comprehensive analytics for a date range
        /// </summary>
        public async Task<AnalyticsResult> GetAnalyticsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var db = await _databaseService.GetConnectionAsync();

            // Default to all time if no dates provided
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var allEntries = await db.Table<JournalEntry>().ToListAsync();
                if (allEntries.Any())
                {
                    startDate ??= allEntries.Min(e => e.EntryDate);
                    endDate ??= allEntries.Max(e => e.EntryDate);
                }
                else
                {
                    startDate = DateTime.Today.AddMonths(-1);
                    endDate = DateTime.Today;
                }
            }

            var result = new AnalyticsResult
            {
                FirstEntryDate = startDate,
                LastEntryDate = endDate
            };

            // Get entries in range
            var entries = await db.Table<JournalEntry>()
                .Where(e => e.EntryDate >= startDate.Value.Date && e.EntryDate <= endDate.Value.Date)
                .ToListAsync();

            result.TotalEntries = entries.Count;

            if (entries.Any())
            {
                // Mood analytics
                await CalculateMoodAnalytics(result, startDate.Value, endDate.Value);

                // Streak analytics
                await CalculateStreakAnalytics(result, endDate.Value);

                // Tag analytics
                await CalculateTagAnalytics(result, startDate.Value, endDate.Value);

                // Word count analytics
                CalculateWordCountAnalytics(result, entries);
            }

            return result;
        }

        /// <summary>
        /// Calculates mood distribution and most frequent mood
        /// </summary>
        private async Task CalculateMoodAnalytics(
            AnalyticsResult result,
            DateTime startDate,
            DateTime endDate)
        {
            // Get mood distribution by sentiment
            result.MoodDistribution = await _moodService
                .GetMoodDistributionBySentimentAsync(startDate, endDate);

            // Calculate percentages
            var total = result.MoodDistribution.Values.Sum();
            if (total > 0)
            {
                result.MoodPercentages = result.MoodDistribution
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => Math.Round((kvp.Value * 100.0) / total, 1)
                    );
            }

            // Get most frequent mood
            var mostFrequent = await _moodService
                .GetMostFrequentMoodAsync(startDate, endDate);

            if (mostFrequent.HasValue)
            {
                result.MostFrequentMood = mostFrequent.Value.MoodName;
                result.MostFrequentMoodCount = mostFrequent.Value.Count;
            }
        }

        /// <summary>
        /// Calculates current streak, longest streak, and missed days
        /// </summary>
        private async Task CalculateStreakAnalytics(
            AnalyticsResult result,
            DateTime endDate)
        {
            var db = await _databaseService.GetConnectionAsync();

            var allEntries = await db.Table<JournalEntry>()
                .OrderBy(e => e.EntryDate)
                .ToListAsync();

            if (!allEntries.Any())
            {
                result.CurrentStreak = 0;
                result.LongestStreak = 0;
                return;
            }

            var entryDates = allEntries.Select(e => e.EntryDate.Date).ToHashSet();

            // Calculate current streak (working backwards from endDate)
            result.CurrentStreak = CalculateStreakFromDate(entryDates, endDate.Date);

            // Calculate longest streak
            result.LongestStreak = CalculateLongestStreak(entryDates, allEntries);

            // Calculate missed days (between first and last entry)
            var firstDate = allEntries.Min(e => e.EntryDate).Date;
            var lastDate = allEntries.Max(e => e.EntryDate).Date;

            result.MissedDays = new List<DateTime>();
            for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
            {
                if (!entryDates.Contains(date))
                {
                    result.MissedDays.Add(date);
                }
            }
        }

        /// <summary>
        /// Calculates streak from a specific date going backwards
        /// </summary>
        private int CalculateStreakFromDate(HashSet<DateTime> entryDates, DateTime fromDate)
        {
            int streak = 0;
            var currentDate = fromDate.Date;

            while (entryDates.Contains(currentDate))
            {
                streak++;
                currentDate = currentDate.AddDays(-1);
            }

            return streak;
        }

        /// <summary>
        /// Calculates the longest consecutive streak
        /// </summary>
        private int CalculateLongestStreak(
            HashSet<DateTime> entryDates,
            List<JournalEntry> allEntries)
        {
            if (!allEntries.Any())
                return 0;

            var sortedDates = entryDates.OrderBy(d => d).ToList();
            int longestStreak = 1;
            int currentStreak = 1;

            for (int i = 1; i < sortedDates.Count; i++)
            {
                if ((sortedDates[i] - sortedDates[i - 1]).Days == 1)
                {
                    currentStreak++;
                    longestStreak = Math.Max(longestStreak, currentStreak);
                }
                else
                {
                    currentStreak = 1;
                }
            }

            return longestStreak;
        }

        /// <summary>
        /// Calculates tag usage statistics
        /// </summary>
        private async Task CalculateTagAnalytics(
            AnalyticsResult result,
            DateTime startDate,
            DateTime endDate)
        {
            result.MostUsedTags = await _tagService
                .GetMostUsedTagsAsync(10, startDate, endDate);

            result.TagBreakdown = await _tagService
                .GetTagBreakdownAsync(startDate, endDate);
        }

        /// <summary>
        /// Calculates word count trends over time
        /// </summary>
        private void CalculateWordCountAnalytics(
            AnalyticsResult result,
            List<JournalEntry> entries)
        {
            if (!entries.Any())
                return;

            // Calculate average word count
            result.AverageWordCount = Math.Round(entries.Average(e => e.WordCount), 1);

            // Group by week for trend analysis
            result.WordCountTrend = entries
                .GroupBy(e => GetWeekStart(e.EntryDate))
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Average(e => e.WordCount), 1)
                );
        }

        /// <summary>
        /// Gets the start of the week (Monday) for a given date
        /// </summary>
        private DateTime GetWeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// Gets streak statistics only
        /// </summary>
        public async Task<(int CurrentStreak, int LongestStreak, List<DateTime> MissedDays)>
            GetStreakStatsAsync()
        {
            var db = await _databaseService.GetConnectionAsync();

            var allEntries = await db.Table<JournalEntry>()
                .OrderBy(e => e.EntryDate)
                .ToListAsync();

            if (!allEntries.Any())
            {
                return (0, 0, new List<DateTime>());
            }

            var entryDates = allEntries.Select(e => e.EntryDate.Date).ToHashSet();

            var currentStreak = CalculateStreakFromDate(entryDates, DateTime.Today);
            var longestStreak = CalculateLongestStreak(entryDates, allEntries);

            var firstDate = allEntries.Min(e => e.EntryDate).Date;
            var lastDate = allEntries.Max(e => e.EntryDate).Date;

            var missedDays = new List<DateTime>();
            for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
            {
                if (!entryDates.Contains(date))
                {
                    missedDays.Add(date);
                }
            }

            return (currentStreak, longestStreak, missedDays);
        }

        /// <summary>
        /// Gets mood trend data for chart visualization
        /// </summary>
        public async Task<List<MoodTrendData>> GetMoodTrendDataAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var trend = await _moodService.GetMoodTrendAsync(startDate, endDate);

            return trend.Select(kvp => new MoodTrendData
            {
                Date = kvp.Key,
                Positive = kvp.Value.GetValueOrDefault("Positive", 0),
                Neutral = kvp.Value.GetValueOrDefault("Neutral", 0),
                Negative = kvp.Value.GetValueOrDefault("Negative", 0)
            }).OrderBy(t => t.Date).ToList();
        }

        /// <summary>
        /// Gets summary statistics
        /// </summary>
        public async Task<SummaryStats> GetSummaryStatsAsync()
        {
            var db = await _databaseService.GetConnectionAsync();

            var totalEntries = await db.Table<JournalEntry>().CountAsync();
            var allEntries = await db.Table<JournalEntry>().ToListAsync();

            var stats = new SummaryStats
            {
                TotalEntries = totalEntries
            };

            if (allEntries.Any())
            {
                stats.FirstEntryDate = allEntries.Min(e => e.EntryDate);
                stats.LastEntryDate = allEntries.Max(e => e.EntryDate);
                stats.TotalWords = allEntries.Sum(e => e.WordCount);
                stats.AverageWordsPerEntry = totalEntries > 0
                    ? Math.Round((double)stats.TotalWords / totalEntries, 1)
                    : 0;
            }

            var streakStats = await GetStreakStatsAsync();
            stats.CurrentStreak = streakStats.CurrentStreak;
            stats.LongestStreak = streakStats.LongestStreak;

            return stats;
        }
    }

    // Additional models for analytics

    public class MoodTrendData
    {
        public DateTime Date { get; set; }
        public int Positive { get; set; }
        public int Neutral { get; set; }
        public int Negative { get; set; }
    }

    public class SummaryStats
    {
        public int TotalEntries { get; set; }
        public DateTime? FirstEntryDate { get; set; }
        public DateTime? LastEntryDate { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalWords { get; set; }
        public double AverageWordsPerEntry { get; set; }
    }
}