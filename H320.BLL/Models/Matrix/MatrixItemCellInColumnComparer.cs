using System.Collections.Generic;

namespace BoxAgr.BLL.Models.Matrix
{
    public class MatrixItemCellInColumnComparer : IComparer<MatrixItem>
    {
        private readonly int _maxY = 0;

        public MatrixItemCellInColumnComparer( int maxY)
        {
            this._maxY = maxY;
        }
        public int Compare(MatrixItem? x, MatrixItem? y)
        {
            if (x == null && y == null) return 0;
            if (x == null || y == null) return -1;

            int x1 = x.y + (x.x * _maxY);
            int x2 = y.y+ (y.x * _maxY);//для того чтоб выстроить все точки на по одной оси Y. но с кареляцией от их положения по X 
            return x1.CompareTo(x2);
        }
    }
}
