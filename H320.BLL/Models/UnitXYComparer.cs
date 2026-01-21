using System.Collections.Generic;


namespace BoxAgr.BLL.Models
{
    public class UnitXYComparer : IComparer<Unit>
    {
        private int maxX = 0;
        private int maxY = 0;

        public UnitXYComparer(int _maxX, int _maxY)
        {
            maxX = _maxX;
            maxY = _maxY;
        }
        public int Compare(Unit? x, Unit? y)
        {
            if (x == null)
                return 0;

            if (y == null)
                return 0;

            int x1 = x.X + (x.Y * maxX);
            int x2 = y.X + (y.Y * maxX);// X ?!?!?!
            return x1.CompareTo(x2);
        }
    }
}
