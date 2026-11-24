using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Saraff.TwainX {

    /// <summary>
    /// Dedicated thread that owns a TWAIN session. On Windows the thread is STA and pumps Win32 messages.
    /// </summary>
    internal sealed class TwainXSessionThread : IDisposable {
        private const uint PM_REMOVE = 0x0001;

        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();
        private readonly Thread _thread;
        private readonly bool _useMessagePump;
        private volatile bool _disposed;

        public TwainXSessionThread(string name) {
            this._useMessagePump = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            this._thread = new Thread(this.Run) {
                IsBackground = true,
                Name = name
            };
            if(this._useMessagePump) {
                try {
                    this._thread.SetApartmentState(ApartmentState.STA);
                } catch(PlatformNotSupportedException) {
                }
            }
            this._thread.Start();
        }

        public bool IsCurrentThread => Thread.CurrentThread.ManagedThreadId == this._thread.ManagedThreadId;

        public void Post(Action action) {
            if(this._disposed) {
                throw new ObjectDisposedException(nameof(TwainXSessionThread));
            }
            this._queue.Add(action);
        }

        public void PumpMessages() {
            if(!this._useMessagePump) {
                return;
            }
            MSG _msg;
            while(PeekMessage(out _msg, IntPtr.Zero, 0, 0, PM_REMOVE)) {
                TranslateMessage(ref _msg);
                DispatchMessage(ref _msg);
            }
        }

        public void Dispose() {
            if(this._disposed) {
                return;
            }
            this._disposed = true;
            this._queue.CompleteAdding();
            if(!this.IsCurrentThread) {
                this._thread.Join(5000);
            }
            this._queue.Dispose();
        }

        private void Run() {
            while(!this._disposed || this._queue.Count > 0) {
                Action _action;
                if(this._queue.TryTake(out _action, 10)) {
                    try {
                        _action();
                    } catch(Exception ex) {
                        Trace.TraceError("Saraff.TwainX: session thread action failed: " + ex);
                    }
                } else {
                    this.PumpMessages();
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public int ptX;
            public int ptY;
        }

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);
    }
}
