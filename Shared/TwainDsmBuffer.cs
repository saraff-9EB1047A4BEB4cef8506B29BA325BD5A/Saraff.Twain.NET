using System;

#if TWAINX
namespace Saraff.TwainX {
#else
namespace Saraff.Twain {
#endif

    /// <summary>
    /// Owns a memory block allocated by the TWAIN data source manager.
    /// </summary>
    internal sealed class TwainDsmBuffer : IDisposable {
        private IntPtr _handle;
        private bool _disposed;

        private TwainDsmBuffer(IntPtr handle) {
            this._handle = handle;
        }

        public IntPtr Handle => this._handle;

        public bool IsInvalid => this._handle == IntPtr.Zero;

        public static TwainDsmBuffer Alloc(int size) => new TwainDsmBuffer(_Memory.Alloc(size));

        public static TwainDsmBuffer FromPointer(IntPtr handle) => new TwainDsmBuffer(handle);

        public void Dispose() {
            if(this._disposed) {
                return;
            }
            this._disposed = true;
            if(this._handle != IntPtr.Zero) {
                _Memory.Free(this._handle);
                this._handle = IntPtr.Zero;
            }
        }
    }
}
