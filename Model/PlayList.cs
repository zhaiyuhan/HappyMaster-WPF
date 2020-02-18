using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyMaster.Model
{
    public class PlayList : ViewModelBase
    {
        private string filename;
        private string title;
        private string album;
        private string artist;
        private string totaltime;
        public string FileName
        {
            get { return filename; }
            set
            {
                filename = value;
                RaisePropertyChanged();
            }
        }
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                RaisePropertyChanged();
            }
        }
        public string Album
        {
            get { return album; }
            set
            {
                title = value;
                RaisePropertyChanged();
            }
        }
        public string Artist
        {
            get { return artist; }
            set
            {
                title = value;
                RaisePropertyChanged();
            }
        }
        public string TotalTime
        {
            get { return totaltime; }
            set
            {
                title = value;
                RaisePropertyChanged();
            }
        }

    }
}
