using Tetris.Events;

namespace Tetris {
    /// <summary>
    /// Publié quand la direction de l'action "Move" change.
    /// <see cref="Direction"/> vaut -1 (côté négatif du 1D Axis), +1 (côté positif) ou 0 (relâché).
    /// Le gameplay gère lui-même la répétition (DAS/ARR) tant que la direction est maintenue.
    /// </summary>
    public readonly struct MoveInputEvent : IEvent {
        public readonly int Direction;

        public MoveInputEvent(int direction) {
            Direction = direction;
        }
    }

    /// <summary>
    /// Publié à l'appui (<see cref="IsPressed"/> = true) et au relâchement (false) de l'action "Down".
    /// </summary>
    public readonly struct DownInputEvent : IEvent {
        public readonly bool IsPressed;

        public DownInputEvent(bool isPressed) {
            IsPressed = isPressed;
        }
    }

    /// <summary>Publié à l'appui de l'action "Rotate".</summary>
    public readonly struct RotateInputEvent : IEvent { }

    /// <summary>Publié à l'appui de l'action "Pause".</summary>
    public readonly struct PauseInputEvent : IEvent { }

    /// <summary>Publié à l'appui de l'action "Misc". Réservé pour un usage futur.</summary>
    public readonly struct MiscInputEvent : IEvent { }
}
