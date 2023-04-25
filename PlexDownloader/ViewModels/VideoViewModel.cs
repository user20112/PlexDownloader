using ReactiveUI;
using System.IO;
using System.Threading.Tasks;
using YoutubeExplode.Videos;

namespace PlexDownloader.ViewModels
{
    public class VideoViewModel : ReactiveObject
    {
        public Video UnderlyingVideo;
        public IDownloadableInfo Info;
        private Avalonia.Media.Imaging.Bitmap _thumbnail;
        private string _title;

        public VideoViewModel(Video video,IDownloadableInfo info)
        {
            Info = info;
            UnderlyingVideo = video;
            Title = video.Title;
            Task.Run(() =>
            {
                Stream stream = new MemoryStream(DownloadHelper.DownloadThumbnail(video));
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