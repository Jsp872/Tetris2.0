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

    /// <summary>
    /// Publié quand des lignes sont supprimées : nouveau score, total de lignes et lignes supprimées par ce coup.
    /// </summary>
    public readonly struct ScoreChangedEvent : IEvent {
        public readonly int Score;
        public readonly int Lines;
        public readonly int LinesCleared;

        public ScoreChangedEvent(int score, int lines, int linesCleared) {
            Score = score;
            Lines = lines;
            LinesCleared = linesCleared;
        }
    }

    /// <summary>Publié quand la partie est perdue.</summary>
    public readonly struct GameOverEvent : IEvent { }

    /// <summary>Publié par le Spawner quand une nouvelle pièce apparaît.</summary>
    public readonly struct BlocSpawnedEvent : IEvent { }

    /// <summary>Publié quand une pièce est posée dans le Board.</summary>
    public readonly struct BlocLockedEvent : IEvent { }
}
