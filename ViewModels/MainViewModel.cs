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

        private ObservableCollection <PlayListModel> gridModelList;
        private ObservableCollection<PlayListModel> GridModelList
        {
            get { return gridModelList; }
            set { gridModelList = value; RaisePropertyChanged("GridModelList"); }
        }
        #region Command
        public RelayCommand<string> AddCommand { get; }
        #endregion

        public void Query()
        {
            var models = localDb.GetMusics();
            GridModelList = new ObservableCollection<PlayListModel>();
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
          PlayListModel _PlayLists = new PlayListModel();
            _PlayLists.Title = _filename;
        GridModelList.Add(_PlayLists);
        }

    }
}