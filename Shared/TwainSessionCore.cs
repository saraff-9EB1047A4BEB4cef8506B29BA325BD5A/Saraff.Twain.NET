#if TWAINX
namespace Saraff.TwainX {

    public sealed partial class TwainX {
#else
namespace Saraff.Twain {

    public sealed partial class Twain32 {
#endif

        private readonly TwainSessionState _session = new TwainSessionState();
        private int _lastSessionFlags;

        private void _SyncTwainState() {
            int _flags = this._session.Flags;
            if(_flags != this._lastSessionFlags) {
                this._lastSessionFlags = _flags;
                TwainEventDispatcher.Raise(this, this.TwainStateChanged, new TwainStateEventArgs((TwainStateFlag)_flags), nameof(this.TwainStateChanged));
            }
        }
    }
}
