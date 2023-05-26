using PlexDownloader.Models;
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
        public static async Task<string> DownloadAudio(Wanted info, IProgress<double> progressProvider)
        {
            // Plex Music naming standard
            //Band/Album/TrackNumber - TrackName.mp3
            return await DownloadAudio(info.ID, Path.Combine(MainWindowViewModel.SaveLocation + "\\" + FileSafe(info.ChannelName) + "\\" + FileSafe(info.PlaylistName) + "\\" + info.IndexInPlaylist.ToString() + "-" + FileSafe(info.VideoTitle) + ".mp3"), progressProvider);
        }
        public static async Task<string> DownloadSeriesStyleVideo(Wanted info, IProgress<double> progressProvider)
        {
            // Plex series naming standard
            //ShowName/Season 02/ShowName – s02e17 – Optional_Info.mp4
            return await DownloadVideo(info.ID, Path.Combine(MainWindowViewModel.SaveLocation, FileSafe(info.ChannelName) + "\\" + "Season 01\\" + FileSafe(info.ChannelName) + "-01e" + info.IndexInPlaylist.ToString() + "-" + FileSafe(info.VideoTitle)), progressProvider);
        }
        public static async Task<string> DownloadStandaloneVideo(Wanted info, IProgress<double> progressProvider)
        {
            // Plex series naming standard
            //MovieName/MovieName.ext
            return await DownloadVideo(info.ID, Path.Combine(MainWindowViewModel.SaveLocation + "\\" + FileSafe(info.VideoTitle) + "\\" + FileSafe(info.VideoTitle) + ".mp4"), progressProvider);
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
            if (File.Exists(filePath + ".mp4"))
                return filePath + ".mp4";
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
        public static async Task<Detected[]> GetPlaylistDownloadables(Source source)
        {
            Playlist? playlist = await Client.Playlists.GetAsync(source.YouTubeURL);
            IReadOnlyList<PlaylistVideo>? videos = await Client.Playlists.GetVideosAsync(playlist.Id);
            Detected[] infos = new Detected[videos.Count];
            for (int x = 0; x < videos.Count; x++)
            {
                Detected info = new Detected();
                info.VideoTitle = videos[x].Title;
                info.SourceID = source.ID;
                info.IndexInPlaylist = x + 1;
                info.PlaylistName = playlist.Title;
                info.ChannelName = playlist.Author.ChannelTitle;
                info.ID = videos[x].Id;
                infos[x] = info;
            }
            return infos;
        }
        public static async Task<Detected[]> GetChannelDownloadables(Source source)
        {
            if ((SourceTypeEnum)source.SourceType == SourceTypeEnum.ChannelAsSeries)
            {
                YoutubeExplode.Channels.Channel? channel = null;
                if (source.YouTubeURL.Contains("@"))
                    channel = await Client.Channels.GetByHandleAsync(source.YouTubeURL);
                else
                    channel = await Client.Channels.GetAsync(source.YouTubeURL);
                IReadOnlyList<PlaylistVideo>? videos = await Client.Channels.GetUploadsAsync(channel.Url);
                Detected[] infos = new Detected[videos.Count];
                for (int x = 0; x < videos.Count; x++)
                {
                    Detected info = new Detected();
                    info.SourceID = source.ID;
                    info.VideoTitle = videos[x].Title;
                    info.IndexInPlaylist = x + 1;
                    info.ChannelName = channel.Title;
                    info.ID = videos[x].Id;
                    infos[x] = info;
                }
                return infos;
            }
            else
            {
                YoutubeExplode.Channels.Channel? channel = null;
                if (source.YouTubeURL.Contains("@"))
                    channel = await Client.Channels.GetByHandleAsync(source.YouTubeURL);
                else
                    channel = await Client.Channels.GetAsync(source.YouTubeURL);
                IReadOnlyList<PlaylistVideo>? videos = await Client.Channels.GetUploadsAsync(channel.Url);
                Detected[] infos = new Detected[videos.Count];
                for (int x = 0; x < videos.Count; x++)
                {
                    Detected info = new Detected();
                    info.SourceID = source.ID;
                    info.VideoTitle = videos[x].Title;
                    info.IndexInPlaylist = x + 1;
                    info.ChannelName = channel.Title;
                    info.ID = videos[x].Id;
                    infos[x] = info;
                }
                return infos;
            }
        }

        internal static async Task<string> GetSourceName(Source source)
        {
            switch ((SourceTypeEnum)source.SourceType)
            {
                case SourceTypeEnum.ChannelAsSeries:
                case SourceTypeEnum.ChannelAsIndividuals:
                    return (await Client.Channels.GetAsync(source.YouTubeURL)).Title;
                case SourceTypeEnum.PlaylistAsSeries:
                case SourceTypeEnum.PlaylistAsIndividuals:
                    var playlist = await Client.Playlists.GetAsync(source.YouTubeURL);
                    return playlist.Author.ChannelTitle + ": " + playlist.Title;
                case SourceTypeEnum.IndividualVideo:
                case SourceTypeEnum.Audio:
                    return (await Client.Videos.GetAsync(source.YouTubeURL)).Title;
            }
            return "";
        }

        public static async Task<Detected[]> GetVideoDownloadable(Source source)
        {
            Detected[] infos = new Detected[1];
            Video? video = await Client.Videos.GetAsync(source.YouTubeURL);
            Detected info = new Detected();
            info.SourceID = source.ID;
            info.VideoTitle = video.Title;
            info.IndexInPlaylist = 0;
            info.ChannelName = video.Author.ChannelTitle;
            info.ID = video.Id;
            infos[0] = info;
            return infos;
        }
        public static async Task<Detected[]> GetDownloadables(Source source)
        {
            if ((SourceTypeEnum)source.SourceType == SourceTypeEnum.ChannelAsIndividuals || (SourceTypeEnum)source.SourceType == SourceTypeEnum.ChannelAsSeries)
            {
                return await GetChannelDownloadables(source);
            }
            else if ((SourceTypeEnum)source.SourceType == SourceTypeEnum.PlaylistAsSeries || (SourceTypeEnum)source.SourceType == SourceTypeEnum.PlaylistAsIndividuals)
            {
                return await GetPlaylistDownloadables(source);
            }
            else
            {
                return await GetVideoDownloadable(source);
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