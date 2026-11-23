using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Menu principal (UI/MainMenu.uxml) : Jouer ouvre le choix du mode, Options ouvre les réglages (<see cref="OptionsMenu"/>),
    /// Quitter ferme le jeu.
    /// Chaque carte de mode affiche le record et lance la scène de jeu dans ce mode (<see cref="GameSession.Mode"/>).
    /// Un mode sans scène renseignée affiche "bientôt disponible" (la scène doit être dans les Build Profiles).
    /// Échap ou Retour revient au menu principal (et enregistre les options si on en sort).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenu : MonoBehaviour {
        const string PanelHiddenClass = "menu-panel--hidden";
        const string ToastHiddenClass = "menu-toast--hidden";

        [Serializable]
        public class GameModeEntry {
            public GameMode mode;
            public string title;
            [TextArea] public string description;
            [Tooltip("Scène à charger. Vide : le mode n'existe pas encore.")]
            public string sceneName;
        }

        [SerializeField] GameModeEntry[] _modes = {
            new GameModeEntry { mode = GameMode.Marathon, title = "MARATHON", sceneName = "Gameplay",
                description = "Survis le plus longtemps possible. Fin au niveau 15 (150 lignes)." },
            new GameModeEntry { mode = GameMode.Sprint, title = "SPRINT", sceneName = "Gameplay",
                description = "Efface 40 lignes le plus vite possible." },
            new GameModeEntry { mode = GameMode.Ultra, title = "ULTRA", sceneName = "Gameplay",
                description = "Marque un maximum de points en 2 minutes." },
        };
        [SerializeField] float _toastDuration = 1.6f;

        VisualElement _mainPanel;
        VisualElement _modesPanel;
        VisualElement _optionsPanel;
        VisualElement _currentPanel;
        VisualElement _modeList;
        Button _playButton;
        Button _optionsButton;
        Button _firstModeButton;
        OptionsMenu _options;
        Label _toast;
        IVisualElementScheduledItem _hideToast;

        void OnEnable() {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            _mainPanel = root.Q("main-panel");
            _modesPanel = root.Q("modes-panel");
            _optionsPanel = root.Q("options-panel");
            _modeList = root.Q("mode-list");
            _toast = root.Q<Label>("toast");
            _playButton = root.Q<Button>("play-button");
            _optionsButton = root.Q<Button>("options-button");
            _options = new OptionsMenu(root.Q("options-list"));
            _options.RecordsReset += () => BuildModeCards(_modeList);

            _playButton.clicked += () => ShowPanel(_modesPanel, _firstModeButton);
            _optionsButton.clicked += ShowOptionsPanel;
            root.Q<Button>("quit-button").clicked += Quit;
            root.Q<Button>("back-button").clicked += ShowMainPanel;
            root.Q<Button>("options-back-button").clicked += ShowMainPanel;
            root.RegisterCallback<NavigationCancelEvent>(_ => ShowMainPanel());

            BuildModeCards(_modeList);
            _modesPanel.style.display = DisplayStyle.None;
            _optionsPanel.style.display = DisplayStyle.None;
            _currentPanel = _mainPanel;
            _playButton.schedule.Execute(() => _playButton.Focus());
        }

        void BuildModeCards(VisualElement list) {
            list.Clear();
            _firstModeButton = null;

            foreach (GameModeEntry mode in _modes) {
                var card = new Button();
                card.AddToClassList("glass");
                card.AddToClassList("mode-card");
                card.clicked += () => OnModeClicked(mode, card);

                var header = new VisualElement();
                header.AddToClassList("mode-card__header");
                header.Add(CreateLabel(mode.title, "mode-card__title"));
                if (string.IsNullOrEmpty(mode.sceneName)) {
                    header.Add(CreateLabel("BIENTÔT", "mode-card__badge"));
                }
                else {
                    string record = ModeRecords.TryGet(mode.mode, out float best)
                        ? $"RECORD  {ModeRecords.Format(mode.mode, best)}"
                        : "AUCUN RECORD";
                    header.Add(CreateLabel(record, "mode-card__record"));
                }

                card.Add(header);
                card.Add(CreateLabel(mode.description, "mode-card__description"));
                list.Add(card);

                _firstModeButton ??= card;
            }
        }

        void OnModeClicked(GameModeEntry mode, VisualElement card) {
            if (!string.IsNullOrEmpty(mode.sceneName)) {
                GameSession.Mode = mode.mode;
                SceneManager.LoadScene(mode.sceneName);
                return;
            }

            Shake(card);
            ShowToast($"Le mode {mode.title} arrive bientôt");
        }

        void ShowOptionsPanel() {
            _options.Refresh();
            ShowPanel(_optionsPanel, _options.FirstRow);
        }

        /// <summary>Retour au menu principal, sur le bouton qui avait ouvert le panneau.</summary>
        void ShowMainPanel() {
            if (_currentPanel == _mainPanel) return;

            Focusable focus = _playButton;
            if (_currentPanel == _optionsPanel) {
                _options.Save();
                focus = _optionsButton;
            }
            ShowPanel(_mainPanel, focus);
        }

        void ShowPanel(VisualElement show, Focusable focus) {
            _currentPanel.AddToClassList(PanelHiddenClass);
            _currentPanel.style.display = DisplayStyle.None;
            _currentPanel = show;

            show.style.display = DisplayStyle.Flex;
            show.schedule.Execute(() => {
                show.RemoveFromClassList(PanelHiddenClass);
                focus?.Focus();
            }).StartingIn(16);
        }

        void ShowToast(string message) {
            _toast.text = message;
            _toast.RemoveFromClassList(ToastHiddenClass);

            _hideToast?.Pause();
            _hideToast = _toast.schedule.Execute(() => _toast.AddToClassList(ToastHiddenClass))
                .StartingIn((long)(_toastDuration * 1000f));
        }

        static void Shake(VisualElement element) {
            string[] steps = { "mode-card--shake-left", "mode-card--shake-right", "mode-card--shake-left", null };
            for (int i = 0; i < steps.Length; i++) {
                string step = steps[i];
                element.schedule.Execute(() => {
                    element.RemoveFromClassList("mode-card--shake-left");
                    element.RemoveFromClassList("mode-card--shake-right");
                    if (step != null) element.AddToClassList(step);
                }).StartingIn(i * 60);
            }
        }

        static Label CreateLabel(string text, string className) {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        static void Quit() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
