namespace FitnessApp.Services;

public sealed record ChatEvent(int ConversationId, int SenderId, int RecipientId, ChatMessageDto Message);

public sealed class ChatBroker
{
    private readonly object _gate = new();
    private readonly Dictionary<int, List<Action<ChatEvent>>> _subscribers = new();

    public IDisposable Subscribe(int userId, Action<ChatEvent> handler)
    {
        lock (_gate)
        {
            if (!_subscribers.TryGetValue(userId, out var list))
            {
                list = new List<Action<ChatEvent>>();
                _subscribers[userId] = list;
            }
            list.Add(handler);
        }
        return new Subscription(this, userId, handler);
    }

    private void Unsubscribe(int userId, Action<ChatEvent> handler)
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

    public void Publish(ChatEvent message)
    {
        NotifyUser(message.SenderId, message);
        if (message.RecipientId != message.SenderId)
            NotifyUser(message.RecipientId, message);
    }

    private void NotifyUser(int userId, ChatEvent message)
    {
        Action<ChatEvent>[] handlers;
        lock (_gate)
        {
            if (!_subscribers.TryGetValue(userId, out var list)) return;
            handlers = list.ToArray();
        }
        foreach (var handler in handlers)
        {
            try { handler(message); } catch { }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly ChatBroker _broker;
        private readonly int _userId;
        private readonly Action<ChatEvent> _handler;
        private bool _disposed;

        public Subscription(ChatBroker broker, int userId, Action<ChatEvent> handler)
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
