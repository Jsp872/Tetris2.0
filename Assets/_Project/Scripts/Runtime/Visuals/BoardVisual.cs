using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Habillage du Board : un panneau en verre aux coins arrondis et une case fantôme par emplacement de la grille.
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

        void Start() {
            Board board = GetComponent<Board>();
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
