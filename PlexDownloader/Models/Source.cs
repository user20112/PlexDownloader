using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexDownloader.Models
{
    public class Source
    {
        public long ID { get; set; }
        public string YouTubeURL { get; set; }
        public string Name { get; set; }
        public long SourceType { get; set; }
        public DateTime LastScanned { get; set; }
        public long TotalNumberOfVideos { get; set; }
        public long NumberOfPendingVideos { get; set; }
        public long NumberOfDownloadedVideos { get; set; }

    }
}
