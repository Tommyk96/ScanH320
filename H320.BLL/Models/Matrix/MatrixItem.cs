using FSerialization;
using System;
using System.Windows.Media;

namespace BoxAgr.BLL.Models.Matrix
{
    public class MatrixItem : IEquatable<MatrixItem>, IComparable<MatrixItem>
    {
        public string boxCode { get; set; } = "";
        public string barcode { get; set; } = "";
        public Int32? cellRowId { get; set; }
        public Int32? cellColumId { get; set; }
        public DateTime dt { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public CodeState CodeState { get; set; }

        public Brush Brush = Brushes.Red;
        public MatrixItem() { }
        public MatrixItem(Unit d)
        {
                barcode = d.Barcode;
                x = d.X;
                y = d.Y;
                CodeState = d.CodeState;
        }


        public override bool Equals(object? obj)
        {
            if(obj is not MatrixItem objAsPart)
                return false;
                   
            else return Equals(objAsPart);
        }
        public bool Equals(MatrixItem? other)
        {
            if (other == null) return false;
            return (x == other.x && y == other.y);
        }
        public int SortByNameAscending(string name1, string name2)
        {
            return name1.CompareTo(name2);
        }
        public int SortByX(int x1, int x2)
        {
            return x1.CompareTo(x2);
        }

        // Default comparer for Part type.
        public int CompareTo(MatrixItem? comparePart)
        {
            // A null value means that this object is greater.
            if (comparePart == null)
                return 1;

            else
                return this.barcode.CompareTo(comparePart.barcode);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
