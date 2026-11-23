using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Décor du menu : des pièces translucides qui tombent lentement en tournant, puis réapparaissent en haut.
    /// Elles prennent le skin de blocs équipé dans le Casier (les nouvelles pièces suivent le changement).
    /// </summary>
    public class MenuFallingBlocks : MonoBehaviour {
        [SerializeField] BlocSO[] _blocs;
        [SerializeField] Sprite _cellSprite;
        [SerializeField, Min(1)] int _count = 10;
        [Tooltip("Zone (en unités) dans laquelle les pièces tombent, centrée sur cet objet.")]
        [SerializeField] Vector2 _area = new Vector2(22f, 26f);
        [SerializeField] Vector2 _fallSpeed = new Vector2(0.6f, 1.6f);
        [SerializeField] Vector2 _spinSpeed = new Vector2(-25f, 25f);
        [SerializeField] Vector2 _scale = new Vector2(0.7f, 1.3f);
        [SerializeField, Range(0f, 1f)] float _opacity = 0.5f;
        [SerializeField] int _sortingOrder = -50;

        Transform[] _pieces;
        float[] _speeds;
        float[] _spins;

        void Start() {
            _pieces = new Transform[_count];
            _speeds = new float[_count];
            _spins = new float[_count];

            for (int i = 0; i < _count; i++) {
                _pieces[i] = new GameObject($"Piece {i}").transform;
                _pieces[i].SetParent(transform, false);
                Respawn(i, Random.Range(-_area.y / 2f, _area.y / 2f));
            }
        }

        void Update() {
            float bottom = -_area.y / 2f - 2f;

            for (int i = 0; i < _pieces.Length; i++) {
                Transform piece = _pieces[i];
                piece.localPosition += Vector3.down * (_speeds[i] * Time.deltaTime);
                piece.Rotate(0f, 0f, _spins[i] * Time.deltaTime);

                if (piece.localPosition.y < bottom) {
                    Respawn(i, _area.y / 2f + 2f);
                }
            }
        }

        void Respawn(int index, float y) {
            Transform piece = _pieces[index];
            for (int c = piece.childCount - 1; c >= 0; c--) {
                Destroy(piece.GetChild(c).gameObject);
            }

            BlocSO data = _blocs[Random.Range(0, _blocs.Length)];
            LockerItem skin = PlayerInventory.GetEquipped(LockerCategory.Blocks);
            Color color = LockerContent.GetPieceColor(data, skin);
            color.a *= _opacity;
            Sprite sprite = LockerContent.GetCellSprite(skin, _cellSprite);

            foreach (Vector2Int cell in data.shape) {
                var go = new GameObject("Cell");
                go.transform.SetParent(piece, false);
                go.transform.localPosition = new Vector3(cell.x - 0.5f, cell.y - 0.5f);

                var spriteRenderer = go.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = color;
                spriteRenderer.sortingOrder = _sortingOrder;
            }

            piece.localPosition = new Vector3(Random.Range(-_area.x / 2f, _area.x / 2f), y);
            piece.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            piece.localScale = Vector3.one * Random.Range(_scale.x, _scale.y);
            _speeds[index] = Random.Range(_fallSpeed.x, _fallSpeed.y);
            _spins[index] = Random.Range(_spinSpeed.x, _spinSpeed.y);
        }
    }
}
