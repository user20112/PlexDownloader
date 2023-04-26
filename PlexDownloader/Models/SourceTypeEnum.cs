using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexDownloader.Models
{
    public enum SourceTypeEnum
    {
        ChannelAsSeries=0,
        ChannelAsIndividuals=1,
        PlaylistAsSeries=2,
        PlaylistAsIndividuals=3,
        IndividualVideo=4,
        Audio=5
    }
}
