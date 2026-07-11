using System;
using System.Collections.Generic;
using System.Reflection;

namespace AzurArchive.Core;

public readonly struct Subscription(WeakReference? subcriber, MethodInfo handler) : IEquatable<Subscription> {
    public readonly WeakReference? SubscriberWeakReference = subcriber;
    public readonly MethodInfo Handler = handler;
    public bool Equals(Subscription other) => SubscriberWeakReference == other.SubscriberWeakReference && Handler == other.Handler;
    public override bool Equals(object? obj) => obj is Subscription other && Equals(other);
    public override int GetHashCode() => SubscriberWeakReference?.GetHashCode() ?? 0 ^ Handler.GetHashCode();
}
public sealed class WeakEventHandler<TEventArgs> : WeakEventHandler where TEventArgs : EventArgs {
    public static WeakEventHandler<TEventArgs> operator +(WeakEventHandler<TEventArgs> source, Action<object?, TEventArgs> handler) {
        source.Connect(handler);
        return source;
    }
    public static WeakEventHandler<TEventArgs> operator -(WeakEventHandler<TEventArgs> source, Action<object?, TEventArgs> handler) {
        source.Disconnect(handler);
        return source;
    }
    public void Connect(Action<object?, TEventArgs> handler) {
        if (handler.Target == null) {
            _eventHandlers.Add(new(null, handler.GetMethodInfo()));
        }
        else {
            _eventHandlers.Add(new(new(handler.Target), handler.GetMethodInfo()));
        }
    }
    public void Disconnect(Action<object?, TEventArgs> handler) {
        object? handlerTarget = handler.Target;
        string methodName = handler.GetMethodInfo().Name;
        for (int n = _eventHandlers.Count - 1; n >= 0; n--) {
            Subscription current = _eventHandlers[n];
            if (current.SubscriberWeakReference != null && !current.SubscriberWeakReference.IsAlive) {
                _eventHandlers.RemoveAt(n);
                continue;
            }
            if (current.SubscriberWeakReference?.Target == handlerTarget && current.Handler.Name == methodName) {
                _eventHandlers.RemoveAt(n);
                break;
            }
        }
    }
    public void Invoke(object? sender, TEventArgs? args) {
        List<(object? subscriber, MethodInfo handler)> toRaise = [];
        List<int> toRemove = [];
        for (int i = 0; i < _eventHandlers.Count; i++) {
            Subscription subscription = _eventHandlers[i];
            // Null subcriber weak reference
            bool isStatic = subscription.SubscriberWeakReference == null;
            if (isStatic) {
                toRaise.Add((null, subscription.Handler));
            }
            else {
                // Null subcriber target
                object? subscriber = subscription.SubscriberWeakReference?.Target;
                if (subscriber == null) {
                    toRemove.Add(i);
                }
                else {
                    toRaise.Add((subscriber, subscription.Handler));
                }
            }
        }
        for (int n = toRemove.Count - 1; n >= 0; n--) {
            int subscriptionIndex = toRemove[n];
            _eventHandlers.RemoveAt(subscriptionIndex);
        }
        for (int i = 0; i < toRaise.Count; i++) {
            (object? subcriber, MethodInfo handler) = toRaise[i];
            handler.Invoke(subcriber, [sender, args]);
        }
    }
}
public class WeakEventHandler {
    protected readonly List<Subscription> _eventHandlers = [];
    public int Count => _eventHandlers.Count;
    public static WeakEventHandler operator +(WeakEventHandler source, Action<object?, EventArgs> handler) {
        source.Connect(handler);
        return source;
    }
    public static WeakEventHandler operator -(WeakEventHandler source, Action<object?, EventArgs> handler) {
        source.Disconnect(handler);
        return source;
    }
    public void Connect(Action<object?, EventArgs> handler) {
        if (handler.Target == null) {
            _eventHandlers.Add(new(null, handler.GetMethodInfo()));
        }
        else {
            _eventHandlers.Add(new(new(handler.Target), handler.GetMethodInfo()));
        }
    }
    public void Disconnect(Action<object?, EventArgs> handler) {
        object? handlerTarget = handler.Target;
        string methodName = handler.GetMethodInfo().Name;
        for (int n = _eventHandlers.Count - 1; n >= 0; n--) {
            Subscription current = _eventHandlers[n];
            if (current.SubscriberWeakReference != null && !current.SubscriberWeakReference.IsAlive) {
                _eventHandlers.RemoveAt(n);
                continue;
            }
            if (current.SubscriberWeakReference?.Target == handlerTarget && current.Handler.Name == methodName) {
                _eventHandlers.RemoveAt(n);
                break;
            }
        }
    }
    public void Invoke(object? sender, EventArgs? args) {
        List<(object? subscriber, MethodInfo handler)> toRaise = [];
        List<int> toRemove = [];
        for (int i = 0; i < _eventHandlers.Count; i++) {
            Subscription subscription = _eventHandlers[i];
            bool isStatic = subscription.SubscriberWeakReference == null;
            if (isStatic) {
                toRaise.Add((null, subscription.Handler));
            }
            else {
                object? subscriber = subscription.SubscriberWeakReference?.Target;
                if (subscriber == null) {
                    toRemove.Add(i);
                }
                else {
                    toRaise.Add((subscriber, subscription.Handler));
                }
            }
        }
        for (int n = toRemove.Count - 1; n >= 0; n--) {
            int subscriptionIndex = toRemove[n];
            _eventHandlers.RemoveAt(subscriptionIndex);
        }
        for (int i = 0; i < toRaise.Count; i++) {
            (object? subcriber, MethodInfo handler) = toRaise[i];
            handler.Invoke(subcriber, [sender, args]);
        }
    }
}