using MyMauiApp.Data;
using MyMauiApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMauiApp.Services
{
    public class TagService
    {
        private readonly DatabaseService _databaseService;

        public TagService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Gets all tags (predefined and custom)
        /// </summary>
        public async Task<List<Tag>> GetAllTagsAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Tag>()
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Gets only predefined tags
        /// </summary>
        public async Task<List<Tag>> GetPredefinedTagsAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Tag>()
                .Where(t => t.IsPredefined)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Gets only custom user-created tags
        /// </summary>
        public async Task<List<Tag>> GetCustomTagsAsync()
        {
            var db = await _databaseService.GetConnectionAsync();
            return await db.Table<Tag>()
                .Where(t => !t.IsPredefined)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Creates a new custom tag
        /// </summary>
        public async Task<Tag> CreateTagAsync(string tagName)
        {
            var db = await _databaseService.GetConnectionAsync();

            // Check if tag already exists
            var existing = await db.Table<Tag>()
                .Where(t => t.Name.ToLower() == tagName.ToLower())
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return existing;
            }

            var tag = new Tag
            {
                Name = tagName.Trim(),
                IsPredefined = false,
                UsageCount = 0
            };

            await db.InsertAsync(tag);
            return tag;
        }

        /// <summary>
        /// Adds tags to a journal entry
        /// </summary>
        public async Task AddTagsToEntryAsync(int entryId, List<int> tagIds)
        {
            var db = await _databaseService.GetConnectionAsync();

            // Remove existing tags for this entry
            await db.Table<EntryTag>()
                .Where(et => et.JournalEntryId == entryId)
                .DeleteAsync();

            // Add new tags
            foreach (var tagId in tagIds)
            {
                var entryTag = new EntryTag
                {
                    JournalEntryId = entryId,
                    TagId = tagId,
                    CreatedAt = DateTime.Now
                };

                await db.InsertAsync(entryTag);

                // Increment usage count
                var tag = await db.Table<Tag>()
                    .Where(t => t.Id == tagId)
                    .FirstOrDefaultAsync();

                if (tag != null)
                {
                    tag.UsageCount++;
                    await db.UpdateAsync(tag);
                }
            }
        }

        /// <summary>
        /// Gets tags for a specific entry
        /// </summary>
        public async Task<List<Tag>> GetTagsForEntryAsync(int entryId)
        {
            var db = await _databaseService.GetConnectionAsync();

            var entryTags = await db.Table<EntryTag>()
                .Where(et => et.JournalEntryId == entryId)
                .ToListAsync();

            var tags = new List<Tag>();
            foreach (var et in entryTags)
            {
                var tag = await db.Table<Tag>()
                    .Where(t => t.Id == et.TagId)
                    .FirstOrDefaultAsync();

                if (tag != null)
                {
                    tags.Add(tag);
                }
            }

            return tags.OrderBy(t => t.Name).ToList();
        }

        /// <summary>
        /// Gets most used tags
        /// </summary>
        public async Task<Dictionary<string, int>> GetMostUsedTagsAsync(
            int topN = 10,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var db = await _databaseService.GetConnectionAsync();

            // Get entries in date range
            var entriesQuery = db.Table<JournalEntry>();

            if (startDate.HasValue)
            {
                entriesQuery = entriesQuery.Where(e => e.EntryDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                entriesQuery = entriesQuery.Where(e => e.EntryDate <= endDate.Value.Date);
            }

            var entries = await entriesQuery.ToListAsync();
            var entryIds = entries.Select(e => e.Id).ToList();

            if (!entryIds.Any())
                return new Dictionary<string, int>();

            // Get all entry-tag relationships for these entries
            var allEntryTags = await db.Table<EntryTag>().ToListAsync();
            var relevantEntryTags = allEntryTags
                .Where(et => entryIds.Contains(et.JournalEntryId))
                .ToList();

            // Count tag usage
            var tagCounts = relevantEntryTags
                .GroupBy(et => et.TagId)
                .ToDictionary(g => g.Key, g => g.Count());

            // Get tag names
            var allTags = await GetAllTagsAsync();
            var result = new Dictionary<string, int>();

            foreach (var kvp in tagCounts.OrderByDescending(kvp => kvp.Value).Take(topN))
            {
                var tag = allTags.FirstOrDefault(t => t.Id == kvp.Key);
                if (tag != null)
                {
                    result[tag.Name] = kvp.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets tag breakdown (percentage of entries per tag)
        /// </summary>
        public async Task<Dictionary<string, double>> GetTagBreakdownAsync(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var db = await _databaseService.GetConnectionAsync();

            var entriesQuery = db.Table<JournalEntry>();

            if (startDate.HasValue)
            {
                entriesQuery = entriesQuery.Where(e => e.EntryDate >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                entriesQuery = entriesQuery.Where(e => e.EntryDate <= endDate.Value.Date);
            }

            var entries = await entriesQuery.ToListAsync();
            var totalEntries = entries.Count;

            if (totalEntries == 0)
                return new Dictionary<string, double>();

            var entryIds = entries.Select(e => e.Id).ToList();

            // Get all entry-tag relationships
            var allEntryTags = await db.Table<EntryTag>().ToListAsync();
            var relevantEntryTags = allEntryTags
                .Where(et => entryIds.Contains(et.JournalEntryId))
                .ToList();

            // Count unique entries per tag
            var tagEntryCounts = relevantEntryTags
                .GroupBy(et => et.TagId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(et => et.JournalEntryId).Distinct().Count()
                );

            // Calculate percentages
            var allTags = await GetAllTagsAsync();
            var result = new Dictionary<string, double>();

            foreach (var kvp in tagEntryCounts.OrderByDescending(kvp => kvp.Value))
            {
                var tag = allTags.FirstOrDefault(t => t.Id == kvp.Key);
                if (tag != null)
                {
                    double percentage = (kvp.Value * 100.0) / totalEntries;
                    result[tag.Name] = Math.Round(percentage, 1);
                }
            }

            return result;
        }

        /// <summary>
        /// Recalculates usage counts for all tags
        /// </summary>
        public async Task RecalculateTagUsageCountsAsync()
        {
            var db = await _databaseService.GetConnectionAsync();

            var allTags = await GetAllTagsAsync();
            var allEntryTags = await db.Table<EntryTag>().ToListAsync();

            foreach (var tag in allTags)
            {
                tag.UsageCount = allEntryTags.Count(et => et.TagId == tag.Id);
                await db.UpdateAsync(tag);
            }
        }

        /// <summary>
        /// Deletes a custom tag (and removes from all entries)
        /// </summary>
        public async Task DeleteTagAsync(int tagId)
        {
            var db = await _databaseService.GetConnectionAsync();

            var tag = await db.Table<Tag>()
                .Where(t => t.Id == tagId)
                .FirstOrDefaultAsync();

            if (tag == null)
            {
                throw new KeyNotFoundException($"Tag with ID {tagId} not found.");
            }

            if (tag.IsPredefined)
            {
                throw new InvalidOperationException("Cannot delete predefined tags.");
            }

            // Remove from all entries
            await db.Table<EntryTag>()
                .Where(et => et.TagId == tagId)
                .DeleteAsync();

            // Delete tag
            await db.DeleteAsync<Tag>(tagId);
        }

        /// <summary>
        /// Searches tags by name
        /// </summary>
        public async Task<List<Tag>> SearchTagsAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return await GetAllTagsAsync();

            var db = await _databaseService.GetConnectionAsync();
            var allTags = await db.Table<Tag>().ToListAsync();

            return allTags
                .Where(t => t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Name)
                .ToList();
        }
    }

}