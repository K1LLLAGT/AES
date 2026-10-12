using System.Collections.Concurrent;
using System.Reactive.Subjects;
using AES.Contracts;

namespace AES.Core;

public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, object> _subjects = new();

    public void Publish<TEvent>(TEvent evt) where TEvent : class
    {
        if (_subjects.TryGetValue(typeof(TEvent), out var subjectObj))
            ((Subject<TEvent>)subjectObj).OnNext(evt);
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        var subject = (Subject<TEvent>)_subjects.GetOrAdd(
            typeof(TEvent), _ => new Subject<TEvent>());
        return subject.Subscribe(handler);
    }
}