using System;
using Tetris.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Interface en jeu (UI Toolkit, layout dans UI/HUD.uxml, style dans UI/HUD.uss).
    /// Score et lignes via <see cref="Show"/>, niveau ou temps via <see cref="ShowThirdStat"/>,
    /// prochaines pièces via <see cref="NextBlocsChangedEvent"/>, compte à rebours, pause et écran de fin.
    /// Les boutons de la pause et de l'écran de fin déclenchent <see cref="PrimaryClicked"/> et <see cref="SecondaryClicked"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HUD : MonoBehaviour {
        const string HiddenClass = "overlay--hidden";
        const string BumpClass = "stat__value--bump";
        const string PreviewCellClass = "preview__cell";
        const string CountdownHiddenClass = "countdown--hidden";
        const string CountdownPopClass = "countdown--pop";
        const string RecordClass = "overlay__record--new";

        [Tooltip("Taille d'une case dans la preview de la prochaine pièce, puis des suivantes.")]
        [SerializeField] float _firstPreviewCellSize = 30f;
        [SerializeField] float _otherPreviewCellSize = 22f;

        /// <summary>Bouton principal : "Reprendre" en pause, "Rejouer" en fin de partie.</summary>
        public event Action PrimaryClicked;
        /// <summary>Bouton secondaire : retour au menu.</summary>
        public event Action SecondaryClicked;

        Label _modeLabel;
        Label _scoreLabel;
        Label _linesLabel;
        Label _thirdCaption;
        Label _thirdLabel;
        VisualElement _nextList;
        VisualElement _overlay;
        Label _overlayTitle;
        Label _overlaySubtitle;
        Label _overlayValue;
        Label _overlayRecord;
        Button _primaryButton;
        Button _secondaryButton;
        Label _countdown;
        IVisualElementScheduledItem _hideCountdown;

        void OnEnable() {
            EventBus<NextBlocsChangedEvent>.Subscribe(OnNextBlocsChanged);
        }

        void OnDisable() {
            EventBus<NextBlocsChangedEvent>.Unsubscribe(OnNextBlocsChanged);
        }

        /// <summary>Nom du mode et légende de la troisième statistique ("NIVEAU" ou "TEMPS").</summary>
        public void SetMode(string modeName, string thirdStatCaption) {
            if (!EnsureInitialized()) return;

            _modeLabel.text = modeName;
            _thirdCaption.text = thirdStatCaption;
        }

        public void Show(int score, int lines) {
            if (!EnsureInitialized()) return;

            SetText(_scoreLabel, ModeRecords.FormatScore(score), true);
            SetText(_linesLabel, lines.ToString(), true);
        }

        /// <summary>Niveau (Marathon) ou temps (Sprint, Ultra).</summary>
        public void ShowThirdStat(string text) {
            if (!EnsureInitialized()) return;

            SetText(_thirdLabel, text, text.Length <= 2);
        }

        /// <summary>Affiche "3", "2", "1", "GO !" en grand au centre ; disparaît seul après un court instant.</summary>
        public void ShowCountdown(string text) {
            if (!EnsureInitialized()) return;

            _countdown.text = text;
            _countdown.RemoveFromClassList(CountdownHiddenClass);
            _countdown.AddToClassList(CountdownPopClass);
            _countdown.schedule.Execute(() => _countdown.RemoveFromClassList(CountdownPopClass)).StartingIn(16);

            _hideCountdown?.Pause();
            _hideCountdown = _countdown.schedule.Execute(() => _countdown.AddToClassList(CountdownHiddenClass)).StartingIn(850);
        }

        public void SetPaused(bool isPaused) {
            if (!EnsureInitialized()) return;

            ShowOverlay(isPaused, "PAUSE", "Échap pour reprendre  ·  N / B pour changer de musique", "", "", false, "REPRENDRE");
        }

        /// <summary>Écran de fin : titre, résultat, ligne de record, boutons Rejouer / Menu.</summary>
        public void ShowResult(string title, string value, string recordLine, bool isNewRecord) {
            if (!EnsureInitialized()) return;

            ShowOverlay(true, title, "", value, isNewRecord ? "NOUVEAU RECORD !" : recordLine, isNewRecord, "REJOUER");
        }

        void ShowOverlay(bool isVisible, string title, string subtitle, string value, string record, bool isNewRecord, string primaryText) {
            _overlayTitle.text = title;
            SetOptionalText(_overlaySubtitle, subtitle);
            SetOptionalText(_overlayValue, value);
            SetOptionalText(_overlayRecord, record);
            _overlayRecord.EnableInClassList(RecordClass, isNewRecord);
            _primaryButton.text = primaryText;

            _overlay.EnableInClassList(HiddenClass, !isVisible);
            _overlay.pickingMode = isVisible ? PickingMode.Position : PickingMode.Ignore;
            _primaryButton.SetEnabled(isVisible);
            _secondaryButton.SetEnabled(isVisible);
            if (isVisible) {
                _primaryButton.schedule.Execute(() => _primaryButton.Focus()).StartingIn(16);
            }
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

            Color color = LockerContent.GetPieceColor(data);
            Color top = Color.Lerp(color, Color.white, 0.55f);
            foreach (Vector2Int cell in data.shape) {
                var square = new VisualElement();
                square.AddToClassList(PreviewCellClass);
                square.style.left = (cell.x - min.x) * cellSize;
                square.style.top = (max.y - cell.y) * cellSize; // L'UI a l'axe Y vers le bas.
                square.style.width = cellSize - 3f;
                square.style.height = cellSize - 3f;
                square.style.backgroundColor = color;
                square.style.borderTopColor = top;
                square.style.borderLeftColor = top;
                shape.Add(square);
            }

            return shape;
        }

        static void SetText(Label label, string text, bool bump) {
            if (label.text == text) return;

            label.text = text;
            if (!bump) return;

            label.AddToClassList(BumpClass);
            label.schedule.Execute(() => label.RemoveFromClassList(BumpClass)).StartingIn(140);
        }

        static void SetOptionalText(Label label, string text) {
            label.text = text;
            label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        bool EnsureInitialized() {
            if (_scoreLabel != null) return true;

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return false;

            _modeLabel = root.Q<Label>("mode-value");
            _scoreLabel = root.Q<Label>("score-value");
            _linesLabel = root.Q<Label>("lines-value");
            _thirdCaption = root.Q<Label>("third-caption");
            _thirdLabel = root.Q<Label>("third-value");
            _nextList = root.Q("next-list");
            _overlay = root.Q("overlay");
            _overlayTitle = root.Q<Label>("overlay-title");
            _overlaySubtitle = root.Q<Label>("overlay-subtitle");
            _overlayValue = root.Q<Label>("overlay-value");
            _overlayRecord = root.Q<Label>("overlay-record");
            _primaryButton = root.Q<Button>("overlay-primary");
            _secondaryButton = root.Q<Button>("overlay-secondary");
            _countdown = root.Q<Label>("countdown");

            _primaryButton.clicked += () => PrimaryClicked?.Invoke();
            _secondaryButton.clicked += () => SecondaryClicked?.Invoke();
            _primaryButton.SetEnabled(false);
            _secondaryButton.SetEnabled(false);
            return _scoreLabel != null;
        }
    }
}
