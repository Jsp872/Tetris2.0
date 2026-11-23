using System.Collections.Generic;
using Tetris.Events;
using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Pièce en train de tomber. Sa position et ses cases sont en coordonnées de grille du Board.
    /// Utilisation : Init, puis CanMove / Move et CanRotate / Rotate, puis Lock quand elle ne peut plus descendre.
    /// Une copie transparente (le "fantôme") montre où la pièce va tomber.
    /// Sprite et couleur viennent du skin de blocs équipé dans le Casier.
    /// </summary>
    public class Bloc : MonoBehaviour {
        /// <summary>Opacité des cases du fantôme.</summary>
        public const float GhostAlpha = 0.22f;
        const int GhostSortingOrder = -5;

        [SerializeField] BlocSO _data;
        [SerializeField] Sprite _cellSprite;

        Board _board;
        Vector2Int _position;
        Vector2Int[] _cells;
        Transform[] _visuals;
        Transform[] _ghosts;
        Color _color;
        Sprite _sprite;

        /// <summary>Pièce en train de tomber, ou null.</summary>
        public static Bloc Current { get; private set; }

        public Color Color => _color;

        /// <summary>Visuels des cases de la pièce.</summary>
        public IReadOnlyList<Transform> Cells => _visuals;

        /// <summary>Visuels des cases du fantôme.</summary>
        public IReadOnlyList<Transform> GhostCells => _ghosts;

        /// <summary>Ligne de la case la plus basse de la pièce.</summary>
        public int MinY {
            get {
                int min = int.MaxValue;
                foreach (Vector2Int cell in _cells) min = Mathf.Min(min, _position.y + cell.y);
                return min;
            }
        }

        /// <summary>Prépare la pièce en haut au centre du Board.</summary>
        public void Init(BlocSO data, Board board) {
            _data = data;
            _board = board;
            _cells = data.shape;
            _position = new Vector2Int(board.Width / 2 - 1, board.SpawnY);
            Current = this;

            CreateVisuals();
            UpdateVisuals();
        }

        void OnDestroy() {
            if (Current == this) Current = null;
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

            EventBus<BlocLockedEvent>.Publish(new BlocLockedEvent());
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
            _ghosts = new Transform[_cells.Length];
            LockerItem skin = PlayerInventory.GetEquipped(LockerCategory.Blocks);
            _color = LockerContent.GetPieceColor(_data, skin);
            _sprite = LockerContent.GetCellSprite(skin, _cellSprite);
            Color ghostColor = _color;
            ghostColor.a = GhostAlpha * _color.a;

            for (int i = 0; i < _cells.Length; i++) {
                _visuals[i] = CreateCell("Cell", _color, 0);
                _ghosts[i] = CreateCell("Ghost", ghostColor, GhostSortingOrder);
            }
        }

        Transform CreateCell(string cellName, Color color, int sortingOrder) {
            var cell = new GameObject(cellName);
            cell.transform.SetParent(transform, false);

            var spriteRenderer = cell.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = _sprite;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
            return cell.transform;
        }

        void UpdateVisuals() {
            int drop = 0;
            while (Fits(_position + Vector2Int.down * (drop + 1), _cells)) drop++;

            for (int i = 0; i < _cells.Length; i++) {
                Vector2Int cell = _position + _cells[i];
                _visuals[i].position = _board.transform.position + new Vector3(cell.x, cell.y);
                _ghosts[i].position = _board.transform.position + new Vector3(cell.x, cell.y - drop);
            }
        }
    }
}
