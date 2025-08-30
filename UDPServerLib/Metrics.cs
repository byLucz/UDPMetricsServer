using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UDPServerLib
{
    public sealed class Metrics : IDisposable
    {
        public const string Host = "127.0.0.1";
        public const int Port = 8888;
        private const int MaxDatagramBytes = 256;
        private const int PrintIntervalMs = 5000;
        private const int ReceiveTimeoutMs = 1000;

        public event Action<IDictionary<string, double>> OnMetricsSnapshot;
        public event Action<string> OnFormatError;
        public event Action<Exception> OnException;
        public event Action<string> OnInfo;

        private readonly Dictionary<string, double> _metrics = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly ReaderWriterLockSlim _rw = new ReaderWriterLockSlim();
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private readonly Encoding _encoding = new UTF8Encoding(false);

        private Thread _recvThread;
        private Thread _printThread;
        private UdpClient _udp;
        private int _running;

        public bool IsRunning => Interlocked.CompareExchange(ref _running, 0, 0) == 1;
        private void SafeInfo(string s) { try { OnInfo?.Invoke(s); } catch { } }
        private void SafeError(Exception ex) { try { OnException?.Invoke(ex); } catch { } }
        private void SafeFormatError(string msg) { try { OnFormatError?.Invoke(msg ?? string.Empty); } catch (Exception ex) { SafeError(ex); } }

        public void Start()
        {
            if (Interlocked.Exchange(ref _running, 1) == 1) return;

            _stopEvent.Reset();
            _udp = new UdpClient(Port);
            _udp.Client.ReceiveTimeout = ReceiveTimeoutMs;

            _recvThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "Udp.Receive" };
            _printThread = new Thread(PrintLoop) { IsBackground = true, Name = "Udp.Print" };

            _recvThread.Start();
            _printThread.Start();

            SafeInfo($"Сервер запущен для {Host}:{Port}");
        }

        public void Dispose() { Stop(); }

        public void Stop()
        {
            if (Interlocked.Exchange(ref _running, 0) == 0) return;

            _stopEvent.Set();
            try { _udp?.Close(); } catch { }

            try { _recvThread?.Join(); } catch { }
            try { _printThread?.Join(); } catch { }

            _udp = null;
            SafeInfo("Сервер остановлен");
        }

        private void ReceiveLoop()
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                while (IsRunning && !_stopEvent.WaitOne(0))
                {
                    try
                    {
                        byte[] data = _udp.Receive(ref remote);
                        if (data == null) continue;

                        if (data.Length > MaxDatagramBytes)
                        {
                            continue;
                        }

                        string message = _encoding.GetString(data);
                        HandleMessage(message);
                    }
                    catch (SocketException se)
                    {
                        continue;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        SafeError(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeError(ex);
            }
        }

        private void HandleMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg) || msg.IndexOf(':') < 0)
            {
                SafeFormatError(msg); return;
            }

            int idx = msg.IndexOf(':');
            string name = msg.Substring(0, idx);
            string valStr = msg.Substring(idx + 1);

            if (string.IsNullOrEmpty(name) || name.IndexOf(' ') >= 0 || name.IndexOf(':') >= 0)
            {
                SafeFormatError(msg); return;
            }

            if (!double.TryParse(valStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double value))
            {
                SafeFormatError(msg); return;
            }

            _rw.EnterWriteLock();
            try { _metrics[name] = value; } 
            finally { _rw.ExitWriteLock(); }
        }

        private void PrintLoop()
        {
            try
            {
                while (IsRunning)
                {
                    if (_stopEvent.WaitOne(PrintIntervalMs)) break;

                    IDictionary<string, double> snapshot;
                    _rw.EnterReadLock();
                    try
                    {
                        snapshot = _metrics.Count == 0
                            ? new Dictionary<string, double>(0)
                            : new Dictionary<string, double>(_metrics);
                    }
                    finally { _rw.ExitReadLock(); }

                    try { OnMetricsSnapshot?.Invoke(snapshot); }
                    catch (Exception ex) { SafeError(ex); }
                }
            }
            catch (Exception ex)
            {
                SafeError(ex);
            }
        }
    }
}
