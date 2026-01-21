using BoxAgr.BLL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BoxAgr.BLL.Interfaces
{
    public interface IBoxRepository
    {
        public bool IsExist(string unitNum);
        public Unit? GetUnitByBarcode(string unitNum);
    }
}
