using Tetris.Events;
using UnityEngine;

namespace Tetris.Placeholder {
    /// <summary>
    /// PLACEHOLDER : spawn, gravité et inputs pour tester Board et Bloc. À remplacer par le Spawner et le GameManager.
    /// Move : déplacer, Down : descente rapide, Rotate : tourner, Pause : pause / reprise, Misc : log de debug.
    /// </summary>
    public class PlaceholderGameManager : MonoBehaviour {
        [SerializeField] Board _board;
        [SerializeField] Bloc _blocPrefab;
        [SerializeField] BlocSO[] _blocs;
        [SerializeField] Sprite _backgroundSprite;
        [SerializeField] float _fallInterval = 0.8f;
        [SerializeField] float _softDropInterval = 0.05f;

        Bloc _current;
        float _fallTimer;
        bool _isSoftDropping;
        bool _isPaused;
        int _totalLines;

        void OnEnable() {
            EventBus<MoveInputEvent>.Subscribe(OnMove);
            EventBus<DownInputEvent>.Subscribe(OnDown);
            EventBus<RotateInputEvent>.Subscribe(OnRotate);
            EventBus<PauseInputEvent>.Subscribe(OnPause);
            EventBus<MiscInputEvent>.Subscribe(OnMisc);
        }

        void OnDisable() {
            EventBus<MoveInputEvent>.Unsubscribe(OnMove);
            EventBus<DownInputEvent>.Unsubscribe(OnDown);
            EventBus<RotateInputEvent>.Unsubscribe(OnRotate);
            EventBus<PauseInputEvent>.Unsubscribe(OnPause);
            EventBus<MiscInputEvent>.Unsubscribe(OnMisc);
            Time.timeScale = 1f;
        }

        void Start() {
            CreateBackground();
            SpawnNext();
        }

        void OnPause(PauseInputEvent e) {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;
            Debug.Log(_isPaused ? "Pause" : "Reprise");
        }

        void OnMisc(MiscInputEvent e) {
            Debug.Log("Misc");
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
                _totalLines += cleared;
                Debug.Log($"{cleared} ligne(s) supprimée(s), total : {_totalLines}");
            }

            SpawnNext();
        }

        void SpawnNext() {
            _current = Instantiate(_blocPrefab);
            _current.Init(_blocs[Random.Range(0, _blocs.Length)], _board);
            _fallTimer = 0f;

            if (!_current.CanMove(Vector2Int.zero)) {
                Debug.Log("Game Over");
                Destroy(_current.gameObject);
                _current = null;
            }
        }

        void CreateBackground() {
            var background = new GameObject("Background");
            background.transform.SetParent(_board.transform, false);
            background.transform.localPosition = new Vector3((_board.Width - 1) / 2f, (_board.Height - 1) / 2f);
            background.transform.localScale = new Vector3(_board.Width, _board.Height, 1f);

            var spriteRenderer = background.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = _backgroundSprite;
            spriteRenderer.color = new Color(0.12f, 0.12f, 0.15f);
            spriteRenderer.sortingOrder = -1;
        }
    }
}
