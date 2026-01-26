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

        /// <summary>
        /// True if predefined, false if custom user tag
        /// </summary>
        public bool IsPredefined { get; set; }

        /// <summary>
        /// Usage count for analytics
        /// </summary>
        public int UsageCount { get; set; }
    }
}
