using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxAgr.BLL.Exeptions
{
    public class DbInfoExeption : SystemException
    {
        public DbInfoExeption(string message) : base(message) { }
        public DbInfoExeption(string message, Exception inner): base(message, inner) { }
    }
}
