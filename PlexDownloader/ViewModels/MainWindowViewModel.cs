using Newtonsoft.Json;
using PlexDownloader.Models;
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
        public ObservableCollection<SourceViewModel> Sources { get; set; } = new ObservableCollection<SourceViewModel>();
        public ObservableCollection<VideoViewModel> AwaitingDownload { get; set; } = new ObservableCollection<VideoViewModel>();

        public static string JsonSaveLocation = Path.Combine(Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).FullName, "Queued");
        public static string SaveLocation = "W:\\YoutubeSeries";

        public MainWindowViewModel()
        {
            DataBaseHelper.Initialize();
            if (!Directory.Exists(JsonSaveLocation))
                Directory.CreateDirectory(JsonSaveLocation);
            List<Source> sources = DataBaseHelper.GetAllSources();
            foreach (Source source in sources)
            {
                Sources.Add(new SourceViewModel() { Downloaded = source.NumberOfDownloadedVideos.ToString(), Pending = source.NumberOfPendingVideos.ToString(), ID = source.ID, Name = source.Name });
            }
            Task.Run(async () =>
            {
                try
                {
                    List<Wanted> items = DataBaseHelper.GetAllWanted();
                    foreach (var item in items)
                    {
                        AwaitingDownload.Add(new VideoViewModel(item));
                    }
                }
                catch (Exception ex)
                {

                }

            });
            Task.Run(DownloadThread);
            Task.Run(ScanThread);
        }
        private async void ScanThread()
        {
            while (Open)
            {
                try
                {
                    List<Source> sourcesToRescan = DataBaseHelper.GetSourcesThatNeedToBeRescanned(120);
                    foreach (Source source in sourcesToRescan)
                    {
                        if (source.YouTubeURL != null)
                        {
                            bool treatAsSeries = (SourceTypeEnum)source.SourceType == SourceTypeEnum.ChannelAsSeries || (SourceTypeEnum)source.SourceType == SourceTypeEnum.PlaylistAsSeries || (SourceTypeEnum)source.SourceType == SourceTypeEnum.Audio;
                            Detected[] downloadables = await DownloadHelper.GetDownloadables(source);
                            DataBaseHelper.UpdateDetectedForSource(source.ID, downloadables);
                            Detected[] toDownload = DataBaseHelper.GetUndownloadedIds(source.ID);
                            if (toDownload.Length > 0)
                            {
                                DataBaseHelper.AddWanteds(toDownload);
                                _ = Task.Run(() =>
                                  {
                                      foreach (Detected item in toDownload)
                                      {
                                          AwaitingDownload.Add(new VideoViewModel(new Wanted(item)));
                                      }
                                  });
                            }
                            source.TotalNumberOfVideos = downloadables.Length;
                            source.LastScanned = DateTime.Now;
                            source.NumberOfPendingVideos = DataBaseHelper.GetWantedCountBySourceID(source.ID);
                            source.NumberOfDownloadedVideos = DataBaseHelper.GetDownloadedCountBySourceID(source.ID);
                            foreach (var sourceViewModel in Sources)
                            {
                                if (sourceViewModel.ID == source.ID)
                                {
                                    sourceViewModel.Pending = source.NumberOfPendingVideos.ToString();
                                    sourceViewModel.Downloaded = source.NumberOfDownloadedVideos.ToString();
                                }
                            }
                            DataBaseHelper.UpdateSource(source);

                        }
                    }
                    if (sourcesToRescan.Count == 0)
                    {
                        Thread.Sleep(5000);
                    }
                }
                catch (Exception ex)
                {

                }
            }
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
        internal void Closing()
        {
            Open = false;
        }

        internal async void EnterClickedInBox()
        {
            bool treatAsSeries = TreatAsSeriesChecked;
            string[] toAdd = Input.Split("\r\n");
            Input = "";
            List<string> sourceUrls = DataBaseHelper.GetAllSourcesURLS();
            int added = 0;
            for (int x = 0; x < toAdd.Length; x++)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(toAdd[x]) && !sourceUrls.Contains(toAdd[x]))
                    {
                        added++;
                        SourceTypeEnum sourceType = 0;
                        if (!VideoChecked)
                        {
                            sourceType = SourceTypeEnum.Audio;
                        }
                        else if (await DownloadHelper.IsChannel(toAdd[x]))
                        {
                            if (treatAsSeries)
                                sourceType = SourceTypeEnum.ChannelAsSeries;
                            else
                                sourceType = SourceTypeEnum.ChannelAsIndividuals;
                        }
                        else if (await DownloadHelper.IsPlaylist(toAdd[x]))
                        {
                            if (treatAsSeries)
                                sourceType = SourceTypeEnum.PlaylistAsSeries;
                            else
                                sourceType = SourceTypeEnum.PlaylistAsIndividuals;
                        }
                        else
                        {
                            sourceType = SourceTypeEnum.IndividualVideo;
                        }
                        Source source = new Source() { ID = -1, LastScanned = DateTime.MinValue, NumberOfDownloadedVideos = 0, NumberOfPendingVideos = 0, SourceType = (int)sourceType, TotalNumberOfVideos = 0, YouTubeURL = toAdd[x] };
                        source.Name = await DownloadHelper.GetSourceName(source);
                        source.ID = DataBaseHelper.AddSource(source);
                        Sources.Add(new SourceViewModel() { Downloaded = "0", Pending = "0", ID = source.ID, Name = source.Name });
                    }
                }
                catch (Exception ex)
                {
                }
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
                        Source source = DataBaseHelper.GetSourceByID(CurrentVid.UnderlyingVideo.SourceID);
                        string downloadedPath = "";
                        switch ((SourceTypeEnum)source.SourceType)
                        {
                            case SourceTypeEnum.ChannelAsSeries:
                            case SourceTypeEnum.PlaylistAsSeries:
                                downloadedPath = await DownloadHelper.DownloadSeriesStyleVideo(CurrentVid.UnderlyingVideo, _provider);
                                break;
                            case SourceTypeEnum.PlaylistAsIndividuals:
                            case SourceTypeEnum.ChannelAsIndividuals:
                            case SourceTypeEnum.IndividualVideo:
                                downloadedPath = await DownloadHelper.DownloadStandaloneVideo(CurrentVid.UnderlyingVideo, _provider);
                                break;
                            case SourceTypeEnum.Audio:
                                downloadedPath = await DownloadHelper.DownloadAudio(CurrentVid.UnderlyingVideo, _provider);
                                break;
                        }
                        AwaitingDownload.RemoveAt(x);
                        DataBaseHelper.AddDownloaded(CurrentVid.UnderlyingVideo, downloadedPath);
                        DataBaseHelper.DeleteWanted(CurrentVid.UnderlyingVideo);
                    }
                    catch (Exception ex)
                    {
                        x++;
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