using System.Collections.Generic;
using Tetris.Events;

namespace Tetris {
    /// <summary>
    /// Publié par le Spawner à chaque nouvelle pièce, quand la file des prochaines pièces change.
    /// <see cref="NextBlocs"/> va de la plus proche (index 0) à la plus lointaine.
    /// </summary>
    public readonly struct NextBlocsChangedEvent : IEvent {
        public readonly IReadOnlyList<BlocSO> NextBlocs;

        public NextBlocsChangedEvent(IReadOnlyList<BlocSO> nextBlocs) {
            NextBlocs = nextBlocs;
        }
    }
}
