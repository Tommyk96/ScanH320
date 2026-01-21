
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using Utilite.Slutsk;

namespace BoxAgr.BLL.Models.Matrix
{

    public class Matrix
    {
        public int Row;
        public int Column;

        public string BoxBarcode = "";
        public DateTime barcodeTimeShtamp = DateTime.MinValue;


        public List<MatrixItem> Cells = new List<MatrixItem>();
        public DateTime matrixTimeShtamp = DateTime.MinValue;


        public Matrix() { }

        public void CreateFromData(Unit[] data)
        {
            Cells.Clear();

            

            foreach (Unit s in data)
            {
                Cells.Add(new MatrixItem(s));
            }

           // Cells.Sort(new MatrixItemCellInLineComparer(Height, Width));
            //добавить в массив определив ряды данных
            //Unit? lu = null;
            //int rowId = 0;
            //foreach (Unit unit in data)
            //{
            //    if (lu is null || lu?.X > unit.X)
            //        rowId++;

            //    lu = unit;
            //    Cells.Add(new MatrixItem(unit) { cellRowId = rowId });
            //}
        }
    }
}
