using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexDownloader.Models
{
    public class Detected
    {
        public long SourceID { get; set; }
        public string ID { get; set; }
        public string VideoTitle { get; set; }
        public string ChannelName { get; set; }
        public string PlaylistName { get; set; }
        public long IndexInPlaylist { get; set; }
    }
}
