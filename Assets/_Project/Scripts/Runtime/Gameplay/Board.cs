using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Grille du jeu. La case (0, 0) est en bas à gauche, centrée sur la position du Board.
    /// Une case fait 1 unité : la case (x, y) est à la position du Board + (x, y).
    /// </summary>
    public class Board : MonoBehaviour {
        [SerializeField] int _width = 10;
        [SerializeField] int _height = 20;

        Transform[,] _grid;

        public int Width => _width;
        public int Height => _height;

        void Awake() {
            _grid = new Transform[_width, _height];
        }

        /// <summary>Vrai si la case est dans la grille et vide.</summary>
        public bool IsFree(int x, int y) {
            return x >= 0 && x < _width && y >= 0 && y < _height && _grid[x, y] == null;
        }

        /// <summary>Occupe la case avec le visuel d'un bloc posé.</summary>
        public void Fill(int x, int y, Transform cell) {
            _grid[x, y] = cell;
        }

        /// <summary>Supprime les lignes pleines, fait descendre celles du dessus et renvoie le nombre de lignes supprimées.</summary>
        public int ClearFullRows() {
            int cleared = 0;

            for (int y = 0; y < _height; y++) {
                if (!IsRowFull(y)) continue;

                ClearRow(y);
                MoveRowsDown(y + 1);
                cleared++;
                y--; // La ligne du dessus vient de descendre ici : on la revérifie.
            }

            return cleared;
        }

        bool IsRowFull(int y) {
            for (int x = 0; x < _width; x++) {
                if (_grid[x, y] == null) return false;
            }
            return true;
        }

        void ClearRow(int y) {
            for (int x = 0; x < _width; x++) {
                Destroy(_grid[x, y].gameObject);
                _grid[x, y] = null;
            }
        }

        void MoveRowsDown(int fromY) {
            for (int y = fromY; y < _height; y++) {
                for (int x = 0; x < _width; x++) {
                    Transform cell = _grid[x, y];
                    if (cell == null) continue;

                    _grid[x, y - 1] = cell;
                    _grid[x, y] = null;
                    cell.position += Vector3.down;
                }
            }
        }

        void OnDrawGizmos() {
            Vector3 size = new Vector3(_width, _height);
            Vector3 center = transform.position + (size - Vector3.one) / 2f;
            center.z = transform.position.z;

            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
