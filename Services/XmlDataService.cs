using HappyMaster.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HappyMaster.Services
{
    class XmlDataService : IDataService
    {
        public List<PlayListModel> GetAllPlayLists()
        {
            List<PlayListModel> playList = new List<PlayListModel>();
            string xmlFileName = System.IO.Path.Combine(Environment.CurrentDirectory, @"");
            XDocument xDoc = XDocument.Load(xmlFileName);
            var playlists = xDoc.Descendants("PlayList");
            foreach(var p in playlists)
            {
                PlayListModel playListModel = new PlayListModel();
                playListModel.Title = p.Element("Title").Value;
                playList.Add(playListModel);
            }
            return playList;
        }
    }
}
