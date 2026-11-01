using System;
using System.Collections.Generic;
using System.Linq;

namespace Saobracaj.Agenti_jobs
{
    public sealed class TextRangeMapper
    {
        private readonly string before, after;
        private readonly Dictionary<int, int> map = new Dictionary<int, int>();
        public TextRangeMapper(string oldText, string newText)
        {
            before = oldText;
            after = newText;
            if (before == after)
                return;
            int n = before.Length, m = after.Length;
            var v = new Dictionary<int, int>
            {
                {
                    1,
                    0
                }
            };
            var trace = new List<Dictionary<int, int>>();
            bool complete = false;
            for (int d = 0; d <= Math.Min(n + m, 512); d++)
            {
                trace.Add(new Dictionary<int, int>(v));
                for (int k = -d; k <= d; k += 2)
                {
                    int x;
                    if (k == -d || (k != d && Get(v, k - 1) < Get(v, k + 1)))
                        x = Get(v, k + 1);
                    else
                        x = Get(v, k - 1) + 1;
                    int y = x - k;
                    while (x < n && y < m && x >= 0 && y >= 0 && before[x] == after[y])
                    {
                        x++;
                        y++;
                    }

                    v[k] = x;
                    if (x >= n && y >= m)
                    {
                        complete = true;
                        break;
                    }
                }

                if (complete)
                    break;
            }

            if (complete)
            {
                int x = n, y = m;
                for (int d = trace.Count - 1; d >= 0; d--)
                {
                    var previous = trace[d];
                    int k = x - y;
                    int pk = k == -d || (k != d && Get(previous, k - 1) < Get(previous, k + 1)) ? k + 1 : k - 1;
                    int px = Get(previous, pk), py = px - pk;
                    while (x > px && y > py)
                    {
                        x--;
                        y--;
                        map[x] = y;
                    }

                    x = px;
                    y = py;
                }
            }
            else
            {
                int prefix = 0;
                while (prefix < Math.Min(n, m) && before[prefix] == after[prefix])
                {
                    map[prefix] = prefix;
                    prefix++;
                }

                int a = n - 1, b = m - 1;
                while (a >= prefix && b >= prefix && before[a] == after[b])
                {
                    map[a] = b;
                    a--;
                    b--;
                }
            }
        }

        private static int Get(Dictionary<int, int> d, int k)
        {
            int v;
            return d.TryGetValue(k, out v) ? v : 0;
        }

        public bool TryMap(int start, int length, out int newStart)
        {
            newStart = -1;
            if (start < 0 || length <= 0 || start > before.Length - length)
                return false;
            if (before == after)
            {
                newStart = start;
                return true;
            }

            int first;
            if (map.TryGetValue(start, out first))
            {
                bool whole = true;
                for (int i = 0; i < length; i++)
                {
                    int pos;
                    if (!map.TryGetValue(start + i, out pos) || pos != first + i)
                    {
                        whole = false;
                        break;
                    }
                }

                if (whole && first >= 0 && first <= after.Length - length && string.CompareOrdinal(before, start, after, first, length) == 0)
                {
                    newStart = first;
                    return true;
                }
            }

            // A moved annotation may be retained only when its exact text occurs once in both versions.
            var text = before.Substring(start, length);
            var found = after.IndexOf(text, StringComparison.Ordinal);
            if (found >= 0 && before.IndexOf(text, StringComparison.Ordinal) == start && before.IndexOf(text, start + 1, StringComparison.Ordinal) < 0 && after.IndexOf(text, found + 1, StringComparison.Ordinal) < 0)
            {
                newStart = found;
                return true;
            }

            return false;
        }
    }
}
