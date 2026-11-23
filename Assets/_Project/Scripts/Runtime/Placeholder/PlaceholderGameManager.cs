using Tetris.Events;
using UnityEngine;

namespace Tetris.Placeholder {
    /// <summary>
    /// PLACEHOLDER : gravité, score et inputs pour tester Board, Bloc, Spawner et HUD. À remplacer par le GameManager.
    /// Move : déplacer, Down : descente rapide, Rotate : tourner, Pause : pause / reprise.
    /// </summary>
    public class PlaceholderGameManager : MonoBehaviour {
        static readonly int[] LineScores = { 0, 100, 300, 500, 800 };

        [SerializeField] Board _board;
        [SerializeField] Spawner _spawner;
        [SerializeField] HUD _hud;
        [SerializeField] float _fallInterval = 0.8f;
        [SerializeField] float _softDropInterval = 0.05f;

        Bloc _current;
        float _fallTimer;
        bool _isSoftDropping;
        bool _isPaused;
        bool _isGameOver;
        int _score;
        int _lines;

        void OnEnable() {
            EventBus<MoveInputEvent>.Subscribe(OnMove);
            EventBus<DownInputEvent>.Subscribe(OnDown);
            EventBus<RotateInputEvent>.Subscribe(OnRotate);
            EventBus<PauseInputEvent>.Subscribe(OnPause);
        }

        void OnDisable() {
            EventBus<MoveInputEvent>.Unsubscribe(OnMove);
            EventBus<DownInputEvent>.Unsubscribe(OnDown);
            EventBus<RotateInputEvent>.Unsubscribe(OnRotate);
            EventBus<PauseInputEvent>.Unsubscribe(OnPause);
            Time.timeScale = 1f;
        }

        void Start() {
            _hud.Show(_score, _lines);
            SpawnNext();
        }

        void OnPause(PauseInputEvent e) {
            if (_isGameOver) return;

            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;
            _hud.SetPaused(_isPaused);
        }

        void OnMove(MoveInputEvent e) {
            if (_isPaused || _current == null || e.Direction == 0) return;

            var direction = new Vector2Int(e.Direction, 0);
            if (_current.CanMove(direction)) {
                _current.Move(direction);
            }
        }

        void OnDown(DownInputEvent e) {
            _isSoftDropping = e.IsPressed;
        }

        void OnRotate(RotateInputEvent e) {
            if (!_isPaused && _current != null && _current.CanRotate()) {
                _current.Rotate();
            }
        }

        void Update() {
            if (_current == null) return;

            _fallTimer += Time.deltaTime;
            if (_fallTimer < (_isSoftDropping ? _softDropInterval : _fallInterval)) return;

            _fallTimer = 0f;
            if (_current.CanMove(Vector2Int.down)) {
                _current.Move(Vector2Int.down);
            }
            else {
                LockCurrent();
            }
        }

        void LockCurrent() {
            _current.Lock();
            _current = null;

            int cleared = _board.ClearFullRows();
            if (cleared > 0) {
                _lines += cleared;
                _score += LineScores[Mathf.Min(cleared, LineScores.Length - 1)];
                _hud.Show(_score, _lines);
                EventBus<ScoreChangedEvent>.Publish(new ScoreChangedEvent(_score, _lines, cleared));
            }

            SpawnNext();
        }

        void SpawnNext() {
            _current = _spawner.SpawnNext();
            _fallTimer = 0f;

            if (!_current.CanMove(Vector2Int.zero)) {
                Destroy(_current.gameObject);
                _current = null;
                _isGameOver = true;
                _hud.ShowGameOver();
                EventBus<GameOverEvent>.Publish(new GameOverEvent());
            }
        }
    }
}
