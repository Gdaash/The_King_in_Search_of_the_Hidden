using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.UI
{
    public enum NotificationKind { Normal, Positive, Negative }

    public readonly struct NotificationPart
    {
        public readonly string Text;
        public readonly NotificationKind Kind;
        public readonly Sprite Icon;
        public readonly ResourceType Resource;
        public readonly int Delta;
        public NotificationPart(string text, NotificationKind kind, Sprite icon)
        { Text = text; Kind = kind; Icon = icon; Resource = null; Delta = 0; }
        public NotificationPart(ResourceType resource, int delta)
        {
            Resource = resource; Delta = delta; Icon = resource.resourceIcon;
            Text = (delta > 0 ? "+" : "") + delta;
            Kind = delta > 0 ? NotificationKind.Positive : NotificationKind.Negative;
        }
    }

    public static class GameNotifications
    {
        public static event Action<IReadOnlyList<NotificationPart>> Posted;
        public static event Action<string, float, IReadOnlyList<NotificationPart>> CoalescedPosted;
        public static event Action<Health> UnitDied;
        private static ActionScope current;
        private static TransferScope transfer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Posted = null; CoalescedPosted = null; UnitDied = null; current = null; transfer = null; }

        // Moving an existing worker/cart out of storage is not a gain or loss.
        public static IDisposable BeginTransfer(ResourceType first, ResourceType second = null) => new TransferScope(first, second);

        private sealed class TransferScope : IDisposable
        {
            private readonly TransferScope parent;
            private readonly ResourceType first, second;
            private bool disposed;
            public TransferScope(ResourceType first, ResourceType second)
            { this.first = first; this.second = second; parent = transfer; transfer = this; }
            public bool Contains(ResourceType type) => type == first || type == second || (parent != null && parent.Contains(type));
            public void Dispose() { if (disposed) return; disposed = true; transfer = parent; }
        }

        // Explicit synchronous action boundaries keep unrelated events separate, even in the same frame.
        public static IDisposable BeginAction() => new ActionScope();
        public static IDisposable BeginPorterSpawn() => new ActionScope("porter.spawn", 1f);

        private sealed class ActionScope : IDisposable
        {
            private readonly ActionScope parent;
            private readonly List<NotificationPart> parts = new();
            private bool disposed;
            private readonly string mergeKey;
            private readonly float mergeWindow;
            public ActionScope(string key = null, float window = 0)
            { parent = current; current = this; mergeKey = key; mergeWindow = window; }
            public void Add(NotificationPart part) => parts.Add(part);
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                current = parent;
                if (mergeKey != null && parts.Count > 0) CoalescedPosted?.Invoke(mergeKey, mergeWindow, parts.ToArray());
                else if (parent != null) parent.parts.AddRange(parts);
                else if (parts.Count > 0)
                {
                    if (parts.TrueForAll(part => part.Resource != null))
                        CoalescedPosted?.Invoke("resources", 1f, parts.ToArray());
                    else Posted?.Invoke(parts.ToArray());
                }
            }
        }

        public static void Post(string text, NotificationKind kind = NotificationKind.Normal, Sprite icon = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var part = new NotificationPart(text, kind, icon);
            if (current != null) current.Add(part);
            else Posted?.Invoke(new[] { part });
        }

        public static void Resource(ResourceType resource, int delta)
        {
            if (resource == null || delta == 0 || !resource.notifyResourceChanges) return;
            if (transfer != null && transfer.Contains(resource)) return;
            var part = new NotificationPart(resource, delta);
            if (current != null) current.Add(part);
            else CoalescedPosted?.Invoke("resources", 1f, new[] { part });
        }

        public static void Death(Health health) => UnitDied?.Invoke(health);
    }
}
