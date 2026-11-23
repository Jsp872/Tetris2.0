using System.Collections.Generic;
using Tetris.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Interface en jeu (UI Toolkit, layout dans UI/HUD.uxml, style dans UI/HUD.uss).
    /// Score et lignes via <see cref="Show"/>, prochaines pièces via <see cref="NextBlocsChangedEvent"/>,
    /// écrans de pause et de fin via <see cref="SetPaused"/> et <see cref="ShowGameOver"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HUD : MonoBehaviour {
        const string HiddenClass = "overlay--hidden";
        const string BumpClass = "stat__value--bump";
        const string PreviewCellClass = "preview__cell";

        [Tooltip("Taille d'une case dans la preview de la prochaine pièce, puis des suivantes.")]
        [SerializeField] float _firstPreviewCellSize = 30f;
        [SerializeField] float _otherPreviewCellSize = 22f;

        Label _scoreLabel;
        Label _linesLabel;
        VisualElement _nextList;
        VisualElement _overlay;
        Label _overlayTitle;
        Label _overlaySubtitle;

        void OnEnable() {
            EventBus<NextBlocsChangedEvent>.Subscribe(OnNextBlocsChanged);
        }

        void OnDisable() {
            EventBus<NextBlocsChangedEvent>.Unsubscribe(OnNextBlocsChanged);
        }

        public void Show(int score, int lines) {
            if (!EnsureInitialized()) return;

            SetValue(_scoreLabel, score);
            SetValue(_linesLabel, lines);
        }

        public void SetPaused(bool isPaused) {
            ShowOverlay(isPaused, "PAUSE", "Échap pour reprendre");
        }

        public void ShowGameOver() {
            ShowOverlay(true, "GAME OVER", "Merci d'avoir joué");
        }

        void ShowOverlay(bool isVisible, string title, string subtitle) {
            if (!EnsureInitialized()) return;

            _overlayTitle.text = title;
            _overlaySubtitle.text = subtitle;
            _overlay.EnableInClassList(HiddenClass, !isVisible);
        }

        void OnNextBlocsChanged(NextBlocsChangedEvent e) {
            if (!EnsureInitialized()) return;

            _nextList.Clear();
            for (int i = 0; i < e.NextBlocs.Count; i++) {
                var item = new VisualElement();
                item.AddToClassList("preview");
                item.AddToClassList(i == 0 ? "preview--first" : "preview--other");
                item.Add(CreateShape(e.NextBlocs[i], i == 0 ? _firstPreviewCellSize : _otherPreviewCellSize));
                _nextList.Add(item);
            }
        }

        /// <summary>Dessine la forme d'une pièce avec des petites cases arrondies, centrée dans son conteneur.</summary>
        static VisualElement CreateShape(BlocSO data, float cellSize) {
            Vector2Int min = data.shape[0];
            Vector2Int max = data.shape[0];
            foreach (Vector2Int cell in data.shape) {
                min = Vector2Int.Min(min, cell);
                max = Vector2Int.Max(max, cell);
            }

            var shape = new VisualElement();
            shape.style.width = (max.x - min.x + 1) * cellSize;
            shape.style.height = (max.y - min.y + 1) * cellSize;

            Color top = Color.Lerp(data.color, Color.white, 0.55f);
            foreach (Vector2Int cell in data.shape) {
                var square = new VisualElement();
                square.AddToClassList(PreviewCellClass);
                square.style.left = (cell.x - min.x) * cellSize;
                square.style.top = (max.y - cell.y) * cellSize; // L'UI a l'axe Y vers le bas.
                square.style.width = cellSize - 3f;
                square.style.height = cellSize - 3f;
                square.style.backgroundColor = data.color;
                square.style.borderTopColor = top;
                square.style.borderLeftColor = top;
                shape.Add(square);
            }

            return shape;
        }

        static void SetValue(Label label, int value) {
            string text = value.ToString();
            if (label.text == text) return;

            label.text = text;
            label.AddToClassList(BumpClass);
            label.schedule.Execute(() => label.RemoveFromClassList(BumpClass)).StartingIn(140);
        }

        bool EnsureInitialized() {
            if (_scoreLabel != null) return true;

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return false;

            _scoreLabel = root.Q<Label>("score-value");
            _linesLabel = root.Q<Label>("lines-value");
            _nextList = root.Q("next-list");
            _overlay = root.Q("overlay");
            _overlayTitle = root.Q<Label>("overlay-title");
            _overlaySubtitle = root.Q<Label>("overlay-subtitle");
            return _scoreLabel != null;
        }
    }
}
