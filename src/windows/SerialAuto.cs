using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace OzonMarkingDesktop
{
    // Each COM port has its own worker, so one unresponsive driver cannot block others.
    internal sealed class AutoSerialChannel : IDisposable
    {
        private readonly ScannerDevice device;
        private readonly ScannerHook hook;
        private readonly Action<string> report;
        private readonly Action<string, int> learned;
        private readonly System.Threading.Timer timer;
        private readonly SerialFramer framer = new SerialFramer();
        private readonly BaudProbe probe;
        private SerialPort port;
        private volatile bool disposed;
        private int working;
        private long retryAt;
        public AutoSerialChannel(ScannerDevice item, ScannerHook scanner, int savedBaud, Action<string> status, Action<string, int> remember)
        {
            device = item; hook = scanner; report = status; learned = remember;
            probe = new BaudProbe(savedBaud);
            timer = new System.Threading.Timer(Tick, null, 100, 100);
        }
        private void Tick(object state)
        {
            if (disposed || Interlocked.CompareExchange(ref working, 1, 0) != 0) return;
            try
            {
                long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                if (port == null)
                {
                    if (now < retryAt) return;
                    retryAt = now + 3000;
                    SerialPort candidate = new SerialPort(device.Port, probe.Speed, Parity.None, 8, StopBits.One);
                    candidate.Handshake = Handshake.None; candidate.DtrEnable = false; candidate.RtsEnable = false;
                    candidate.ReadTimeout = 100; candidate.WriteTimeout = 100; candidate.Encoding = Encoding.GetEncoding(28591);
                    try
                    {
                        candidate.Open(); port = candidate; framer.Reset();
                        if (disposed) { Close(); return; }
                        report(device.Port + ": слушаю, " + probe.Speed + " бод. Сканируйте в пустое поле.");
                    }
                    catch { candidate.Dispose(); report(device.Port + ": занят или недоступен. Автоматический повтор подключения."); }
                    return;
                }
                try
                {
                    string chunk = port.ReadExisting();
                    if (chunk.Length > 0)
                    {
                        probe.Received(now);
                        foreach (SerialScan scan in framer.Feed(chunk, now, Native.GetForegroundWindow(), hook.Generation)) Accept(scan);
                    }
                    SerialScan idle = framer.Poll(now); if (idle != null) Accept(idle);
                    if (probe.ShouldAdvance(now))
                    {
                        Close(); retryAt = 0;
                        report(device.Port + ": подбираю скорость. При необходимости повторите сканирование.");
                    }
                }
                catch { Close(); retryAt = now + 3000; report(device.Port + ": соединение потеряно. Ожидаю переподключение."); }
            }
            finally
            {
                if (disposed) Close();
                Interlocked.Exchange(ref working, 0);
            }
        }
        private void Accept(SerialScan scan)
        {
            if (!MarkingCode.Normalize(scan.Raw).Ok) return;
            bool first = !probe.Confirmed;
            probe.Confirm();
            if (first)
            {
                learned(device.Id.Length > 0 ? device.Id : device.Port, probe.Speed);
                report(device.Port + ": сканер определён, " + probe.Speed + " бод. Параметры запомнены.");
            }
            if (!disposed) hook.SubmitSerial(scan);
        }
        private void Close() { if (port != null) { try { port.Dispose(); } catch { } port = null; } framer.Reset(); }
        public void Dispose()
        {
            disposed = true; timer.Dispose();
            if (Interlocked.CompareExchange(ref working, 1, 0) == 0) { try { Close(); } finally { Interlocked.Exchange(ref working, 0); } }
        }
    }
}
