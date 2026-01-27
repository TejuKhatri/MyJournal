using MyMauiApp.Data;
using MyMauiApp.Models;
using MyMauiApp.Services;

using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyMauiApp.Services
{
    public class JournalService
    {
        private readonly DatabaseService _databaseService;

    public JournalService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<JournalEntry> CreateEntryAsync(JournalEntry entry)
    {
        var db = await _databaseService.GetConnectionAsync();

        
        entry.EntryDate = entry.EntryDate.Date;

        // Check if entry already exists for this date
        var existing = await GetEntryByDateAsync(entry.EntryDate);
        if (existing != null)
        {
            throw new InvalidOperationException($"An entry already exists for {entry.EntryDate:yyyy-MM-dd}. Please update the existing entry.");
        }

       
        entry.CreatedAt = DateTime.Now;
        entry.UpdatedAt = DateTime.Now;

        // Calculate word count
        entry.WordCount = CalculateWordCount(entry.Content);

        await db.InsertAsync(entry);
        return entry;
    }

    public async Task<JournalEntry> UpdateEntryAsync(JournalEntry entry)
    {
        var db = await _databaseService.GetConnectionAsync();

        var existing = await db.Table<JournalEntry>()
            .Where(e => e.Id == entry.Id)
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            throw new KeyNotFoundException($"Entry with ID {entry.Id} not found.");
        }


        entry.UpdatedAt = DateTime.Now;
        entry.WordCount = CalculateWordCount(entry.Content);
        entry.CreatedAt = existing.CreatedAt; 

        await db.UpdateAsync(entry);
        return entry;
    }

    public async Task DeleteEntryAsync(int entryId)
    {
        var db = await _databaseService.GetConnectionAsync();

        // Delete associated tags first
        await db.Table<EntryTag>()
            .Where(et => et.JournalEntryId == entryId)
            .DeleteAsync();

        // Delete the entry
        var deleted = await db.DeleteAsync<JournalEntry>(entryId);
        if (deleted == 0)
        {
            throw new KeyNotFoundException($"Entry with ID {entryId} not found.");
        }
    }

    public async Task<JournalEntryDto?> GetEntryByIdAsync(int id)
    {
        var db = await _databaseService.GetConnectionAsync();

        var entry = await db.Table<JournalEntry>()
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync();

        if (entry == null)
            return null;

        return await LoadEntryRelationshipsAsync(entry);
    }

    public async Task<JournalEntryDto?> GetEntryByDateAsync(DateTime date)
    {
        var db = await _databaseService.GetConnectionAsync();
        date = date.Date;

        var entry = await db.Table<JournalEntry>()
            .Where(e => e.EntryDate == date)
            .FirstOrDefaultAsync();

        if (entry == null)
            return null;

        return await LoadEntryRelationshipsAsync(entry);
    }

    public async Task<(List<JournalEntryDto> Entries, int TotalCount)> GetEntriesPagedAsync(
        int pageNumber = 1,
        int pageSize = 10,
        bool descending = true)
    {
        var db = await _databaseService.GetConnectionAsync();

        // Get total count
        var totalCount = await db.Table<JournalEntry>().CountAsync();

        // Get paginated entries
        var query = db.Table<JournalEntry>();

        query = descending
            ? query.OrderByDescending(e => e.EntryDate)
            : query.OrderBy(e => e.EntryDate);

        var entries = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Load relationships
        var dtos = new List<JournalEntryDto>();
        foreach (var entry in entries)
        {
            var dto = await LoadEntryRelationshipsAsync(entry);
            dtos.Add(dto);
        }

        return (dtos, totalCount);
    }

    public async Task<List<JournalEntryDto>> SearchEntriesAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return new List<JournalEntryDto>();

        var db = await _databaseService.GetConnectionAsync();
        searchText = searchText.ToLower();

        var entries = await db.Table<JournalEntry>()
            .ToListAsync();

        var filtered = entries
            .Where(e => e.Title.ToLower().Contains(searchText) ||
                       e.Content.ToLower().Contains(searchText))
            .OrderByDescending(e => e.EntryDate)
            .ToList();

        var dtos = new List<JournalEntryDto>();
        foreach (var entry in filtered)
        {
            var dto = await LoadEntryRelationshipsAsync(entry);
            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<List<JournalEntryDto>> FilterEntriesAsync(SearchFilterCriteria criteria)
    {
        var db = await _databaseService.GetConnectionAsync();

        var entries = await db.Table<JournalEntry>().ToListAsync();

        // Apply date range filter
        if (criteria.StartDate.HasValue)
        {
            entries = entries.Where(e => e.EntryDate >= criteria.StartDate.Value.Date).ToList();
        }
        if (criteria.EndDate.HasValue)
        {
            entries = entries.Where(e => e.EntryDate <= criteria.EndDate.Value.Date).ToList();
        }

        // Apply mood filter
        if (criteria.MoodIds.Any())
        {
            entries = entries.Where(e =>
                criteria.MoodIds.Contains(e.PrimaryMoodId) ||
                (e.SecondaryMood1Id.HasValue && criteria.MoodIds.Contains(e.SecondaryMood1Id.Value)) ||
                (e.SecondaryMood2Id.HasValue && criteria.MoodIds.Contains(e.SecondaryMood2Id.Value))
            ).ToList();
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(criteria.Category))
        {
            entries = entries.Where(e =>
                e.Category.Equals(criteria.Category, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        // Apply search text filter
        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var searchLower = criteria.SearchText.ToLower();
            entries = entries.Where(e =>
                e.Title.ToLower().Contains(searchLower) ||
                e.Content.ToLower().Contains(searchLower)
            ).ToList();
        }

        var dtos = new List<JournalEntryDto>();
        foreach (var entry in entries.OrderByDescending(e => e.EntryDate))
        {
            var dto = await LoadEntryRelationshipsAsync(entry);

            // Apply tag filter 
            if (criteria.TagIds.Any())
            {
                var entryTagIds = dto.Tags.Select(t => t.Id).ToList();
                if (criteria.TagIds.Any(tid => entryTagIds.Contains(tid)))
                {
                    dtos.Add(dto);
                }
            }
            else
            {
                dtos.Add(dto);
            }
        }

        return dtos;
    }

    public async Task<List<JournalEntryDto>> GetEntriesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var db = await _databaseService.GetConnectionAsync();

        startDate = startDate.Date;
        endDate = endDate.Date;

        var entries = await db.Table<JournalEntry>()
            .Where(e => e.EntryDate >= startDate && e.EntryDate <= endDate)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync();

        var dtos = new List<JournalEntryDto>();
        foreach (var entry in entries)
        {
            var dto = await LoadEntryRelationshipsAsync(entry);
            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<List<DateTime>> GetEntryDatesAsync()
    {
        var db = await _databaseService.GetConnectionAsync();

        var entries = await db.Table<JournalEntry>()
            .ToListAsync();

        return entries.Select(e => e.EntryDate.Date).Distinct().OrderBy(d => d).ToList();
    }

    private async Task<JournalEntryDto> LoadEntryRelationshipsAsync(JournalEntry entry)
    {
        var db = await _databaseService.GetConnectionAsync();

        var dto = new JournalEntryDto
        {
            Id = entry.Id,
            Title = entry.Title,
            Content = entry.Content,
            EntryDate = entry.EntryDate,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            Category = entry.Category,
            WordCount = entry.WordCount
        };

        // Load moods
        dto.PrimaryMood = await db.Table<Mood>()
            .Where(m => m.Id == entry.PrimaryMoodId)
            .FirstOrDefaultAsync();

        if (entry.SecondaryMood1Id.HasValue)
        {
            dto.SecondaryMood1 = await db.Table<Mood>()
                .Where(m => m.Id == entry.SecondaryMood1Id.Value)
                .FirstOrDefaultAsync();
        }

        if (entry.SecondaryMood2Id.HasValue)
        {
            dto.SecondaryMood2 = await db.Table<Mood>()
                .Where(m => m.Id == entry.SecondaryMood2Id.Value)
                .FirstOrDefaultAsync();
        }

        // Load tags
        var entryTags = await db.Table<EntryTag>()
            .Where(et => et.JournalEntryId == entry.Id)
            .ToListAsync();

        foreach (var et in entryTags)
        {
            var tag = await db.Table<Tag>()
                .Where(t => t.Id == et.TagId)
                .FirstOrDefaultAsync();

            if (tag != null)
            {
                dto.Tags.Add(tag);
            }
        }

        return dto;
    }

    private int CalculateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        // Remove Markdown syntax
        var cleaned = Regex.Replace(content, @"[#*_`~\[\]()>]", " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ");

        var words = cleaned.Split(new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries);

        return words.Length;
    }
}
}