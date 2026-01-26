using MyMauiApp.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMauiApp.Data
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        /// <summary>
        /// Initializes database connection and creates tables
        /// </summary>
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
                // Store on desktop for easy access and backup
                dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "journal_app.db"
                );
#else
                dbPath = Path.Combine(
                    FileSystem.AppDataDirectory,
                    "journal_app.db"
                );
#endif

                _database = new SQLiteAsyncConnection(dbPath);

                // Create tables with proper relationships
                await _database.CreateTableAsync<JournalEntry>();
                await _database.CreateTableAsync<Mood>();
                await _database.CreateTableAsync<Tag>();
                await _database.CreateTableAsync<EntryTag>();

                // Seed initial data if tables are empty
                await SeedInitialData();
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Returns initialized database connection
        /// </summary>
        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            await Init();
            return _database!;
        }

        /// <summary>
        /// Seeds predefined moods and tags on first run
        /// </summary>
        private async Task SeedInitialData()
        {
            var existingMoods = await _database!.Table<Mood>().CountAsync();
            if (existingMoods == 0)
            {
                var moods = new List<Mood>
                {
                    // Positive moods
                    new Mood { Name = "Happy", Sentiment = "Positive", Emoji = "" },
                    new Mood { Name = "Excited", Sentiment = "Positive", Emoji = "" },
                    new Mood { Name = "Relaxed", Sentiment = "Positive", Emoji = "" },
                    new Mood { Name = "Grateful", Sentiment = "Positive", Emoji = "" },
                    new Mood { Name = "Confident", Sentiment = "Positive", Emoji = "" },

                    // Neutral moods
                    new Mood { Name = "Calm", Sentiment = "Neutral", Emoji = "" },
                    new Mood { Name = "Thoughtful", Sentiment = "Neutral", Emoji = "" },
                    new Mood { Name = "Curious", Sentiment = "Neutral", Emoji = "" },
                    new Mood { Name = "Nostalgic", Sentiment = "Neutral", Emoji = "" },
                    new Mood { Name = "Bored", Sentiment = "Neutral", Emoji = "" },

                    // Negative moods
                    new Mood { Name = "Sad", Sentiment = "Negative", Emoji = "" },
                    new Mood { Name = "Angry", Sentiment = "Negative", Emoji = "" },
                    new Mood { Name = "Stressed", Sentiment = "Negative", Emoji = "" },
                    new Mood { Name = "Lonely", Sentiment = "Negative", Emoji = "" },
                    new Mood { Name = "Anxious", Sentiment = "Negative", Emoji = "" }
                };

                await _database.InsertAllAsync(moods);
            }

            var existingTags = await _database.Table<Tag>().CountAsync();
            if (existingTags == 0)
            {
                var tags = new List<Tag>
                {
                    // Predefined tags from requirements
                    new Tag { Name = "Work", IsPredefined = true },
                    //new Tag { Name = "Career", IsPredefined = true },
                    //new Tag { Name = "Studies", IsPredefined = true },
                    new Tag { Name = "Family", IsPredefined = true },
                    new Tag { Name = "Friends", IsPredefined = true },
                    new Tag { Name = "Relationships", IsPredefined = true },
                    new Tag { Name = "Health", IsPredefined = true },
                    new Tag { Name = "Fitness", IsPredefined = true },
                    //new Tag { Name = "Personal Growth", IsPredefined = true },
                    //new Tag { Name = "Self-care", IsPredefined = true },
                    //new Tag { Name = "Hobbies", IsPredefined = true },
                    new Tag { Name = "Travel", IsPredefined = true },
                    new Tag { Name = "Nature", IsPredefined = true },
                //    new Tag { Name = "Finance", IsPredefined = true },
                //    new Tag { Name = "Spirituality", IsPredefined = true },
                //    new Tag { Name = "Birthday", IsPredefined = true },
                //    new Tag { Name = "Holiday", IsPredefined = true },
                //    new Tag { Name = "Vacation", IsPredefined = true },
                //    new Tag { Name = "Celebration", IsPredefined = true },
                //    new Tag { Name = "Exercise", IsPredefined = true },
                //    new Tag { Name = "Reading", IsPredefined = true },
                //    new Tag { Name = "Writing", IsPredefined = true },
                //    new Tag { Name = "Cooking", IsPredefined = true },
                //    new Tag { Name = "Meditation", IsPredefined = true },
                //    new Tag { Name = "Yoga", IsPredefined = true },
                //    new Tag { Name = "Music", IsPredefined = true },
                //    new Tag { Name = "Shopping", IsPredefined = true },
                //    new Tag { Name = "Parenting", IsPredefined = true },
                //    new Tag { Name = "Projects", IsPredefined = true },
                //    new Tag { Name = "Planning", IsPredefined = true },
                //    new Tag { Name = "Reflection", IsPredefined = true }
                };

                await _database.InsertAllAsync(tags);
            }
        }

        /// <summary>
        /// Clears all data (for testing purposes)
        /// </summary>
        public async Task ClearAllDataAsync()
        {
            await Init();
            await _database!.DeleteAllAsync<EntryTag>();
            await _database.DeleteAllAsync<JournalEntry>();
            // Don't delete moods and tags as they're predefined
        }

        /// <summary>
        /// Gets database file path
        /// </summary>
        public string GetDatabasePath()
        {
#if WINDOWS
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "journal_app.db"
            );
#else
            return Path.Combine(
                FileSystem.AppDataDirectory,
                "journal_app.db"
            );
#endif
        }
    }
}
