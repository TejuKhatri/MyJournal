using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
