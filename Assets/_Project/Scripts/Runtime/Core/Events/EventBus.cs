using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetris.Events {
    /// <summary>
    /// Bus d'événements global et typé : un canal indépendant par type d'événement <typeparamref name="T"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// void OnEnable()  => EventBus&lt;MoveInputEvent&gt;.Subscribe(OnMove);
    /// void OnDisable() => EventBus&lt;MoveInputEvent&gt;.Unsubscribe(OnMove);
    /// void OnMove(MoveInputEvent e) { ... }
    ///
    /// EventBus&lt;MoveInputEvent&gt;.Publish(new MoveInputEvent(-1));
    /// </code>
    /// </example>
    /// <remarks>
    /// - Les événements sont des structs : aucune allocation à la publication.
    /// - Un handler qui lève une exception est loggé sans empêcher les autres d'être appelés.
    /// - S'abonner / se désabonner pendant un Publish est sans risque : le changement
    ///   s'applique à partir du Publish suivant.
    /// - Tous les canaux sont vidés à chaque entrée en Play Mode (domain reload désactivé).
    /// </remarks>
    public static class EventBus<T> where T : struct, IEvent {
        static readonly List<Action<T>> _handlers = new();
        static Action<T>[] _snapshot = Array.Empty<Action<T>>();
        static bool _isDirty;

        static EventBus() {
            EventBusRegistry.Register(Clear);
        }

        public static int SubscriberCount => _handlers.Count;

        public static void Subscribe(Action<T> handler) {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (_handlers.Contains(handler)) return;

            _handlers.Add(handler);
            _isDirty = true;
        }

        public static void Unsubscribe(Action<T> handler) {
            if (handler != null && _handlers.Remove(handler)) {
                _isDirty = true;
            }
        }

        public static void Publish(T evt) {
            if (_isDirty) {
                _snapshot = _handlers.ToArray();
                _isDirty = false;
            }

            Action<T>[] handlers = _snapshot;
            for (int i = 0; i < handlers.Length; i++) {
                try {
                    handlers[i](evt);
                }
                catch (Exception exception) {
                    Debug.LogException(exception);
                }
            }
        }

        public static void Clear() {
            _handlers.Clear();
            _snapshot = Array.Empty<Action<T>>();
            _isDirty = false;
        }
    }
}
