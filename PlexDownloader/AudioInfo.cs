using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexDownloader
{
    public class AudioInfo:IDownloadableInfo
    {
        public string Artist;
        public string Album;
        public int TrackNumber; 
        public string TrackName;
    }
}
