using System.Collections.Generic;

namespace BoxAgr.BLL.Models.Matrix
{
    public class MatrixItemYComparer : IComparer<MatrixItem>
    {
        public int Compare(MatrixItem? x, MatrixItem? y)
        {
            if (x == null && y == null) return 0;
            if (x == null || y == null) return -1;

            return x.y.CompareTo(y.y);
        }
    }
}
