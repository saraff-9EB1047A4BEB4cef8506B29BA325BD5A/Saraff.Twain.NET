using System;

#if TWAINX
namespace Saraff.TwainX {
#else
namespace Saraff.Twain {
#endif

    [Flags]
    internal enum TwainSessionFlags {
        None = 0,
        DsmOpen = 0x1,
        DsOpen = 0x2,
        DsEnabled = 0x4,
        DsReady = 0x8
    }

    /// <summary>
    /// Tracks the TWAIN session state without depending on the platform-specific component.
    /// </summary>
    internal sealed class TwainSessionState {
        private int _flags;

        public int Flags => this._flags;

        public bool IsDsmOpen => (this._flags & (int)TwainSessionFlags.DsmOpen) != 0;

        public bool IsDsOpen => (this._flags & (int)TwainSessionFlags.DsOpen) != 0;

        public bool IsDsEnabled => (this._flags & (int)TwainSessionFlags.DsEnabled) != 0;

        public bool IsDsReady => (this._flags & (int)TwainSessionFlags.DsReady) != 0;

        public void OpenDsm() => this._flags |= (int)TwainSessionFlags.DsmOpen;

        public void CloseDsm() => this._flags &= ~(int)TwainSessionFlags.DsmOpen;

        public void OpenDs() => this._flags |= (int)TwainSessionFlags.DsOpen;

        public void CloseDs() => this._flags &= ~(int)TwainSessionFlags.DsOpen;

        public void EnableDs() {
            if(this.IsDsReady) {
                this.ClearReady();
            } else {
                this._flags |= (int)TwainSessionFlags.DsEnabled;
            }
        }

        public void SetEnabledAndReady() => this._flags |= (int)(TwainSessionFlags.DsEnabled | TwainSessionFlags.DsReady);

        public void DisableDs() => this._flags &= ~(int)TwainSessionFlags.DsEnabled;

        public void ClearReady() => this._flags &= ~(int)TwainSessionFlags.DsReady;
    }
}
