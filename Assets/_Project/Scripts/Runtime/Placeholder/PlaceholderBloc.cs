using UnityEngine;

namespace Tetris.Placeholder {
    /// <summary>
    /// PLACEHOLDER : pièce active minimale pour tester le Board. À remplacer par le vrai Bloc.
    /// </summary>
    public class PlaceholderBloc : MonoBehaviour {
        static Sprite _cellSprite;

        Board _board;
        Vector2Int _position;
        Vector2Int[] _cells;
        Transform[] _visuals;

        public void Init(Vector2Int[] shape, Color color, Board board, Vector2Int position) {
            _board = board;
            _position = position;
            _cells = shape;
            _visuals = new Transform[_cells.Length];

            for (int i = 0; i < _cells.Length; i++) {
                var cell = new GameObject("Cell");
                cell.transform.SetParent(transform, false);
                cell.transform.localScale = Vector3.one * 0.95f;

                var spriteRenderer = cell.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = GetCellSprite();
                spriteRenderer.color = color;

                _visuals[i] = cell.transform;
            }

            UpdateVisuals();
        }

        public bool IsValid() {
            return Fits(_position, _cells);
        }

        public bool TryMove(Vector2Int direction) {
            if (!Fits(_position + direction, _cells)) return false;

            _position += direction;
            UpdateVisuals();
            return true;
        }

        public void Lock() {
            for (int i = 0; i < _cells.Length; i++) {
                Vector2Int cell = _position + _cells[i];
                _visuals[i].SetParent(_board.transform, true);
                _board.Fill(cell.x, cell.y, _visuals[i]);
            }

            Destroy(gameObject);
        }

        public static Sprite GetCellSprite() {
            if (_cellSprite == null) {
                var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _cellSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return _cellSprite;
        }

        bool Fits(Vector2Int position, Vector2Int[] cells) {
            for (int i = 0; i < cells.Length; i++) {
                Vector2Int cell = position + cells[i];
                if (!_board.IsFree(cell.x, cell.y)) return false;
            }
            return true;
        }

        void UpdateVisuals() {
            for (int i = 0; i < _cells.Length; i++) {
                Vector2Int cell = _position + _cells[i];
                _visuals[i].position = _board.transform.position + new Vector3(cell.x, cell.y);
            }
        }
    }
}
