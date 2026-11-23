using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tetris.Events {
    /// <summary>
    /// Garde une trace de chaque canal <see cref="EventBus{T}"/> créé pour pouvoir tous les vider.
    /// Nécessaire car le domain reload est désactivé (Enter Play Mode Options) :
    /// sans ça, les abonnés d'une session de Play Mode survivraient à la suivante.
    /// </summary>
    public static class EventBusRegistry {
        static readonly List<Action> _clearActions = new();

        internal static void Register(Action clearAction) {
            _clearActions.Add(clearAction);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll() {
            for (int i = 0; i < _clearActions.Count; i++) {
                _clearActions[i]();
            }
        }
    }
}
