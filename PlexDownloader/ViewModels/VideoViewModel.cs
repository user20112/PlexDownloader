using PlexDownloader.Models;
using ReactiveUI;
using System.IO;
using System.Threading.Tasks;
using YoutubeExplode.Videos;

namespace PlexDownloader.ViewModels
{
    public class VideoViewModel : ReactiveObject
    {
        public Wanted UnderlyingVideo;
        private Avalonia.Media.Imaging.Bitmap _thumbnail;
        private string _title;

        public VideoViewModel(Wanted video)
        {
            UnderlyingVideo = video;
            Title = video.VideoTitle;
            Task.Run(async () =>
            {
                Stream stream = new MemoryStream(DownloadHelper.DownloadThumbnail(await DownloadHelper.GetVideo(video.ID)));
                Thumbnail = new Avalonia.Media.Imaging.Bitmap(stream);
            });
        }

        public Avalonia.Media.Imaging.Bitmap Thumbnail
        {
            get => _thumbnail;
            set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
        }

        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }
    }
}