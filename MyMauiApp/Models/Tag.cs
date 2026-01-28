using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMauiApp.Models
{
    [Table("Tags")]
    public class Tag
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(50), Unique]
        public string Name { get; set; } = string.Empty;

        public bool IsPredefined { get; set; }

        public int UsageCount { get; set; }
    }
}
