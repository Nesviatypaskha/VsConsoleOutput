using System;

namespace service.pipes
{
    public static class EventHandlerExtensions
    {
        public static void SafeInvoke<T>(this EventHandler<T> @event, object sender, T eventArgs) where T : EventArgs
        {
            if (@event != null)
            {
                @event(sender, eventArgs);
            }
        }
    }
}
