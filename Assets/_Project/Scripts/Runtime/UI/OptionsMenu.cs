using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Lignes du panneau Options (UI/MainMenu.uxml, conteneur "options-list"), générées en code :
    /// <list type="bullet">
    /// <item>Son : volumes général, musique, effets et mèmes (barre cliquable et glissable).</item>
    /// <item>Affichage : plein écran ou fenêtré, résolution, synchronisation verticale.</item>
    /// <item>Données : effacer les records (deuxième clic pour confirmer).</item>
    /// <item>Spoof Goof : options absurdes (<see cref="SpoofGoof"/>), surlignées quand elles sont actives.</item>
    /// </list>
    /// Chaque ligne se sélectionne au clavier ou à la manette (haut / bas) et se règle avec gauche / droite.
    /// La liste défile (ScrollView) et suit la ligne sélectionnée.
    /// Le Spoof Goof est caché : un espace vide le pousse juste sous le bas de la carte, et ses lignes
    /// ne sont pas sélectionnables au clavier tant que le joueur n'a pas fait défiler la liste jusqu'à lui.
    /// </summary>
    public class OptionsMenu {
        const float VolumeStep = 0.1f;
        const long ResetConfirmDelay = 3000;
        const string ConfirmClass = "options-pill--confirm";
        const string DoneClass = "options-pill--done";
        const string GoofActiveClass = "options-choice__value--goof";
        const string GoofTitle = "SPOOF GOOF";
        const float GoofHiddenMargin = 60f;

        readonly VisualElement _list;
        readonly ScrollView _scroll;
        readonly StringBuilder _titleBuilder = new();
        readonly List<VisualElement> _goofRows = new();
        VisualElement _goofSpacer;
        VisualElement _goofSection;
        bool _isGoofRevealed;
        IVisualElementScheduledItem _cancelReset;

        /// <summary>Déclenché après l'effacement des records.</summary>
        public event Action RecordsReset;

        /// <summary>Première ligne, à sélectionner à l'ouverture du panneau.</summary>
        public Focusable FirstRow { get; private set; }

        public OptionsMenu(VisualElement list) {
            _list = list;
            _scroll = list as ScrollView;
            if (_scroll != null) _scroll.verticalScroller.valueChanged += _ => RevealGoofIfVisible();
        }

        /// <summary>
        /// Reconstruit les lignes avec les valeurs actuelles (l'écran a pu changer, par exemple avec Alt+Entrée).
        /// <paramref name="revealGoof"/> : le Spoof Goof reste sélectionnable (sinon il est caché jusqu'au prochain défilement).
        /// </summary>
        public void Refresh(bool revealGoof = false) {
            _list.Clear();
            FirstRow = null;
            _goofRows.Clear();
            _isGoofRevealed = revealGoof;
            _cancelReset?.Pause();

            AddSection("SON");
            AddVolume("GÉNÉRAL", GameSettings.MasterVolume, v => GameSettings.MasterVolume = v);
            AddVolume("MUSIQUE", GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            AddVolume("EFFETS", GameSettings.SfxVolume, v => GameSettings.SfxVolume = v);
            AddVolume("MÈMES", GameSettings.MemeVolume, v => GameSettings.MemeVolume = v);

            AddSection("AFFICHAGE");
            AddDisplayRows();
            AddChoice("VSYNC", new[] { "ACTIVÉE", "DÉSACTIVÉE" }, GameSettings.VSync ? 0 : 1, i => GameSettings.VSync = i == 0);

            AddSection("DONNÉES");
            AddResetRecords();

            AddSpoofGoof();
            if (_scroll != null) _scroll.scrollOffset = Vector2.zero;
        }

        /// <summary>À appeler en quittant le panneau : écrit les options sur le disque.</summary>
        public void Save() {
            GameSettings.Save();
        }

        // ---------- Affichage ----------

        void AddDisplayRows() {
            List<Vector2Int> resolutions = GetResolutions(out int resolutionIndex);
            bool isFullScreen = Screen.fullScreenMode != FullScreenMode.Windowed;

            void Apply() {
                Vector2Int size = resolutions[resolutionIndex];
                Screen.SetResolution(size.x, size.y, isFullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            }

            AddChoice("ÉCRAN", new[] { "PLEIN ÉCRAN", "FENÊTRÉ" }, isFullScreen ? 0 : 1, i => {
                isFullScreen = i == 0;
                Apply();
            });

            string[] names = resolutions.ConvertAll(r => $"{r.x} × {r.y}").ToArray();
            AddChoice("RÉSOLUTION", names, resolutionIndex, i => {
                resolutionIndex = i;
                Apply();
            });
        }

        /// <summary>Résolutions de l'écran sans doublons (les fréquences sont ignorées), plus la taille actuelle si elle n'y est pas.</summary>
        static List<Vector2Int> GetResolutions(out int currentIndex) {
            var sizes = new List<Vector2Int>();
            foreach (Resolution resolution in Screen.resolutions) {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (!sizes.Contains(size)) sizes.Add(size);
            }

            var current = new Vector2Int(Screen.width, Screen.height);
            if (!sizes.Contains(current)) {
                sizes.Add(current);
                sizes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            }

            currentIndex = sizes.IndexOf(current);
            return sizes;
        }

        // ---------- Données ----------

        void AddResetRecords() {
            bool isConfirming = false;
            Label pill = CreateLabel("EFFACER", "options-pill");

            void Reset() {
                isConfirming = false;
                pill.text = "EFFACER";
                pill.RemoveFromClassList(ConfirmClass);
                pill.RemoveFromClassList(DoneClass);
            }

            void Submit() {
                _cancelReset?.Pause();

                if (!isConfirming) {
                    isConfirming = true;
                    pill.text = "T'ES SÛR ?";
                    pill.RemoveFromClassList(DoneClass);
                    pill.AddToClassList(ConfirmClass);
                    _cancelReset = pill.schedule.Execute(Reset).StartingIn(ResetConfirmDelay);
                    return;
                }

                ModeRecords.ClearAll();
                isConfirming = false;
                pill.text = "C'EST FAIT";
                pill.RemoveFromClassList(ConfirmClass);
                pill.AddToClassList(DoneClass);
                _cancelReset = pill.schedule.Execute(Reset).StartingIn(ResetConfirmDelay);
                RecordsReset?.Invoke();
            }

            VisualElement row = CreateRow("RECORDS", null);
            row.Q(className: "options-row__control").Add(pill);
            row.RegisterCallback<ClickEvent>(_ => Submit());
            row.RegisterCallback<NavigationSubmitEvent>(e => {
                Submit();
                e.StopPropagation();
            });
        }

        // ---------- Spoof Goof ----------

        void AddSpoofGoof() {
            _goofSpacer = new VisualElement { pickingMode = PickingMode.Ignore };
            _goofSpacer.RegisterCallback<GeometryChangedEvent>(_ => FitGoofSpacer());
            _list.Add(_goofSpacer);

            int firstGoofRow = _list.Query(className: "options-row").ToList().Count;
            AddGoofTitle();
            _list.Add(CreateLabel("L'inversion et la gravité désactivent les records.", "options-note"));

            AddGoof("INVERSION", SpoofGoof.Inversion, "AUCUNE", "VERTICALE");
            AddGoof("BOUSSOLE", SpoofGoof.Compass, "NORMALE", "CASSÉE");
            AddGoof("GRAVITÉ", SpoofGoof.Gravity, "LUNE", "TERRE", "JUPITER");
            AddGoof("VOIX", SpoofGoof.Voice, "CHIPMUNK", "NORMALE", "DÉMON");
            AddGoof("MAL DE MER", SpoofGoof.Seasickness, "AUCUN", "BATEAU", "TEMPÊTE");
            AddGoof("DISCO", SpoofGoof.Disco, "NON", "OUI");
            AddGoof("PIÈCES", SpoofGoof.Drunk, "SOBRES", "POMPETTES", "BOURRÉES");
            AddGoof("MÉMOIRE", SpoofGoof.Goldfish, "ÉLÉPHANT", "POISSON ROUGE");
            AddGoof("INTERFACE", SpoofGoof.Interface, "CORRECTE", "DÉPLORABLE");

            VisualElement row = CreateRow("TOUT ÇA", null);
            row.Q(className: "options-row__control").Add(CreateLabel("REDEVENIR SÉRIEUX", "options-pill"));
            void Submit() {
                SpoofGoofSettings.ResetAll();
                // Reconstruit les lignes après l'événement en cours, puis revient sur ce bouton.
                _list.schedule.Execute(() => {
                    Refresh(revealGoof: true);
                    VisualElement last = _list.Query(className: "options-row").Last();
                    last.Focus();
                    // Attend la mise en page des nouvelles lignes avant de défiler.
                    last.schedule.Execute(() => _scroll?.ScrollTo(last)).StartingIn(16);
                });
            }
            row.RegisterCallback<ClickEvent>(_ => Submit());
            row.RegisterCallback<NavigationSubmitEvent>(e => {
                Submit();
                e.StopPropagation();
            });

            List<VisualElement> rows = _list.Query(className: "options-row").ToList();
            _goofRows.AddRange(rows.GetRange(firstGoofRow, rows.Count - firstGoofRow));
            foreach (VisualElement goofRow in _goofRows) {
                goofRow.focusable = _isGoofRevealed;
            }
        }

        /// <summary>
        /// Hauteur de l'espace vide : assez pour que le titre du Spoof Goof commence sous le bas de la carte
        /// quand la liste n'a pas défilé, quelle que soit la taille des réglages au-dessus.
        /// </summary>
        void FitGoofSpacer() {
            if (_scroll == null) return;

            float viewportHeight = _scroll.contentViewport.layout.height;
            float top = _goofSpacer.layout.y;
            if (float.IsNaN(viewportHeight) || float.IsNaN(top)) return;

            float height = Mathf.Max(0f, viewportHeight - top) + GoofHiddenMargin;
            if (!Mathf.Approximately(_goofSpacer.resolvedStyle.height, height)) {
                _goofSpacer.style.height = height;
            }
        }

        /// <summary>Rend le Spoof Goof sélectionnable dès que son titre apparaît dans la carte.</summary>
        void RevealGoofIfVisible() {
            if (_isGoofRevealed || _goofSection == null) return;

            float visibleBottom = _scroll.scrollOffset.y + _scroll.contentViewport.layout.height;
            if (_goofSection.layout.y >= visibleBottom) return;

            _isGoofRevealed = true;
            foreach (VisualElement row in _goofRows) {
                row.focusable = true;
            }
        }

        void AddGoof(string title, SpoofGoof option, params string[] values) {
            AddChoice(title, values, SpoofGoofSettings.Get(option), i => SpoofGoofSettings.Set(option, i),
                SpoofGoofSettings.Default(option));
        }

        /// <summary>Titre arc-en-ciel lettre par lettre, qui ondule.</summary>
        void AddGoofTitle() {
            Label title = CreateLabel(GoofTitle, "options-section");
            title.AddToClassList("options-section--goof");
            title.enableRichText = true;
            _list.Add(title);
            _goofSection = title;

            title.schedule.Execute(() => {
                float time = Time.unscaledTime;
                _titleBuilder.Clear();
                for (int i = 0; i < GoofTitle.Length; i++) {
                    Color color = Color.HSVToRGB(Mathf.Repeat(time * 0.5f - i * 0.08f, 1f), 0.6f, 1f);
                    _titleBuilder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>')
                        .Append(GoofTitle[i]).Append("</color>");
                }
                title.text = _titleBuilder.ToString();
                title.style.rotate = new Rotate(Mathf.Sin(time * 2.6f) * 2.5f);
            }).Every(40);
        }

        // ---------- Types de lignes ----------

        void AddSection(string title) {
            _list.Add(CreateLabel(title, "options-section"));
        }

        /// <summary>Volume : barre cliquable et glissable, gauche / droite par pas de 10 %.</summary>
        void AddVolume(string title, float value, Action<float> onChanged) {
            var bar = new VisualElement();
            bar.AddToClassList("options-volume__bar");
            var track = new VisualElement();
            track.AddToClassList("options-volume__track");
            var fill = new VisualElement();
            fill.AddToClassList("options-volume__fill");
            var knob = new VisualElement();
            knob.AddToClassList("options-volume__knob");
            track.Add(fill);
            track.Add(knob);
            bar.Add(track);
            Label valueLabel = CreateLabel("", "options-volume__value");

            void Set(float newValue, bool notify) {
                value = Mathf.Clamp01(newValue);
                fill.style.width = Length.Percent(value * 100f);
                knob.style.left = Length.Percent(value * 100f);
                int percent = Mathf.RoundToInt(value * 100f);
                valueLabel.text = percent == 0 ? "MUET" : $"{percent} %";
                if (notify) onChanged(value);
            }

            VisualElement row = CreateRow(title, step => Set(Mathf.Round((value + step * VolumeStep) * 10f) / 10f, true));
            VisualElement control = row.Q(className: "options-row__control");
            control.Add(bar);
            control.Add(valueLabel);
            Set(value, false);

            void SetFromPointer(Vector2 localPosition) {
                float width = track.layout.width;
                if (width > 0f) Set(localPosition.x / width, true);
            }

            track.pickingMode = PickingMode.Ignore;
            bar.RegisterCallback<PointerDownEvent>(e => {
                row.Focus();
                bar.CapturePointer(e.pointerId);
                SetFromPointer(bar.ChangeCoordinatesTo(track, e.localPosition));
            });
            bar.RegisterCallback<PointerMoveEvent>(e => {
                if (bar.HasPointerCapture(e.pointerId)) SetFromPointer(bar.ChangeCoordinatesTo(track, e.localPosition));
            });
            bar.RegisterCallback<PointerUpEvent>(e => bar.ReleasePointer(e.pointerId));
        }

        /// <summary>
        /// Choix dans une liste : clic ou Entrée pour la valeur suivante, flèches ou gauche / droite pour défiler.
        /// <paramref name="normalIndex"/> (optionnel) : la valeur est surlignée quand elle s'en écarte.
        /// </summary>
        void AddChoice(string title, string[] values, int index, Action<int> onChanged, int normalIndex = -1) {
            int count = values.Length;
            VisualElement previous = CreateArrow("options-choice__chevron--left");
            VisualElement next = CreateArrow("options-choice__chevron--right");
            Label valueLabel = CreateLabel(values[index], "options-choice__value");

            void Highlight() {
                valueLabel.EnableInClassList(GoofActiveClass, normalIndex >= 0 && index != normalIndex);
            }

            void Select(int newIndex) {
                index = (newIndex % count + count) % count;
                valueLabel.text = values[index];
                Highlight();
                onChanged(index);
            }

            Highlight();

            VisualElement row = CreateRow(title, step => Select(index + step));
            VisualElement control = row.Q(className: "options-row__control");
            control.Add(previous);
            control.Add(valueLabel);
            control.Add(next);

            row.RegisterCallback<ClickEvent>(e => Select(index + (e.target == previous ? -1 : 1)));
            row.RegisterCallback<NavigationSubmitEvent>(e => {
                Select(index + 1);
                e.StopPropagation();
            });
        }

        /// <summary>
        /// Ligne sélectionnable : titre à gauche, zone de réglage ("options-row__control") à droite.
        /// <paramref name="onStep"/> reçoit -1 ou 1 pour gauche / droite ; null : gauche / droite gardent la navigation normale.
        /// </summary>
        VisualElement CreateRow(string title, Action<int> onStep) {
            var row = new VisualElement { focusable = true };
            row.AddToClassList("options-row");
            row.Add(CreateLabel(title, "options-row__label"));

            var control = new VisualElement();
            control.AddToClassList("options-row__control");
            row.Add(control);

            if (onStep != null) {
                row.RegisterCallback<NavigationMoveEvent>(e => {
                    int step = e.direction switch {
                        NavigationMoveEvent.Direction.Left => -1,
                        NavigationMoveEvent.Direction.Right => 1,
                        _ => 0,
                    };
                    if (step == 0) return;

                    onStep(step);
                    e.StopPropagation();
                    row.focusController?.IgnoreEvent(e);
                });
            }

            row.RegisterCallback<FocusInEvent>(_ => _scroll?.ScrollTo(row));

            _list.Add(row);
            FirstRow ??= row;
            return row;
        }

        static VisualElement CreateArrow(string chevronClass) {
            var arrow = new VisualElement();
            arrow.AddToClassList("options-choice__arrow");
            var chevron = new VisualElement { pickingMode = PickingMode.Ignore };
            chevron.AddToClassList("options-choice__chevron");
            chevron.AddToClassList(chevronClass);
            arrow.Add(chevron);
            return arrow;
        }

        static Label CreateLabel(string text, string className) {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }
    }
}
