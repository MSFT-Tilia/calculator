using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace CalculatorApp
{
    public class PerfUtils
    {
        public class ScopedLog : IDisposable
        {
            public ScopedLog(PerfUtils utils, string message)
            {
                _utils = utils;
                _msg = message;
                _utils.WriteLine($"ScopedLog | {_msg} | begins ");
            }

            ~ScopedLog()
            {
                _utils.WriteLine($"ScopedLog | {_msg} | ends with bad timing");
            }

            public void Dispose()
            {
                _utils.WriteLine($"ScopedLog | {_msg} | ends");
                GC.SuppressFinalize(this);
            }

            private PerfUtils _utils;
            private string _msg;
        }

        public readonly static PerfUtils Default = new PerfUtils();

        public PerfUtils()
        {
            var date = DateTime.UtcNow;
            var filename = $"calc.perflog-{date.ToString("dd-MM-yy_hh-mm-ss-ff")}.log";

            StorageFolder storageFolder =
                ApplicationData.Current.LocalFolder;

            _file = storageFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

            _workthread = new Thread(new ThreadStart(_WorkThread));
            _workthread.Start();
        }

        public ScopedLog CreateScopedLog(string content)
        {
            return new ScopedLog(this, content);
        }

        public void WriteLine(string content)
        {
            var date = DateTime.UtcNow;
            var prefix = $"{date.ToString("dd-MM-yy hh:mm:ss.ff")} | ";
            _SubmitReq(new WriteRequest { Content = $"{prefix}{content}\n" });
            lock(_workthread)
            {
                Monitor.Pulse(_workthread);
            }
        }

        public void Quit(bool join = true)
        {
            _quit = true;
            lock(_workthread)
            {
                Monitor.Pulse(_workthread);
            }
            _workthread.Join();
        }

        private struct WriteRequest
        {
            public string Content;
        }

        private void _WorkThread()
        {
            while(!_quit)
            {
                lock (_workthread)
                {
                    while (_TryFetchReq(out WriteRequest? req))
                    {
                        FileIO.AppendTextAsync(_file, req.Value.Content).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    Monitor.Wait(_workthread, 2000);
                }
            }
        }

        private void _SubmitReq(WriteRequest req)
        {
            lock(_wreqs)
            {
                _wreqs.Enqueue(req);
            }
        }

        private bool _TryFetchReq(out WriteRequest? req)
        {
            lock(_wreqs)
            {
                if(_wreqs.Count > 0)
                {
                    req = _wreqs.Dequeue();
                    return true;
                }
            }

            req = null;
            return false;
        }

        private Windows.Storage.StorageFile _file;

        private Queue<WriteRequest> _wreqs = new Queue<WriteRequest>();
        private Thread _workthread = null;
        private volatile bool _quit = false;
    }
}
