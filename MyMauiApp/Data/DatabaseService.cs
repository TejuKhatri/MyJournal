using MyMauiApp.Models;
using SQLite;
using System;

namespace MyMauiApp.Data
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public async Task Init()
        {
            if (_database != null)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_database != null)
                    return;

                string dbPath;

#if WINDOWS
                dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Teju.db"
                );
#else
                dbPath = Path.Combine(
                    FileSystem.AppDataDirectory,
                    "Teju.db"
                );
#endif

                _database = new SQLiteAsyncConnection(dbPath);

                await _database.CreateTableAsync<JournalEntry>();
                await _database.CreateTableAsync<Mood>();
                await _database.CreateTableAsync<Tag>();
                await _database.CreateTableAsync<EntryTag>();

                await SeedInitialData();
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            await Init();
            return _database!;
        }

        private async Task SeedInitialData()
        {
            // MOODS: always insert missing moods
            var moodsToSeed = new List<Mood>
            {
                // Positive
                new Mood { Name = "Happy", Sentiment = "Positive", Emoji = "😊" },
                new Mood { Name = "Excited", Sentiment = "Positive", Emoji = "🤩" },
                new Mood { Name = "Relaxed", Sentiment = "Positive", Emoji = "😌" },
                new Mood { Name = "Grateful", Sentiment = "Positive", Emoji = "🙏" },
                new Mood { Name = "Confident", Sentiment = "Positive", Emoji = "😎" },

                // Neutral
                new Mood { Name = "Calm", Sentiment = "Neutral", Emoji = "😐" },
                new Mood { Name = "Thoughtful", Sentiment = "Neutral", Emoji = "🤔" },
                new Mood { Name = "Curious", Sentiment = "Neutral", Emoji = "🧐" },
                new Mood { Name = "Nostalgic", Sentiment = "Neutral", Emoji = "💭" },
                new Mood { Name = "Bored", Sentiment = "Neutral", Emoji = "🥱" },

                // Negative
                new Mood { Name = "Sad", Sentiment = "Negative", Emoji = "😔" },
                new Mood { Name = "Angry", Sentiment = "Negative", Emoji = "😡" },
                new Mood { Name = "Stressed", Sentiment = "Negative", Emoji = "😫" },
                new Mood { Name = "Lonely", Sentiment = "Negative", Emoji = "😞" },
                new Mood { Name = "Anxious", Sentiment = "Negative", Emoji = "😰" }
            };

            var existingMoods = await _database!.Table<Mood>().ToListAsync();

            foreach (var m in moodsToSeed)
            {
                bool exists = existingMoods.Any(x =>
                    x.Name.Equals(m.Name, StringComparison.OrdinalIgnoreCase) &&
                    x.Sentiment.Equals(m.Sentiment, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    await _database.InsertAsync(m);
                }
            }

            // Tags
            var existingTags = await _database.Table<Tag>().CountAsync();
            if (existingTags == 0)
            {
                var tags = new List<Tag>
                {
                    new Tag { Name = "Work", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Career", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Studies", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Family", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Friends", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Relationships", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Health", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Fitness", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Personal Growth", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Self-care", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Hobbies", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Travel", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Nature", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Finance", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Spirituality", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Birthday", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Holiday", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Vacation", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Celebration", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Exercise", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Reading", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Writing", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Cooking", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Meditation", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Yoga", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Music", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Shopping", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Parenting", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Projects", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Planning", IsPredefined = true, UsageCount = 0 },
                    new Tag { Name = "Reflection", IsPredefined = true, UsageCount = 0 }
                };

                await _database.InsertAllAsync(tags);
            }
        }

        public async Task ClearAllDataAsync()
        {
            await Init();
            await _database!.DeleteAllAsync<EntryTag>();
            await _database.DeleteAllAsync<JournalEntry>();
        }

        public string GetDatabasePath()
        {
#if WINDOWS
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Teju.db"
            );
#else
            return Path.Combine(
                FileSystem.AppDataDirectory,
                "Teju.db"
            );
#endif
        }
    }
}
