using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPServerLib
{
    public static class Utils
    {
        public static string FormatMetricsLine(IDictionary<string, double> metrics)
        {
            if (metrics == null || metrics.Count == 0)
                return "[METRIC] Нет данных";

            var parts = metrics
                .OrderBy(k => k.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key} = {kv.Value}");

            return "[METRIC] " + string.Join(" | ", parts);
        }
    }
}
