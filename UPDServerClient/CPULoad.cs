using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UPDServerClient
{
    internal class CPULoad
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string host = "127.0.0.1"; 
            int port = 8888; 
            int periodMs = 5000;       

            using (var udp = new UdpClient())
            using (var cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
            {
                _ = cpu.NextValue();
                Thread.Sleep(periodMs);

                Console.WriteLine($"cpu_load клиент запущен для {host}:{port} каждые {periodMs} мс");

                bool exit = false;
                var exitThread = new Thread(() =>
                {
                    Console.ReadLine();
                    exit = true;
                })
                { IsBackground = true, Name = "ExitWaiter" };
                exitThread.Start();

                while (!exit)
                {
                    float percent = cpu.NextValue();
                    double load01 = Math.Round(percent / 100.0, 4);

                    string payload = "cpu_load:" + load01.ToString("G", CultureInfo.InvariantCulture);
                    byte[] data = Encoding.UTF8.GetBytes(payload);

                    Thread.Sleep(periodMs);
                }
            }

            Console.WriteLine("Клиент остановлен");
        }
    }
}
