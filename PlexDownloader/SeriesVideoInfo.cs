using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexDownloader
{
    public class SeriesVideoInfo : IDownloadableInfo
    {
        public string Show;
        public int Season;
        public int Episode;
        public string EpisodeName;
    }
}
