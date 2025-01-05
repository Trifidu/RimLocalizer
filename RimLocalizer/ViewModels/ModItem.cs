using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RimLocalizer.ViewModels
{
    public class ModItem
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? PreviewPath { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
