using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexDownloader.Models
{
    public class Wanted
    {
        public Wanted()
        {

        }
        public Wanted(Detected item)
        {
            ID = item.ID;
            VideoTitle = item.VideoTitle;
            ChannelName = item.ChannelName;
            PlaylistName = item.PlaylistName;
            IndexInPlaylist = item.IndexInPlaylist;
            SourceID = item.SourceID;
        }

        public string ID { get; set; }
        public string VideoTitle { get; set; }
        public string ChannelName { get; set; }
        public string PlaylistName { get; set; }
        public long IndexInPlaylist { get; set; }
        public long SourceID { get; set; }
    }
}
