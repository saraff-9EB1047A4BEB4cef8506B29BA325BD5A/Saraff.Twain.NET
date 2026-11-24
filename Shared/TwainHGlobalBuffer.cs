using System;
using System.Runtime.InteropServices;

#if TWAINX
namespace Saraff.TwainX {
#else
namespace Saraff.Twain {
#endif

    /// <summary>
    /// Owns an unmanaged block allocated by <see cref="Marshal.AllocHGlobal(int)"/>.
    /// </summary>
    internal sealed class TwainHGlobalBuffer : SafeHandle {
        private TwainHGlobalBuffer() : base(IntPtr.Zero, true) {
        }

        public static TwainHGlobalBuffer Alloc(int size) {
            var _buffer = new TwainHGlobalBuffer();
            _buffer.SetHandle(Marshal.AllocHGlobal(size));
            return _buffer;
        }

        public static TwainHGlobalBuffer FromPointer(IntPtr pointer) {
            var _buffer = new TwainHGlobalBuffer();
            _buffer.SetHandle(pointer);
            return _buffer;
        }

        public override bool IsInvalid => this.handle == IntPtr.Zero;

        protected override bool ReleaseHandle() {
            Marshal.FreeHGlobal(this.handle);
            return true;
        }
    }
}
