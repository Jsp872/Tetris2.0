using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Habillage du Board : un panneau en verre aux coins arrondis, une case fantôme par emplacement de la grille
    /// et la ligne de limite en pointillés sous la zone d'apparition (<see cref="Board.LimitY"/>).
    /// La ligne pulse, et rougit quand la pile s'en approche. Avec l'inversion Spoof Goof, elle est dessinée en bas.
    /// Purement visuel, ne touche pas à la logique du Board.
    /// </summary>
    [RequireComponent(typeof(Board))]
    public class BoardVisual : MonoBehaviour {
        [SerializeField] Sprite _panelFill;
        [SerializeField] Sprite _panelOutline;
        [SerializeField] Sprite _slotSprite;
        [SerializeField] Color _fillColor = new Color(0.04f, 0.05f, 0.12f, 0.55f);
        [SerializeField] Color _outlineColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] Color _slotColor = new Color(1f, 1f, 1f, 0.04f);
        [Tooltip("Marge entre la grille et le bord du panneau, en unités.")]
        [SerializeField] float _padding = 0.4f;

        [Header("Ligne de limite")]
        [SerializeField] Color _limitColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] Color _limitDangerColor = new Color(1f, 0.3f, 0.4f, 0.95f);
        [Tooltip("La ligne passe en danger quand la pile arrive à ce nombre de lignes d'elle.")]
        [SerializeField, Min(1)] int _dangerDistance = 4;
        const float DashLength = 0.32f;
        const float DashThickness = 0.07f;

        Board _board;
        SpriteRenderer[] _limitDashes;
        float _danger;

        void Start() {
            Board board = GetComponent<Board>();
            _board = board;
            var center = new Vector3((board.Width - 1) / 2f, (board.Height - 1) / 2f);
            var size = new Vector2(board.Width + _padding * 2f, board.Height + _padding * 2f);

            CreatePanel("Panel Fill", _panelFill, _fillColor, center, size, -20);
            CreatePanel("Panel Outline", _panelOutline, _outlineColor, center, size, -19);

            var slots = new GameObject("Slots").transform;
            slots.SetParent(transform, false);
            for (int x = 0; x < board.Width; x++) {
                for (int y = 0; y < board.Height; y++) {
                    var slot = new GameObject($"Slot {x},{y}");
                    slot.transform.SetParent(slots, false);
                    slot.transform.localPosition = new Vector3(x, y);

                    var spriteRenderer = slot.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = _slotSprite;
                    spriteRenderer.color = _slotColor;
                    spriteRenderer.sortingOrder = -18;
                }
            }

            CreateLimitLine(board);
        }

        void CreateLimitLine(Board board) {
            var line = new GameObject("Limit Line").transform;
            line.SetParent(transform, false);
            float y = board.LimitY - 0.5f;
            line.localPosition = new Vector3(0f, SpoofGoofSettings.IsInverted ? board.Height - 1 - y : y);

            // Sprite blanc de 1 unité, étiré pour chaque tiret.
            Sprite white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            int count = Mathf.RoundToInt(board.Width * 2f);
            _limitDashes = new SpriteRenderer[count];

            for (int i = 0; i < count; i++) {
                var dash = new GameObject("Dash");
                dash.transform.SetParent(line, false);
                dash.transform.localPosition = new Vector3(-0.25f + i * 0.5f, 0f);
                dash.transform.localScale = new Vector3(DashLength, DashThickness, 1f);

                var spriteRenderer = dash.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = white;
                spriteRenderer.color = _limitColor;
                spriteRenderer.sortingOrder = -10;
                _limitDashes[i] = spriteRenderer;
            }
        }

        void Update() {
            if (_limitDashes == null) return;

            bool isNear = _board.HasBlocksFrom(_board.LimitY - _dangerDistance);
            _danger = Mathf.MoveTowards(_danger, isNear ? 1f : 0f, Time.unscaledDeltaTime * 3f);

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Lerp(2f, 9f, _danger));
            Color color = Color.Lerp(_limitColor, _limitDangerColor, _danger);
            color.a *= Mathf.Lerp(0.55f, 1f, pulse);

            foreach (SpriteRenderer dash in _limitDashes) {
                dash.color = color;
            }
        }

        void CreatePanel(string name, Sprite sprite, Color color, Vector3 center, Vector2 size, int sortingOrder) {
            var panel = new GameObject(name);
            panel.transform.SetParent(transform, false);
            panel.transform.localPosition = center;

            var spriteRenderer = panel.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = size;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }
}
