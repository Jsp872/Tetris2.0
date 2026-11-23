using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Menu principal (UI/MainMenu.uxml) : Jouer ouvre le choix du mode, Options n'est pas encore disponible, Quitter ferme le jeu.
    /// Chaque carte de mode affiche le record et lance la scène de jeu dans ce mode (<see cref="GameSession.Mode"/>).
    /// Un mode sans scène renseignée affiche "bientôt disponible" (la scène doit être dans les Build Profiles).
    /// Échap revient au menu principal.
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
        Button _playButton;
        Button _firstModeButton;
        Label _toast;
        IVisualElementScheduledItem _hideToast;

        void OnEnable() {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            _mainPanel = root.Q("main-panel");
            _modesPanel = root.Q("modes-panel");
            _toast = root.Q<Label>("toast");
            _playButton = root.Q<Button>("play-button");

            _playButton.clicked += () => ShowPanel(_modesPanel, _mainPanel, _firstModeButton);
            root.Q<Button>("options-button").clicked += () => ShowToast("Les options arrivent bientôt");
            root.Q<Button>("quit-button").clicked += Quit;
            root.Q<Button>("back-button").clicked += ShowMainPanel;
            root.RegisterCallback<NavigationCancelEvent>(_ => ShowMainPanel());

            BuildModeCards(root.Q("mode-list"));
            _modesPanel.style.display = DisplayStyle.None;
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

        void ShowMainPanel() {
            if (_mainPanel.style.display == DisplayStyle.None) {
                ShowPanel(_mainPanel, _modesPanel, _playButton);
            }
        }

        static void ShowPanel(VisualElement show, VisualElement hide, Focusable focus) {
            hide.AddToClassList(PanelHiddenClass);
            hide.style.display = DisplayStyle.None;

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
