using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PlexDownloader.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public VideoViewModel _currentVid;
        private bool Open = true;
        public double _downloadProgress = 0;
        public bool _videoChecked = true;
        public bool _treatAsSeriesChecked = false;
        private string _input = "";
        private DownloadUpdateProvider _provider;
        private List<AudioInfo> AudiosPending = new List<AudioInfo>();
        private List<SeriesVideoInfo> SeriesPending = new List<SeriesVideoInfo>();
        private List<MovieVideoInfo> MoviesPending = new List<MovieVideoInfo>();
        private List<string> EnteredStrings = new List<string>();
        private List<AudioInfo> AudiosErrored = new List<AudioInfo>();
        private List<SeriesVideoInfo> SeriesErrored = new List<SeriesVideoInfo>();
        private List<MovieVideoInfo> MoviesErrored = new List<MovieVideoInfo>();

        public static string JsonSaveLocation = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).FullName, "Queued");
        public static string SaveLocation = "Z:\\YoutubeSeries";

        public MainWindowViewModel()
        {
            if (!Directory.Exists(JsonSaveLocation))
                Directory.CreateDirectory(JsonSaveLocation);
            List<AudioInfo> audioUrls = new List<AudioInfo>();
            List<SeriesVideoInfo> seriesUrls = new List<SeriesVideoInfo>();
            List<MovieVideoInfo> movieUrls = new List<MovieVideoInfo>();
            if (File.Exists(Path.Combine(JsonSaveLocation, "Series.json")))
                seriesUrls = JsonConvert.DeserializeObject<List<SeriesVideoInfo>>(File.ReadAllText(Path.Combine(JsonSaveLocation, "Series.json")));
            if (File.Exists(Path.Combine(JsonSaveLocation, "Series.json")))
                movieUrls = JsonConvert.DeserializeObject<List<MovieVideoInfo>>(File.ReadAllText(Path.Combine(JsonSaveLocation, "Movies.json")));
            if (File.Exists(Path.Combine(JsonSaveLocation, "Audio.json")))
                audioUrls = JsonConvert.DeserializeObject<List<AudioInfo>>(File.ReadAllText(Path.Combine(JsonSaveLocation, "Audio.json")));

            if (File.Exists(Path.Combine(JsonSaveLocation, "SeriesErrored.json")))
                SeriesErrored = JsonConvert.DeserializeObject<List<SeriesVideoInfo>>(File.ReadAllText(Path.Combine(JsonSaveLocation, "SeriesErrored.json")));
            if (File.Exists(Path.Combine(JsonSaveLocation, "MoviesErrored.json")))
                MoviesErrored = JsonConvert.DeserializeObject<List<MovieVideoInfo>>(File.ReadAllText(Path.Combine(JsonSaveLocation, "MoviesErrored.json")));
            if (File.Exists(Path.Combine(JsonSaveLocation, "AudioErrored.json")))
                AudiosErrored = JsonConvert.DeserializeObject<List<AudioInfo>>(File.ReadAllText(Path.Combine(JsonSaveLocation, "AudioErrored.json")));
            if (File.Exists(Path.Combine(JsonSaveLocation, "EnteredStrings.json")))
                EnteredStrings = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(Path.Combine(JsonSaveLocation, "EnteredStrings.json")));
            Task.Run(async () =>
            {
                if (seriesUrls.Count > 0)
                    await AddDownloadables(seriesUrls);
                if (movieUrls.Count > 0)
                    await AddDownloadables(movieUrls);
                if (audioUrls.Count > 0)
                    await AddDownloadables(audioUrls);
                UpdateOnDisk();
                _provider = new DownloadUpdateProvider(this);
            });
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
        public bool TreatAsSeriesChecked
        {
            get => _treatAsSeriesChecked;
            set => this.RaiseAndSetIfChanged(ref _treatAsSeriesChecked, value);
        }

        private ObservableCollection<VideoViewModel> AwaitingDownload { get; set; } = new ObservableCollection<VideoViewModel>();

        public async Task AddDownloadables(List<MovieVideoInfo> info)
        {
            foreach (var obj in info)
            {
                MoviesPending.Add(obj);
                AwaitingDownload.Add(new VideoViewModel(await DownloadHelper.GetVideo(obj.URL), obj));
            }
        }
        public async Task AddDownloadables(List<AudioInfo> info)
        {
            foreach (var obj in info)
            {
                AudiosPending.Add(obj);
                AwaitingDownload.Add(new VideoViewModel(await DownloadHelper.GetVideo(obj.URL), obj));
            }
        }
        public async Task AddDownloadables(List<SeriesVideoInfo> info)
        {
            foreach (var obj in info)
            {
                SeriesPending.Add(obj);
                AwaitingDownload.Add(new VideoViewModel(await DownloadHelper.GetVideo(obj.URL), obj));
            }
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
            File.WriteAllText(Path.Combine(JsonSaveLocation, "Series.json"), JsonConvert.SerializeObject(SeriesPending));
            File.WriteAllText(Path.Combine(JsonSaveLocation, "Movies.json"), JsonConvert.SerializeObject(MoviesPending));
            File.WriteAllText(Path.Combine(JsonSaveLocation, "Audio.json"), JsonConvert.SerializeObject(AudiosPending));
            File.WriteAllText(Path.Combine(JsonSaveLocation, "SeriesErrored.json"), JsonConvert.SerializeObject(SeriesErrored));
            File.WriteAllText(Path.Combine(JsonSaveLocation, "MoviesErrored.json"), JsonConvert.SerializeObject(MoviesErrored));
            File.WriteAllText(Path.Combine(JsonSaveLocation, "AudioErrored.json"), JsonConvert.SerializeObject(AudiosErrored));
            File.WriteAllText(Path.Combine(JsonSaveLocation, "EnteredStrings.json"), JsonConvert.SerializeObject(EnteredStrings));
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
                IDownloadableInfo[] urls = await DownloadHelper.GetDownloadables(input, TreatAsSeriesChecked);
                if (urls[0] is MovieVideoInfo)
                {
                    List<MovieVideoInfo> items = new List<MovieVideoInfo>();
                    foreach (var item in urls)
                    {
                        items.Add((MovieVideoInfo)item);
                    }
                    await AddDownloadables(items);
                    UpdateOnDisk();
                }
                else if (urls[0] is SeriesVideoInfo)
                {
                    List<SeriesVideoInfo> items = new List<SeriesVideoInfo>();
                    foreach (var item in urls)
                    {
                        items.Add((SeriesVideoInfo)item);
                    }
                    await AddDownloadables(items);
                    UpdateOnDisk();
                }
                else
                {
                    List<AudioInfo> items = new List<AudioInfo>();
                    foreach (var item in urls)
                    {
                        items.Add((AudioInfo)item);
                    }
                    await AddDownloadables(items);
                    UpdateOnDisk();
                }
            }
            catch (Exception ex)
            {
            }
        }

        private async void DownloadThread()
        {
            int x = 0;
            while (Open)
            {
                if (AwaitingDownload.Count > x)
                {
                    try
                    {
                        CurrentVid = AwaitingDownload[x];
                        if (AwaitingDownload[x].Info is SeriesVideoInfo seriesInfo)
                        {

                            await DownloadHelper.DownloadSeriesStyleVideo(AwaitingDownload[x].UnderlyingVideo.Url, seriesInfo, _provider);
                            AwaitingDownload.RemoveAt(x);
                            SeriesPending.Remove(seriesInfo);
                            UpdateOnDisk();
                        }
                        else if (AwaitingDownload[x].Info is MovieVideoInfo movieInfo)
                        {
                            await DownloadHelper.DownloadStandaloneVideo(AwaitingDownload[x].UnderlyingVideo.Url, movieInfo.Name, _provider);
                            AwaitingDownload.RemoveAt(x);
                            MoviesPending.Remove(movieInfo);
                            UpdateOnDisk();
                        }
                        else if (AwaitingDownload[x].Info is AudioInfo audioInfo)
                        {
                            await DownloadHelper.DownloadAudio(AwaitingDownload[x].UnderlyingVideo.Url, audioInfo, _provider);
                            AwaitingDownload.RemoveAt(x);
                            AudiosPending.Remove(audioInfo);
                            UpdateOnDisk();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (AwaitingDownload[x].Info is SeriesVideoInfo seriesInfo)
                        {
                            AwaitingDownload.RemoveAt(x);
                            SeriesPending.Remove(seriesInfo);
                            SeriesErrored.Add(seriesInfo);
                            UpdateOnDisk();
                        }
                        else if (AwaitingDownload[x].Info is MovieVideoInfo movieInfo)
                        {
                            AwaitingDownload.RemoveAt(x);
                            MoviesPending.Remove(movieInfo);
                            MoviesErrored.Add(movieInfo);
                            UpdateOnDisk();
                        }
                        else if (AwaitingDownload[x].Info is AudioInfo audioInfo)
                        {
                            AwaitingDownload.RemoveAt(x);
                            AudiosPending.Remove(audioInfo);
                            AudiosErrored.Add(audioInfo);
                            UpdateOnDisk();
                        }
                    }
                }
                else
                {
                    x = 0;
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