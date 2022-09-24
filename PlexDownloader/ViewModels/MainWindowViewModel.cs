using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PlexDownloader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public VideoViewModel _currentVid;
        public double _downloadProgress = 0;
        public bool _videoChecked = true;
        private string _input = "";
        private DownloadUpdateProvider _provider;
        private List<string> AudioURLS = new List<string>();
        private string JsonSaveLocation = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).FullName, "Queued");
        private bool Open = true;
        private string SaveLocation = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).FullName, "Downloaded");
        private List<string> VideoURLS = new List<string>();

        public MainWindowViewModel()
        {
            if (!Directory.Exists(JsonSaveLocation))
                Directory.CreateDirectory(JsonSaveLocation);
            List<string> aurls = new List<string>();
            List<string> vurls = new List<string>();
            if (File.Exists(Path.Combine(JsonSaveLocation, "Videos.json")))
                vurls = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Path.Combine(JsonSaveLocation, "Videos.json")));
            if (File.Exists(Path.Combine(JsonSaveLocation, "Audio.json")))
                aurls = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Path.Combine(JsonSaveLocation, "Audio.json")));
            if (vurls.Count != 0)
                AddVideoURLS(vurls.ToArray());
            if (aurls.Count != 0)
                AddAudioURLS(aurls.ToArray());
            _provider = new DownloadUpdateProvider(this);
            Task.Run(DownloadThread);
        }

        public VideoViewModel CurrentVid
        {
            get => _currentVid;
            set => this.RaiseAndSetIfChanged(ref _currentVid, value);
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set => this.RaiseAndSetIfChanged(ref _downloadProgress, value);
        }

        public string Input
        {
            get => _input;
            set => this.RaiseAndSetIfChanged(ref _input, value);
        }

        public bool VideoChecked
        {
            get => _videoChecked;
            set => this.RaiseAndSetIfChanged(ref _videoChecked, value);
        }

        private ObservableCollection<Tuple<VideoViewModel, string>> AwaitingDownload { get; set; } = new ObservableCollection<Tuple<VideoViewModel, string>>();

        public void AddAudioURLS(string[] URLs)
        {
            Task.Run(async () =>
            {
                int x = 0;
                foreach (string url in URLs)
                {
                    AudioURLS.Add(url);
                    AwaitingDownload.Add(new Tuple<VideoViewModel, string>(new VideoViewModel(await DownloadHelper.GetVideo(url)), "Audio"));
                }
                UpdateOnDisk();
            });
        }

        public void AddVideoURLS(string[] URLs)
        {
            Task.Run(async () =>
            {
                int x = 0;
                foreach (string url in URLs)
                {
                    VideoURLS.Add(url);
                    AwaitingDownload.Add(new Tuple<VideoViewModel, string>(new VideoViewModel(await DownloadHelper.GetVideo(url)), "Video"));
                }
                UpdateOnDisk();
            });
        }

        public async void temp()
        {
            // You can specify both video ID or URL
            //string downloadedVideo = await DownloadHelper.DownloadVideo("https://www.youtube.com/watch?v=yvIGcnbDyio", SaveLocation);
            //string downloadedAudio = await DownloadHelper.DownloadAudio("https://www.youtube.com/watch?v=yvIGcnbDyio", SaveLocation);
            //string[] URLs = await DownloadHelper.GetURLsFromPlayList("https://www.youtube.com/watch?v=yvIGcnbDyio");// normal song
            //string[] URLs = await DownloadHelper.GetURLsFromPlayList("https://www.youtube.com/watch?v=g-RdUOLXG8k&list=RDMMg-RdUOLXG8k&");// MyMix
            //string[] URLs = await DownloadHelper.GetURLsFromPlayList("https://www.youtube.com/watch?v=LsNd5DmbOgA&list=PL5-WT4DlkvgpZCLKZuDSgwbsY7jRBlkGw");// known stable playlist.
        }

        public void UpdateOnDisk()
        {
            File.WriteAllText(Path.Combine(JsonSaveLocation, "Video.json"), JsonConvert.SerializeObject(VideoURLS));
            File.WriteAllText(Path.Combine(JsonSaveLocation, "Audio.json"), JsonConvert.SerializeObject(AudioURLS));
        }

        internal void Closing()
        {
            Open = false;
            UpdateOnDisk();
        }

        internal async void EnterClickedInBox()
        {
            try
            {
                string input = Input.Replace("\r\n", "");
                Input = "";
                string[] urls = await DownloadHelper.GetAllUrls(input);
                if (VideoChecked)
                    AddVideoURLS(urls);
                else
                    AddAudioURLS(urls);
            }
            catch
            {
            }
        }

        private async void DownloadThread()
        {
            while (Open)
            {
                if (AwaitingDownload.Count > 0)
                {
                    CurrentVid = AwaitingDownload[0].Item1;
                    if (AwaitingDownload[0].Item2 == "Video")
                    {
                        await DownloadHelper.DownloadVideo(AwaitingDownload[0].Item1.UnderlyingVideo.Url, Path.Combine("Videos", SaveLocation), _provider);
                        AwaitingDownload.RemoveAt(0);
                        UpdateOnDisk();
                    }
                    else
                    {
                        await DownloadHelper.DownloadAudio(AwaitingDownload[0].Item1.UnderlyingVideo.Url, Path.Combine("Songs", SaveLocation), _provider);
                        AwaitingDownload.RemoveAt(0);
                        UpdateOnDisk();
                    }
                }
                else
                {
                    CurrentVid = null;
                    DownloadProgress = 0;
                    Thread.Sleep(100);
                }
            }
        }

        private class DownloadUpdateProvider : IProgress<double>
        {
            private MainWindowViewModel _vm;

            public DownloadUpdateProvider(MainWindowViewModel vm)
            {
                _vm = vm;
            }

            public void Report(double value)
            {
                _vm.DownloadProgress = value * 100;
            }
        }
    }
}