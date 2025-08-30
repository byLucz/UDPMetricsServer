using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UDPServerLib;

namespace UDPServerCore
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            using (var server = new Metrics())
            {
                server.OnInfo += s => Console.WriteLine("[INFO] " + s);
                server.OnFormatError += msg => Console.WriteLine("Ошибка формата: " + (msg ?? ""));
                server.OnException += ex => Console.WriteLine("Ошибка: " + ex.Message);
                server.OnMetricsSnapshot += snapshot => Console.WriteLine(Utils.FormatMetricsLine(snapshot));

                server.Start();
                Console.ReadLine();
                server.Stop();
            }
        }
    }
}
