using System;

namespace Saraff.TwainX {

    internal interface ITwainXSession : IDisposable {
        bool ShowUI { get; set; }

        bool ModalUI { get; set; }

        IntPtr Parent { get; set; }

        string DsmPath { get; set; }

        bool DisableAfterAcquire { get; set; }

        void Acquire();

        bool CloseDSM();

        event EventHandler AcquireCompleted;

        event EventHandler<TwainX.EndXferEventArgs> EndXfer;

        event EventHandler<TwainX.XferDoneEventArgs> XferDone;

        event EventHandler<TwainX.AcquireErrorEventArgs> AcquireError;

        event EventHandler<TwainX.TwainStateEventArgs> TwainStateChanged;
    }
}
