using System;
using System.Diagnostics;

#if TWAINX
namespace Saraff.TwainX {
#else
namespace Saraff.Twain {
#endif

    /// <summary>
    /// Invokes user event handlers without allowing one handler to break the TWAIN session.
    /// </summary>
    internal static class TwainEventDispatcher {
        public static void Raise(object sender, EventHandler handler, EventArgs args, string eventName) {
            if(handler == null) {
                return;
            }
            foreach(EventHandler _handler in handler.GetInvocationList()) {
                try {
                    _handler(sender, args);
                } catch(Exception ex) {
                    Trace.TraceError("Saraff.Twain: the '" + eventName + "' event handler failed: " + ex);
                }
            }
        }

        public static void Raise<T>(object sender, EventHandler<T> handler, T args, string eventName) where T : EventArgs {
            if(handler == null) {
                return;
            }
            foreach(EventHandler<T> _handler in handler.GetInvocationList()) {
                try {
                    _handler(sender, args);
                } catch(Exception ex) {
                    Trace.TraceError("Saraff.Twain: the '" + eventName + "' event handler failed: " + ex);
                }
            }
        }
    }
}
