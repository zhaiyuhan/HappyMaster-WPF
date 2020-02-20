using HappyMaster.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyMaster.Db
{
    public class localDb
    {
        public localDb()
        {

        }
        private List<PlayListModel> PlayLists;

        private void Init()
        {
            PlayLists = new List<PlayListModel>();
        }
        public List<PlayListModel> GetMusics()
        {
            return PlayLists;
        }


        public void AddRecord(PlayListModel pl)
        {
            PlayLists.Add(pl);
        }
        public List<PlayListModel> SearchMusic(string music_name)
        {
            return PlayLists.Where(q => q.Title.Contains(music_name)).ToList();
        }

        public PlayListModel GetByFileName(string _filename)
        {
            return PlayLists.FirstOrDefault(t => t.FileName == _filename);
        }
    }
    
     
}
