using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileScanner.Domain.Models
{
    public class FileItem
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public string MimeType { get; set; }
    }
}
