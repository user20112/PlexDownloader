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

        public static async Task<string> DownloadAudio(string URL, string OutputFolder, IProgress<double> progressProvider)
        {
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);
            // You can specify both video ID or URL
            Video? video = await Client.Videos.GetAsync(URL);
            StreamManifest? streamManifest = await Client.Videos.Streams.GetManifestAsync(URL);
            IStreamInfo? streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            Stream? stream = await Client.Videos.Streams.GetAsync(streamInfo);
            OutputFolder = Path.Combine(OutputFolder, "Audio", FileSafe(video.Author.ChannelTitle));
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);
            string name = Path.Combine(OutputFolder, FileSafe(video.Title) + ".mp3");
            await Client.Videos.Streams.DownloadAsync(streamInfo, name, progressProvider);
            return name;
        }

        public static byte[] DownloadThumbnail(Video video)
        {
            string url = video.Thumbnails[0].Url;
            using (WebClient client = new WebClient())
            {
                return client.DownloadData(new Uri(url));
            }
        }

        public static async Task<string> DownloadVideo(string URL, string OutputFolder, IProgress<double> progressProvider)
        {
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);
            Video? video = await Client.Videos.GetAsync(URL);
            OutputFolder = Path.Combine(OutputFolder, "Video", FileSafe(video.Author.ChannelTitle));
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);
            string name = Path.Combine(OutputFolder, FileSafe(video.Title) + ".mp4");
            await Client.Videos.DownloadAsync(URL, name, progressProvider);
            return name;
        }

        public static async Task<string[]> GetAllUrls(string URL)
        {
            IReadOnlyList<PlaylistVideo>? videos = new List<PlaylistVideo>();
            try
            {
                Playlist? playlist = await Client.Playlists.GetAsync(URL);
                videos = await Client.Playlists.GetVideosAsync(playlist.Id);
            }
            catch
            {
                // if its not a playlist, check if its a channel
                try
                {
                    videos = await Client.Channels.GetUploadsAsync(URL);
                }
                catch
                {
                    // its niether so it must be a normal video
                    return new string[1] { URL };
                }
            }
            string[] urls = new string[videos.Count];
            int x = 0;
            foreach (PlaylistVideo? video in videos)
            {
                urls[x++] = video.Url;
            }
            return urls;
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