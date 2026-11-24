using System;
using System.Threading;
using System.Threading.Tasks;

namespace Saraff.TwainX {

    /// <summary>
    /// Runs asynchronous TWAIN acquisitions on a dedicated session thread.
    /// </summary>
    public sealed class TwainXAcquisitionSession : IDisposable {
        private readonly TwainXAcquireOptions _options;
        private readonly TwainXSessionThread _thread;
        private int _busy;
        private bool _disposed;

        public TwainXAcquisitionSession() : this(null) {
        }

        public TwainXAcquisitionSession(TwainXAcquireOptions options) {
            this._options = options ?? new TwainXAcquireOptions();
            this._thread = new TwainXSessionThread("Saraff.TwainX acquisition");
        }

        public Task<TwainXAcquisitionResult> AcquireAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            if(this._disposed) {
                throw new ObjectDisposedException(nameof(TwainXAcquisitionSession));
            }
            if(Interlocked.CompareExchange(ref this._busy, 1, 0) != 0) {
                throw new InvalidOperationException("An acquisition is already running on this session.");
            }

            var _result = new TaskCompletionSource<TwainXAcquisitionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            if(cancellationToken.IsCancellationRequested) {
                _result.SetCanceled();
                Interlocked.Exchange(ref this._busy, 0);
                return _result.Task;
            }

            try {
                this._thread.Post(delegate {
                    try {
                        TwainXAcquisitionResult _value;
                        using(var _session = new TwainXSessionAdapter()) {
                            var _runner = new TwainXAcquisitionRunner();
                            _value = _runner.Run(_session, this._options, cancellationToken, this._thread.PumpMessages);
                        }
                        _result.SetResult(_value);
                    } catch(Exception ex) {
                        _result.SetException(ex);
                    } finally {
                        Interlocked.Exchange(ref this._busy, 0);
                    }
                });
            } catch(Exception ex) {
                Interlocked.Exchange(ref this._busy, 0);
                _result.SetException(ex);
            }

            return _result.Task;
        }

        public void Dispose() {
            if(this._disposed) {
                return;
            }
            this._disposed = true;
            this._thread.Dispose();
        }
    }
}
