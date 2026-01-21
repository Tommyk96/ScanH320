using BoxAgr.BLL.Models;
using System;
using System.Collections.Generic;

namespace BoxAgr.BLL.Exeptions
{
    public class BoxInfoExeption : SystemException
    {
        public List<Unit> Layer { get; set; }
        public BoxInfoExeption(string message, List<Unit> layer ) : base(message) { Layer = layer; }
        
    }
}
