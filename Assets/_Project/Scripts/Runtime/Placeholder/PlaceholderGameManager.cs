using Tetris.Events;
using UnityEngine;

namespace Tetris.Placeholder {
    /// <summary>
    /// PLACEHOLDER : spawn, gravité et déplacements pour tester le Board. À remplacer par le vrai Spawner.
    /// Écoute MoveInputEvent (déplacement d'une case) et DownInputEvent (descente rapide tant que maintenu).
    /// </summary>
    public class PlaceholderGameManager : MonoBehaviour {
        static readonly Vector2Int[][] Shapes = {
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },  // I
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },   // O
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },  // T
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },  // S
            new[] { new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0) },  // Z
            new[] { new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) }, // J
            new[] { new Vector2Int(1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) },  // L
        };

        static readonly Color[] Colors = {
            Color.cyan, Color.yellow, new Color(0.6f, 0f, 1f), Color.green, Color.red, Color.blue, new Color(1f, 0.5f, 0f),
        };

        [SerializeField] Board _board;
        [SerializeField] float _fallInterval = 0.8f;
        [SerializeField] float _softDropInterval = 0.05f;

        PlaceholderBloc _current;
        float _fallTimer;
        bool _isSoftDropping;
        int _totalLines;

        void OnEnable() {
            EventBus<MoveInputEvent>.Subscribe(OnMove);
            EventBus<DownInputEvent>.Subscribe(OnDown);
        }

        void OnDisable() {
            EventBus<MoveInputEvent>.Unsubscribe(OnMove);
            EventBus<DownInputEvent>.Unsubscribe(OnDown);
        }

        void Start() {
            CreateBackground();
            SpawnNext();
        }

        void OnMove(MoveInputEvent e) {
            if (_current != null && e.Direction != 0) {
                _current.TryMove(new Vector2Int(e.Direction, 0));
            }
        }

        void OnDown(DownInputEvent e) {
            _isSoftDropping = e.IsPressed;
        }

        void Update() {
            if (_current == null) return;

            _fallTimer += Time.deltaTime;
            if (_fallTimer < (_isSoftDropping ? _softDropInterval : _fallInterval)) return;

            _fallTimer = 0f;
            if (!_current.TryMove(Vector2Int.down)) {
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
            int index = Random.Range(0, Shapes.Length);
            var spawnPosition = new Vector2Int(_board.Width / 2 - 1, _board.Height - 2);

            _current = new GameObject("Bloc").AddComponent<PlaceholderBloc>();
            _current.Init(Shapes[index], Colors[index], _board, spawnPosition);
            _fallTimer = 0f;

            if (!_current.IsValid()) {
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
            spriteRenderer.sprite = PlaceholderBloc.GetCellSprite();
            spriteRenderer.color = new Color(0.12f, 0.12f, 0.15f);
            spriteRenderer.sortingOrder = -1;
        }
    }
}
