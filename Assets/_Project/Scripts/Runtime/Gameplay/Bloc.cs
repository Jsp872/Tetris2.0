using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Pièce en train de tomber. Sa position et ses cases sont en coordonnées de grille du Board.
    /// Utilisation : Init, puis CanMove / Move et CanRotate / Rotate, puis Lock quand elle ne peut plus descendre.
    /// </summary>
    public class Bloc : MonoBehaviour {
        [SerializeField] BlocSO _data;
        [SerializeField] Sprite _cellSprite;

        Board _board;
        Vector2Int _position;
        Vector2Int[] _cells;
        Transform[] _visuals;

        /// <summary>Prépare la pièce en haut au centre du Board.</summary>
        public void Init(BlocSO data, Board board) {
            _data = data;
            _board = board;
            _cells = data.shape;
            _position = new Vector2Int(board.Width / 2 - 1, board.Height - 2);

            CreateVisuals();
            UpdateVisuals();
        }

        /// <summary>Vrai si la pièce peut se décaler de <paramref name="direction"/>. Avec Vector2Int.zero : vrai si sa place actuelle est libre.</summary>
        public bool CanMove(Vector2Int direction) {
            return Fits(_position + direction, _cells);
        }

        /// <summary>Vrai si la pièce peut tourner d'un quart de tour dans le sens horaire.</summary>
        public bool CanRotate() {
            return _data.canRotate && Fits(_position, GetRotatedCells());
        }

        public void Move(Vector2Int direction) {
            _position += direction;
            UpdateVisuals();
        }

        public void Rotate() {
            _cells = GetRotatedCells();
            UpdateVisuals();
        }

        /// <summary>Pose la pièce dans le Board, puis détruit l'objet Bloc. Les cases restent dans le Board.</summary>
        public void Lock() {
            for (int i = 0; i < _cells.Length; i++) {
                Vector2Int cell = _position + _cells[i];
                _visuals[i].SetParent(_board.transform, true);
                _board.Fill(cell.x, cell.y, _visuals[i]);
            }

            Destroy(gameObject);
        }

        bool Fits(Vector2Int position, Vector2Int[] cells) {
            for (int i = 0; i < cells.Length; i++) {
                Vector2Int cell = position + cells[i];
                if (!_board.IsFree(cell.x, cell.y)) return false;
            }
            return true;
        }

        Vector2Int[] GetRotatedCells() {
            var rotated = new Vector2Int[_cells.Length];
            for (int i = 0; i < _cells.Length; i++) {
                rotated[i] = new Vector2Int(_cells[i].y, -_cells[i].x);
            }
            return rotated;
        }

        void CreateVisuals() {
            _visuals = new Transform[_cells.Length];

            for (int i = 0; i < _cells.Length; i++) {
                var cell = new GameObject("Cell");
                cell.transform.SetParent(transform, false);

                var spriteRenderer = cell.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = _cellSprite;
                spriteRenderer.color = _data.color;

                _visuals[i] = cell.transform;
            }
        }

        void UpdateVisuals() {
            for (int i = 0; i < _cells.Length; i++) {
                Vector2Int cell = _position + _cells[i];
                _visuals[i].position = _board.transform.position + new Vector3(cell.x, cell.y);
            }
        }
    }
}
