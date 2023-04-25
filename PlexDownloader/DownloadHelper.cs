using PlexDownloader.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Converter;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace PlexDownloader
{
    public class DownloadHelper
    {
        private static YoutubeClient Client = new YoutubeClient();
        public static async Task<string> DownloadAudio(string url, AudioInfo info, IProgress<double> progressProvider)
        {
            // Plex Music naming standard
            //Band/Album/TrackNumber - TrackName.mp3
            return await DownloadAudio(url, Path.Combine(MainWindowViewModel.SaveLocation + "\\" + FileSafe(info.Artist) + "\\" + FileSafe(info.Album) + "\\" + info.TrackNumber.ToString() + "-" + FileSafe(info.TrackName) + ".mp3"), progressProvider);
        }
        public static async Task<string> DownloadSeriesStyleVideo(string url, SeriesVideoInfo info, IProgress<double> progressProvider)
        {
            // Plex series naming standard
            //ShowName/Season 02/ShowName – s02e17 – Optional_Info.mp4
            return await DownloadVideo(url, Path.Combine(MainWindowViewModel.SaveLocation, FileSafe(info.Show) + "\\" + "Season " + info.Season.ToString() + "\\" + FileSafe(info.Show) + "-" + info.Season.ToString() + "e" + info.Episode.ToString() + "-" + FileSafe(info.EpisodeName)), progressProvider);
        }
        public static async Task<string> DownloadStandaloneVideo(string url, string name, IProgress<double> progressProvider)
        {
            // Plex series naming standard
            //MovieName/MovieName.ext
            return await DownloadVideo(url, Path.Combine(MainWindowViewModel.SaveLocation + "\\" + FileSafe(name) + "\\" + FileSafe(name) + ".mp4"), progressProvider);
        }
        public static async Task<string> DownloadAudio(string url, string filePath, IProgress<double> progressProvider)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            StreamManifest? streamManifest = await Client.Videos.Streams.GetManifestAsync(url);
            IStreamInfo? streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            string name = Path.Combine(Path.GetFileNameWithoutExtension(filePath) + ".mp3");
            await Client.Videos.Streams.DownloadAsync(streamInfo, name, progressProvider);
            return name;
        }
        public static async Task<string> DownloadVideo(string url, string filePath, IProgress<double> progressProvider)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await Client.Videos.DownloadAsync(url, filePath + ".mp4", progressProvider);
            return filePath + ".mp4";
        }

        public static byte[] DownloadThumbnail(Video video)
        {
            string url = video.Thumbnails[0].Url;
            using (WebClient client = new WebClient())
            {
                return client.DownloadData(new Uri(url));
            }
        }

        public static async Task<bool> IsChannel(string url)
        {
            IReadOnlyList<PlaylistVideo>? videos = new List<PlaylistVideo>();
            try
            {
                YoutubeExplode.Channels.Channel? channel = null;
                if (url.Contains("@"))
                    channel = await Client.Channels.GetByHandleAsync(url);
                else
                    channel = await Client.Channels.GetAsync(url);
                videos = await Client.Channels.GetUploadsAsync(channel.Url);
                return true;
            }
            catch (Exception ex2)
            {
                // its niether so it must be a normal video
            }
            return false;
        }
        public static async Task<bool> IsVideo(string url)
        {
            IReadOnlyList<PlaylistVideo>? videos = new List<PlaylistVideo>();
            try
            {
                Playlist? playlist = await Client.Playlists.GetAsync(url);
                videos = await Client.Playlists.GetVideosAsync(playlist.Id);
                return false;
            }
            catch
            {
                // if its not a playlist, check if its a channel
                try
                {
                    videos = await Client.Channels.GetUploadsAsync(url);
                    return false;
                }
                catch
                {
                    // its niether so it must be a normal video
                }
            }
            return true;
        }
        public static async Task<bool> IsPlaylist(string url)
        {
            IReadOnlyList<PlaylistVideo>? videos = new List<PlaylistVideo>();
            try
            {
                Playlist? playlist = await Client.Playlists.GetAsync(url);
                if (playlist != null)
                    return true;
            }
            catch
            {
            }
            return false;
        }
        public static async Task<IDownloadableInfo[]> GetPlaylistDownloadables(string url)
        {
            int seasonValue = 1;
            Playlist? playlist = await Client.Playlists.GetAsync(url);
            IReadOnlyList<PlaylistVideo>? videos = await Client.Playlists.GetVideosAsync(playlist.Id);
            IDownloadableInfo[] infos = new IDownloadableInfo[videos.Count];
            for (int x = 0; x < videos.Count; x++)
            {
                SeriesVideoInfo info = new SeriesVideoInfo();
                info.EpisodeName = videos[x].Title;
                info.Episode = x + 1;
                info.Season = seasonValue;
                info.Show = playlist.Author.ChannelTitle + " " + playlist.Title;
                info.URL = videos[x].Url;
                infos[x] = info;
            }
            return infos;
        }
        public static async Task<IDownloadableInfo[]> GetChannelDownloadables(string url, bool treatAsSeries)
        {
            if (treatAsSeries)
            {
                int seasonValue = 1;
                YoutubeExplode.Channels.Channel? channel = null;
                if (url.Contains("@"))
                    channel = await Client.Channels.GetByHandleAsync(url);
                else
                    channel = await Client.Channels.GetAsync(url);
                IReadOnlyList<PlaylistVideo>? videos = await Client.Channels.GetUploadsAsync(channel.Url);
                IDownloadableInfo[] infos = new IDownloadableInfo[videos.Count];
                for (int x = 0; x < videos.Count; x++)
                {
                    SeriesVideoInfo info = new SeriesVideoInfo();
                    info.EpisodeName = videos[x].Title;
                    info.Episode = x + 1;
                    info.Season = seasonValue;
                    info.Show = channel.Title;
                    info.URL = videos[x].Url;
                    infos[x] = info;
                }
                return infos;
            }
            else
            {
                YoutubeExplode.Channels.Channel? channel = null;
                if (url.Contains("@"))
                    channel = await Client.Channels.GetByHandleAsync(url);
                else
                    channel = await Client.Channels.GetAsync(url);
                IReadOnlyList<PlaylistVideo>? videos = await Client.Channels.GetUploadsAsync(channel.Url);
                IDownloadableInfo[] infos = new IDownloadableInfo[videos.Count];
                for (int x = 0; x < videos.Count; x++)
                {
                    MovieVideoInfo info = new MovieVideoInfo();
                    info.Name = videos[x].Title;
                    info.URL = videos[x].Url;
                    infos[x] = info;
                }
                return infos;
            }
        }
        public static async Task<IDownloadableInfo[]> GetVideoDownloadable(string url)
        {
            IDownloadableInfo[] infos = new IDownloadableInfo[1];
            MovieVideoInfo? info = new MovieVideoInfo();
            info.URL = url;
            Video? video = await Client.Videos.GetAsync(url);
            info.Name = video.Title;
            infos[0] = info;
            return infos;
        }
        public static async Task<IDownloadableInfo[]> GetDownloadables(string url, bool treatChannelAsSeries)
        {
            if (await IsChannel(url))
            {
                return await GetChannelDownloadables(url, treatChannelAsSeries);
            }
            else if (await IsPlaylist(url))
            {
                return await GetPlaylistDownloadables(url);
            }
            else
            {
                return await GetVideoDownloadable(url);
            }
        }

        public static async Task<Video> GetVideo(string URL)
        {
            return await Client.Videos.GetAsync(URL);
        }

        private static string FileSafe(string input)
        {
            List<string> invalids = new List<string>() { "\\", "/", ":", "*", "?", "\"", "'", "<", ">", "|", "\0" };
            foreach (string? value in invalids)
            {
                input = input.Replace(value, "");
            }
            return input;
        }
    }
}