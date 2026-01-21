using System.Collections.Generic;

namespace BoxAgr.BLL.Models.Matrix
{
    public class MatrixItemCellInLineComparer : IComparer<MatrixItem>
    {
        private readonly int _maxX = 0;
       

        public MatrixItemCellInLineComparer(int maxX)
        {
            this._maxX = maxX;
            
        }
        public int Compare(MatrixItem? x, MatrixItem? y)
        {
            if (x == null && y == null) return 0;
            if (x == null || y == null) return -1;

            int x1 = x.x + (x.y * _maxX);
            int x2 = y.x + (y.y * _maxX);//для того чтоб выстроить все точки на по одной оси X. но с кареляцией от их положения по Y 
            return x1.CompareTo(x2);
        }
    }
}
