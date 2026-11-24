using System;

namespace Saraff.TwainX {

    internal sealed class TwainXSessionAdapter : ITwainXSession {
        private readonly TwainX _twain = new TwainX();

        public bool ShowUI {
            get => this._twain.ShowUI;
            set => this._twain.ShowUI = value;
        }

        public bool ModalUI {
            get => this._twain.ModalUI;
            set => this._twain.ModalUI = value;
        }

        public IntPtr Parent {
            get => this._twain.Parent;
            set => this._twain.Parent = value;
        }

        public string DsmPath {
            get => this._twain.DsmPath;
            set => this._twain.DsmPath = value;
        }

        public bool DisableAfterAcquire {
            get => this._twain.DisableAfterAcquire;
            set => this._twain.DisableAfterAcquire = value;
        }

        public void Acquire() => this._twain.Acquire();

        public bool CloseDSM() => this._twain.CloseDSM();

        public void Dispose() => this._twain.Dispose();

        public event EventHandler AcquireCompleted {
            add => this._twain.AcquireCompleted += value;
            remove => this._twain.AcquireCompleted -= value;
        }

        public event EventHandler<TwainX.EndXferEventArgs> EndXfer {
            add => this._twain.EndXfer += value;
            remove => this._twain.EndXfer -= value;
        }

        public event EventHandler<TwainX.XferDoneEventArgs> XferDone {
            add => this._twain.XferDone += value;
            remove => this._twain.XferDone -= value;
        }

        public event EventHandler<TwainX.AcquireErrorEventArgs> AcquireError {
            add => this._twain.AcquireError += value;
            remove => this._twain.AcquireError -= value;
        }

        public event EventHandler<TwainX.TwainStateEventArgs> TwainStateChanged {
            add => this._twain.TwainStateChanged += value;
            remove => this._twain.TwainStateChanged -= value;
        }
    }
}
