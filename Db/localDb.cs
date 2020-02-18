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
        private List<PlayList> PlayLists;

        private void Init()
        {
            PlayLists = new List<PlayList>();
        }
        public List<PlayList> GetMusics()
        {
            return PlayLists;
        }


        public void AddRecord(PlayList pl)
        {
            PlayLists.Add(pl);
        }
        public List<PlayList> SearchMusic(string music_name)
        {
            return PlayLists.Where(q => q.Title.Contains(music_name)).ToList();
        }

        public PlayList GetByFileName(string _filename)
        {
            return PlayLists.FirstOrDefault(t => t.FileName == _filename);
        }
    }
    
     
}
