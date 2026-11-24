using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Saraff.TwainX;
using Xunit;

namespace Saraff.TwainX.Tests;

public sealed class AsyncAcquisitionTests
{
    [Fact]
    public void Runner_CollectsSinglePageAndClosesDsm() {
        var fake = new FakeSession();
        var payload = new byte[] { 1, 2, 3 };
        fake.OnAcquire = session => {
            session.RaiseEndXfer(new MemoryStream(payload));
            session.RaiseAcquireCompleted();
        };

        var result = new TwainXAcquisitionRunner().Run(
            fake,
            new TwainXAcquireOptions { ShowUI = false, DisableAfterAcquire = true },
            CancellationToken.None,
            null);

        try {
            Assert.Single(result.Streams);
            using var stream = new MemoryStream();
            result.Streams[0].CopyTo(stream);
            Assert.Equal(payload, stream.ToArray());
            Assert.True(fake.Closed);
        } finally {
            result.Dispose();
        }
    }

    [Fact]
    public void Runner_StopsAfterMaxImages() {
        var fake = new FakeSession();
        fake.OnAcquire = session => {
            session.RaiseEndXfer(new MemoryStream(new byte[] { 1 }));
            session.RaiseEndXfer(new MemoryStream(new byte[] { 2 }));
            session.RaiseAcquireCompleted();
        };

        var result = new TwainXAcquisitionRunner().Run(
            fake,
            new TwainXAcquireOptions {
                ShowUI = true,
                DisableAfterAcquire = false,
                MaxImages = 2
            },
            CancellationToken.None,
            null);

        try {
            Assert.Equal(2, result.Streams.Count);
            Assert.True(fake.Closed);
        } finally {
            result.Dispose();
        }
    }

    [Fact]
    public void Runner_ReportsAcquireError() {
        var fake = new FakeSession();
        fake.OnAcquire = session => session.RaiseAcquireError(new InvalidOperationException("boom"));

        Assert.Throws<TwainException>(() => new TwainXAcquisitionRunner().Run(
            fake,
            new TwainXAcquireOptions(),
            CancellationToken.None,
            null));

        Assert.True(fake.Closed);
    }

    [Fact]
    public void Runner_ObservesCancellation() {
        var fake = new FakeSession();
        fake.OnAcquire = session => { };
        using var cancellation = new CancellationTokenSource(50);

        Assert.Throws<OperationCanceledException>(() => new TwainXAcquisitionRunner().Run(
            fake,
            new TwainXAcquireOptions(),
            cancellation.Token,
            null));

        Assert.True(fake.Closed);
    }

    [Fact]
    public void SessionThread_RunsActionsOnDedicatedThread() {
        using var thread = new TwainXSessionThread("test-session-thread");
        var completed = new ManualResetEventSlim(false);
        var actionThreadId = 0;
        var actionApartment = ApartmentState.Unknown;

        thread.Post(() => {
            actionThreadId = Thread.CurrentThread.ManagedThreadId;
            actionApartment = Thread.CurrentThread.GetApartmentState();
            completed.Set();
        });

        Assert.True(completed.Wait(TimeSpan.FromSeconds(5)));
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, actionThreadId);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Equal(ApartmentState.STA, actionApartment);
        }
    }

    private sealed class FakeSession : ITwainXSession
    {
        public Action<FakeSession> OnAcquire { get; set; }

        public bool Closed { get; private set; }

        public bool ShowUI { get; set; }

        public bool ModalUI { get; set; }

        public IntPtr Parent { get; set; }

        public string DsmPath { get; set; }

        public bool DisableAfterAcquire { get; set; }

        public event EventHandler AcquireCompleted;

        public event EventHandler<TwainX.EndXferEventArgs> EndXfer;

#pragma warning disable 0067
        public event EventHandler<TwainX.XferDoneEventArgs> XferDone;
#pragma warning restore 0067

        public event EventHandler<TwainX.AcquireErrorEventArgs> AcquireError;

        public event EventHandler<TwainX.TwainStateEventArgs> TwainStateChanged;

        public void Acquire() => this.OnAcquire?.Invoke(this);

        public bool CloseDSM() {
            this.Closed = true;
            return true;
        }

        public void Dispose() {
        }

        public void RaiseEndXfer(Stream stream) => this.EndXfer?.Invoke(this, TwainX.EndXferEventArgs.FromStream(stream));

        public void RaiseAcquireCompleted() => this.AcquireCompleted?.Invoke(this, EventArgs.Empty);

        public void RaiseAcquireError(Exception exception) => this.AcquireError?.Invoke(this, new TwainX.AcquireErrorEventArgs(new TwainException(exception.Message, exception)));

        public void RaiseState(TwainX.TwainStateFlag state) => this.TwainStateChanged?.Invoke(this, new TwainX.TwainStateEventArgs(state));
    }
}
