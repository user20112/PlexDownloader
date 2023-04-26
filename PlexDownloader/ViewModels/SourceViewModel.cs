using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexDownloader.ViewModels
{
    public class SourceViewModel : ReactiveObject
    {
        private string _downloaded;
        private string _name;
        private string _pending;
        public long ID;
        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }
        public string Pending
        {
            get => "Pending:" + _pending;
            set => this.RaiseAndSetIfChanged(ref _pending, value);
        }
        public string Downloaded
        {
            get => "Downloaded:" + _downloaded;
            set => this.RaiseAndSetIfChanged(ref _downloaded, value);
        }
    }
}
