using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexDownloader.Models
{
    public class Downloaded
    {
        public long ID { get; set; }
        public long SourceID { get; set; }
        public string VideoTitle { get; set; }
        public string ChannelName { get; set; }
        public string PlaylistName { get; set; }
        public long IndexInPlaylist { get; set; }
        public string FilePath { get; set; }
    }
}
