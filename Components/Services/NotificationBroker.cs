using FitnessApp.Models;

namespace FitnessApp.Services;

public sealed class NotificationBroker
{
    private readonly object _gate = new();
    private readonly Dictionary<int, List<Action<Notification>>> _subscribers = new();

    public IDisposable Subscribe(int userId, Action<Notification> handler)
    {
        lock (_gate)
        {
            if (!_subscribers.TryGetValue(userId, out var list))
            {
                list = new List<Action<Notification>>();
                _subscribers[userId] = list;
            }
            list.Add(handler);
        }
        return new Subscription(this, userId, handler);
    }

    private void Unsubscribe(int userId, Action<Notification> handler)
    {
        lock (_gate)
        {
            if (_subscribers.TryGetValue(userId, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0) _subscribers.Remove(userId);
            }
        }
    }

    public void Publish(Notification notification)
    {
        Action<Notification>[] handlers;
        lock (_gate)
        {
            if (!_subscribers.TryGetValue(notification.UserId, out var list)) return;
            handlers = list.ToArray();
        }
        foreach (var handler in handlers)
        {
            try { handler(notification); } catch { }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly NotificationBroker _broker;
        private readonly int _userId;
        private readonly Action<Notification> _handler;
        private bool _disposed;

        public Subscription(NotificationBroker broker, int userId, Action<Notification> handler)
        {
            _broker = broker;
            _userId = userId;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _broker.Unsubscribe(_userId, _handler);
        }
    }
}
