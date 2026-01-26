using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMauiApp.Models
{
    public class SearchFilterCriteria
    {
        public string? SearchText { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<int> MoodIds { get; set; } = new();
        public List<int> TagIds { get; set; } = new();
        public string? Category { get; set; }
    }
}
