using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyMaster.Model
{
    public class PlayListModel : ViewModelBase
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
                RaisePropertyChanged("FileName");
            }
        }
        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                RaisePropertyChanged("Title");
            }
        }
        public string Album
        {
            get { return album; }
            set
            {
                album = value;
                RaisePropertyChanged("Album");
            }
        }
        public string Artist
        {
            get { return artist; }
            set
            {
                artist = value;
                RaisePropertyChanged("Artist");
            }
        }
        public string TotalTime
        {
            get { return totaltime; }
            set
            {
                totaltime = value;
                RaisePropertyChanged("TotalTime");
            }
        }

    }
}
