using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Saraff.TwainX {

    internal sealed class TwainXAcquisitionRunner {
        private readonly object _sync = new object();
        private readonly List<Stream> _streams = new List<Stream>();
        private readonly List<TwainX.ImageInfo> _imageInfos = new List<TwainX.ImageInfo>();
        private readonly ManualResetEventSlim _completed = new ManualResetEventSlim(false);

        private TwainXAcquireOptions _options;
        private ITwainXSession _session;
        private Action _pump;
        private CancellationToken _token;
        private Exception _error;
        private TwainX.ImageInfo _currentImageInfo;
        private bool _maxReached;
        private bool _canceled;

        public TwainXAcquisitionResult Run(ITwainXSession session, TwainXAcquireOptions options, CancellationToken token, Action pump) {
            if(options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            if(options.MaxImages.HasValue && options.MaxImages.Value <= 0) {
                throw new ArgumentOutOfRangeException(nameof(options.MaxImages));
            }
            this._session = session;
            this._options = options;
            this._token = token;
            this._pump = pump;

            session.ShowUI = options.ShowUI;
            session.ModalUI = options.ModalUI;
            session.Parent = options.Parent;
            session.DsmPath = options.DsmPath;
            session.DisableAfterAcquire = options.DisableAfterAcquire;

            session.EndXfer += this.OnEndXfer;
            session.XferDone += this.OnXferDone;
            session.AcquireCompleted += this.OnAcquireCompleted;
            session.AcquireError += this.OnAcquireError;
            session.TwainStateChanged += this.OnTwainStateChanged;

            try {
                session.Acquire();

                while(!this._completed.IsSet) {
                    if(this._token.IsCancellationRequested) {
                        this._canceled = true;
                        break;
                    }
                    if(this._pump != null) {
                        this._pump();
                    }
                    this._completed.Wait(10);
                }

                if(this._canceled) {
                    throw new OperationCanceledException(this._token);
                }
                if(this._error != null) {
                    throw this._error;
                }

                return new TwainXAcquisitionResult(this._streams, this._imageInfos);
            } catch {
                this.DisposeStreams();
                throw;
            } finally {
                session.EndXfer -= this.OnEndXfer;
                session.XferDone -= this.OnXferDone;
                session.AcquireCompleted -= this.OnAcquireCompleted;
                session.AcquireError -= this.OnAcquireError;
                session.TwainStateChanged -= this.OnTwainStateChanged;

                try {
                    session.CloseDSM();
                } catch(Exception ex) {
                    Trace.TraceWarning("Saraff.TwainX: error while closing DSM after acquisition: " + ex);
                }
                this._completed.Dispose();
            }
        }

        private void OnXferDone(object sender, TwainX.XferDoneEventArgs e) {
            try {
                this._currentImageInfo = e.GetImageInfo();
            } catch(Exception ex) {
                this._error = ex;
                this._completed.Set();
            }
        }

        private void OnEndXfer(object sender, TwainX.EndXferEventArgs e) {
            try {
                Stream _source = e.Stream;
                if(_source != null) {
                    var _copy = new MemoryStream();
                    _source.CopyTo(_copy);
                    _copy.Position = 0;
                    lock(this._sync) {
                        this._streams.Add(_copy);
                        this._imageInfos.Add(this._currentImageInfo);
                    }
                    this._currentImageInfo = null;
                    if(this._options.Progress != null) {
                        try {
                            this._options.Progress.Report(new TwainXAcquisitionProgress(this._streams.Count, this._imageInfos[this._imageInfos.Count - 1]));
                        } catch(Exception ex) {
                            Trace.TraceWarning("Saraff.TwainX: progress handler failed: " + ex);
                        }
                    }
                }

                if(this._options.MaxImages.HasValue && this._streams.Count >= this._options.MaxImages.Value) {
                    this._maxReached = true;
                    this._completed.Set();
                }
            } catch(Exception ex) {
                this._error = ex;
                this._completed.Set();
            }
        }

        private void OnAcquireCompleted(object sender, EventArgs e) {
            if(this._maxReached || this._options.DisableAfterAcquire || !this._options.ShowUI) {
                this._completed.Set();
            }
        }

        private void OnAcquireError(object sender, TwainX.AcquireErrorEventArgs e) {
            this._error = e.Exception;
            this._completed.Set();
        }

        private void OnTwainStateChanged(object sender, TwainX.TwainStateEventArgs e) {
            if(this._streams.Count > 0 && (e.TwainState & TwainX.TwainStateFlag.DSEnabled) == 0) {
                this._completed.Set();
            }
        }

        private void DisposeStreams() {
            lock(this._sync) {
                foreach(Stream _stream in this._streams) {
                    try {
                        _stream.Dispose();
                    } catch {
                    }
                }
                this._streams.Clear();
                this._imageInfos.Clear();
            }
        }
    }
}
