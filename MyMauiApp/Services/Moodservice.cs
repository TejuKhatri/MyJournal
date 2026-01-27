using MyMauiApp.Data;
using MyMauiApp.Models;

namespace MyMauiApp.Services
{
    public class MoodService
    {
        private readonly DatabaseService _databaseService;

        public MoodService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<Mood>> GetAllMoodsAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Mood>().ToListAsync();
        }

        public async Task<Dictionary<string, List<Mood>>> GetMoodsBySentimentAsync()
        {
            var moods = await GetAllMoodsAsync();

            return moods
                .GroupBy(m => m.Sentiment)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task<Mood?> GetMoodByIdAsync(int id)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Mood>()
                .Where(m => m.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Mood>> GetMoodsBySentimentTypeAsync(string sentiment)
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Mood>()
                .Where(m => m.Sentiment == sentiment)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetMoodUsageStatsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var db = await _databaseService.GetConnectionAsync();

            var query = db.Table<JournalEntry>();

            if (startDate.HasValue)
            {
                query = query.Where(e => e.EntryDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.EntryDate <= endDate.Value.Date);
            }

            var entries = await query.ToListAsync();

            var moodCounts = new Dictionary<int, int>();

            foreach (var entry in entries)
            {
                // Count primary mood
                if (!moodCounts.ContainsKey(entry.PrimaryMoodId))
                    moodCounts[entry.PrimaryMoodId] = 0;
                moodCounts[entry.PrimaryMoodId]++;

                // Count secondary mood 1
                if (entry.SecondaryMood1Id.HasValue)
                {
                    if (!moodCounts.ContainsKey(entry.SecondaryMood1Id.Value))
                        moodCounts[entry.SecondaryMood1Id.Value] = 0;
                    moodCounts[entry.SecondaryMood1Id.Value]++;
                }

                // Count secondary mood 2
                if (entry.SecondaryMood2Id.HasValue)
                {
                    if (!moodCounts.ContainsKey(entry.SecondaryMood2Id.Value))
                        moodCounts[entry.SecondaryMood2Id.Value] = 0;
                    moodCounts[entry.SecondaryMood2Id.Value]++;
                }
            }

           
            var result = new Dictionary<string, int>();
            var allMoods = await GetAllMoodsAsync();

            foreach (var kvp in moodCounts)
            {
                var mood = allMoods.FirstOrDefault(m => m.Id == kvp.Key);
                if (mood != null)
                {
                    result[mood.Name] = kvp.Value;
                }
            }

            return result.OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public async Task<Dictionary<string, int>> GetMoodDistributionBySentimentAsync(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var db = await _databaseService.GetConnectionAsync();

            var query = db.Table<JournalEntry>();

            if (startDate.HasValue)
            {
                query = query.Where(e => e.EntryDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.EntryDate <= endDate.Value.Date);
            }

            var entries = await query.ToListAsync();
            var allMoods = await GetAllMoodsAsync();

            var sentimentCounts = new Dictionary<string, int>
            {
                { "Positive", 0 },
                { "Neutral", 0 },
                { "Negative", 0 }
            };

            foreach (var entry in entries)
            {
                // Primary mood counts fully
                var primaryMood = allMoods.FirstOrDefault(m => m.Id == entry.PrimaryMoodId);
                if (primaryMood != null)
                {
                    sentimentCounts[primaryMood.Sentiment]++;
                }

                // Secondary moods also count
                if (entry.SecondaryMood1Id.HasValue)
                {
                    var secondaryMood1 = allMoods.FirstOrDefault(m => m.Id == entry.SecondaryMood1Id.Value);
                    if (secondaryMood1 != null)
                    {
                        sentimentCounts[secondaryMood1.Sentiment]++;
                    }
                }

                if (entry.SecondaryMood2Id.HasValue)
                {
                    var secondaryMood2 = allMoods.FirstOrDefault(m => m.Id == entry.SecondaryMood2Id.Value);
                    if (secondaryMood2 != null)
                    {
                        sentimentCounts[secondaryMood2.Sentiment]++;
                    }
                }
            }

            return sentimentCounts;
        }

      
        public async Task<(string MoodName, int Count)?> GetMostFrequentMoodAsync(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var moodStats = await GetMoodUsageStatsAsync(startDate, endDate);

            if (!moodStats.Any())
                return null;

            var mostFrequent = moodStats.OrderByDescending(kvp => kvp.Value).First();
            return (mostFrequent.Key, mostFrequent.Value);
        }

        public async Task<Dictionary<DateTime, Dictionary<string, int>>> GetMoodTrendAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var db = await _databaseService.GetConnectionAsync();

            var entries = await db.Table<JournalEntry>()
                .Where(e => e.EntryDate >= startDate.Date && e.EntryDate <= endDate.Date)
                .OrderBy(e => e.EntryDate)
                .ToListAsync();

            var allMoods = await GetAllMoodsAsync();
            var trend = new Dictionary<DateTime, Dictionary<string, int>>();

            foreach (var entry in entries)
            {
                if (!trend.ContainsKey(entry.EntryDate))
                {
                    trend[entry.EntryDate] = new Dictionary<string, int>
                    {
                        { "Positive", 0 },
                        { "Neutral", 0 },
                        { "Negative", 0 }
                    };
                }

                var primaryMood = allMoods.FirstOrDefault(m => m.Id == entry.PrimaryMoodId);
                if (primaryMood != null)
                {
                    trend[entry.EntryDate][primaryMood.Sentiment]++;
                }
            }

            return trend;
        }
    }
}