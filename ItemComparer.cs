using System;
using System.Collections.Generic;

public class ItemComparer
{
    public class ExpirationDateComparator : IComparer<Item>
    {
        public int Compare(Item? a, Item? b)
        {
            if (a == null || b == null) return 0;

            bool aPerishable = a is PerishableItem;
            bool bPerishable = b is PerishableItem;

            if (aPerishable && !bPerishable) return -1;
            if (!aPerishable && bPerishable) return 1;

            if (aPerishable && bPerishable)
            {
                var pa = (PerishableItem)a;
                var pb = (PerishableItem)b;
                int dateCompare = pa.ExpirationDate.CompareTo(pb.ExpirationDate);
                if (dateCompare != 0) return dateCompare;
                return string.Compare(pa.Name, pb.Name, StringComparison.Ordinal);
            }

            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        }
    }
}