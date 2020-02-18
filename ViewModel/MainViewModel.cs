using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HappyMaster.Db;
using HappyMaster.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace HappyMaster.ViewModel
{

    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            localDb = new localDb();
            AddCommand = new RelayCommand<string>(t => Add(t));
            
        }
        localDb localDb;

        private ObservableCollection <PlayList> gridModelList;
        private ObservableCollection<PlayList> GridModelList
        {
            get { return gridModelList; }
            set { gridModelList = value; RaisePropertyChanged(); }
        }
        #region Command
        public RelayCommand<string> AddCommand { get; }
        #endregion

        public void Query()
        {
            var models = localDb.GetMusics();
            GridModelList = new ObservableCollection<PlayList>();
            if (models != null)
            {
                models.ForEach(arg =>
                {
                    GridModelList.Add(arg);
                });
            }
        }

        public void Add(string _filename)
        {
          PlayList _PlayLists = new PlayList();
            _PlayLists.Title = _filename;
        GridModelList.Add(_PlayLists);
        }

    }
}