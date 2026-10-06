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
        public NotificationPart(string text, NotificationKind kind, Sprite icon)
        { Text = text; Kind = kind; Icon = icon; }
    }

    public static class GameNotifications
    {
        public static event Action<IReadOnlyList<NotificationPart>> Posted;
        public static event Action<Health> UnitDied;
        private static ActionScope current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Posted = null; UnitDied = null; current = null; }

        // Explicit synchronous action boundaries keep unrelated events separate, even in the same frame.
        public static IDisposable BeginAction() => new ActionScope();

        private sealed class ActionScope : IDisposable
        {
            private readonly ActionScope parent;
            private readonly List<NotificationPart> parts = new();
            private bool disposed;
            public ActionScope() { parent = current; current = this; }
            public void Add(NotificationPart part) => parts.Add(part);
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                current = parent;
                if (parent != null) parent.parts.AddRange(parts);
                else if (parts.Count > 0) Posted?.Invoke(parts.ToArray());
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
            if (resource == null || delta == 0) return;
            Post((delta > 0 ? "+" : "") + delta,
                delta > 0 ? NotificationKind.Positive : NotificationKind.Negative, resource.resourceIcon);
        }

        public static void Death(Health health) => UnitDied?.Invoke(health);
    }
}
