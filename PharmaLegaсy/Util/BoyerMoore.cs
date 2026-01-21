using System;
using System.Collections.Generic;
using System.Text;

namespace Util
{
    public static class BoyerMoore
    {
        static private void preBmBc(byte[] x, int[] bmBc)
        {
            int i;

            for (i = 0; i < bmBc.Length; ++i)
                bmBc[i] = x.Length;
            for (i = 0; i < x.Length - 1; ++i)
                bmBc[x[i]] = x.Length - i - 1;
        }
        static private void suffixes(byte[] x, int[] suff)
        {
            int f = 0, g, i;

            suff[x.Length - 1] = x.Length;
            g = x.Length - 1;
            for (i = x.Length - 2; i >= 0; --i)
            {
                if (i > g && suff[i + x.Length - 1 - f] < i - g)
                    suff[i] = suff[i + x.Length - 1 - f];
                else
                {
                    if (i < g)
                        g = i;
                    f = i;
                    while (g >= 0 && x[g] == x[g + x.Length - 1 - f])
                        --g;
                    suff[i] = f - g;
                }
            }
        }
        static private void preBmGs(byte[] x, int[] bmGs)
        {
            int i, j;
            int[] suff = new int[x.Length];

            suffixes(x, suff);

            for (i = 0; i < x.Length; ++i)
                bmGs[i] = x.Length;
            j = 0;
            for (i = x.Length - 1; i >= 0; --i)
                if (suff[i] == i + 1)
                    for (; j < x.Length - 1 - i; ++j)
                        if (bmGs[j] == x.Length)
                            bmGs[j] = x.Length - 1 - i;
            for (i = 0; i <= x.Length - 2; ++i)
                bmGs[x.Length - 1 - suff[i]] = x.Length - 1 - i;
        }
        static public int PatternSearch(byte[] pattern, byte[] source)
        {
            int i, j;
            int[] bmGs = new int[pattern.Length];

            int ASIZE = int.MinValue;
            foreach (byte b in pattern)
            {
                if (b > ASIZE)
                    ASIZE = b;
            }
            foreach (byte b in source)
            {
                if (b > ASIZE)
                    ASIZE = b;
            }
            ASIZE++;
            int[] bmBc = new int[ASIZE];

            /* Preprocessing */
            preBmGs(pattern, bmGs);
            preBmBc(pattern, bmBc);

            /* Searching */
            j = 0;
            while (j <= source.Length - pattern.Length)
            {
                for (i = pattern.Length - 1; i >= 0 && pattern[i] == source[i + j]; --i) ;
                if (i < 0)
                {
                    //OUTPUT(j);
                    return j;
                    //j += bmGs[0];
                }
                else
                    j += MAX(bmGs[i], bmBc[source[i + j]] - pattern.Length + 1 + i);
            }

            return -1;
        }
        static private void OUTPUT(int j)
        {
            Console.WriteLine("Found at index #{0}", j);
        }
        static private int MAX(int i1, int i2)
        {
            if (i1 >= i2)
                return i1;
            return i2;
        }
    }
}
