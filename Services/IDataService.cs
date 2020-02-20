using HappyMaster.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyMaster.Services
{
    interface IDataService
    {
        List<PlayListModel> GetAllPlayLists();
    }
}
