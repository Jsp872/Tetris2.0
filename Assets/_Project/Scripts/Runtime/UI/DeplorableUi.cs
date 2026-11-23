using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace Tetris {
    /// <summary>
    /// Option Spoof Goof "Interface déplorable" : la même direction artistique, mais en pire.
    /// Sur toutes les UIDocument, tant que l'option est active (et retiré proprement quand elle ne l'est plus) :
    /// <list type="bullet">
    /// <item>Panneaux et boutons de travers, textes mal alignés, peu lisibles, polices dépareillées (Comic Sans si dispo).</item>
    /// <item>Bandeau d'avertissement qui défile, toast "Enregistrement..." qui ne finit jamais.</item>
    /// <item>Bannière de cookies qui revient toujours, bulle de chat inutile qui accumule les notifications.</item>
    /// <item>Faux curseurs d'autres joueurs, fausse mise à jour importante de temps en temps.</item>
    /// <item>Les boutons fuient la souris pendant un moment, puis se laissent attraper.</item>
    /// <item>Chaque clic sur un bouton demande confirmation : des dizaines de réponses hésitantes,
    /// une seule qui accepte, jamais la même.</item>
    /// <item>Les volumes changent tout seuls (remis comme avant quand l'option est désactivée).</item>
    /// <item>Un clic sur trois environ change la musique au hasard.</item>
    /// </list>
    /// Tout reste utilisable, juste... pénible. Les ajouts ne prennent pas le focus.
    /// Styles "deplorable-*" dans Common.uss.
    /// </summary>
    public class DeplorableUi : MonoBehaviour {
        const float ScanInterval = 0.5f;
        const float CookieReturnDelay = 40f;
        const float UpdateDuration = 7f;
        const string LayerName = "deplorable-layer";

        static readonly string[] PreferredFonts = { "Comic Sans MS", "Comic Neue", "Comic Relief", "Papyrus", "Impact", "Chalkboard" };
        static readonly string[] CursorNames = { "Sarah", "Invité_482", "Dave", "Jean-Michel (admin)" };
        static readonly Color[] CursorColors = {
            new(1f, 0.36f, 0.6f), new(0.3f, 0.75f, 1f), new(0.45f, 0.9f, 0.35f), new(1f, 0.7f, 0.2f),
        };
        const string WarningText = "/!\\ NE CONFIEZ RIEN D'IMPORTANT À CE TETRIS   /!\\ LES SCORES NE SONT PAS GARANTIS ET NE LE SERONT JAMAIS   " +
                                   "/!\\ CE JEU N'A PAS ÉTÉ TESTÉ PAR DES PROFESSIONNELS   /!\\ NI PAR DES AMATEURS   ";

        const float EvadeRadius = 120f;
        const float EvadeSpeed = 1100f;
        const float EvadeBudget = 2.5f;
        const float EvadeCooldown = 15f;
        const float EvadeMaxOffset = 280f;
        const int ConfirmChoices = 24;

        static readonly string[] Doubts = {
            "peut-être", "je sais pas", "non", "bof", "plus tard", "on verra", "pas vraiment", "euh...", "ça dépend",
            "pourquoi ?", "j'hésite", "non merci", "laisse-moi réfléchir", "jamais", "probablement pas", "c'est compliqué",
            "oui mais non", "non mais oui", "je préfère pas", "ptet bien", "ouais nan", "pas aujourd'hui", "rappelle-moi",
            "demande à ma mère", "faut voir", "mouais", "pas sûr", "j'sais plus", "c'est quoi la question ?", "nope",
            "absolument pas", "dans tes rêves", "sans façon", "oula non", "ni oui ni non", "pile ou face ?", "hmm",
            "je reviens", "c'est non", "attends",
        };
        static readonly string[] Yeses = {
            "oui", "je suis absolument sûr", "affirmatif", "carrément", "évidemment", "bien sûr que oui", "oui, je le veux",
            "valider", "d'accord", "go", "100 % sûr", "oui chef", "allez",
        };

        class Evasion {
            public Vector2 Base;
            public Vector2 Offset;
            public Vector2 Applied = new(float.NaN, 0f);
            public float Budget = EvadeBudget;
            public float CooldownUntil;
        }

        [Flags]
        enum Changes { None = 0, Rotate = 1, Translate = 2, Font = 4, Opacity = 8, Spacing = 16, Align = 32 }

        static DeplorableUi _instance;

        readonly Dictionary<VisualElement, Changes> _processed = new();
        readonly List<VisualElement> _cursors = new();
        readonly Dictionary<Button, Evasion> _evasions = new();
        float[] _savedVolumes;
        float _nextAudioChaos;
        Button _pressed;
        Button _confirmTarget;
        VisualElement _confirm;
        Label _confirmTitle;
        VisualElement _confirmGrid;
        Font[] _fonts;
        VisualElement _root;
        VisualElement _layer;
        Label _warning;
        Label _saving;
        VisualElement _cookie;
        Label _chatBadge;
        VisualElement _update;
        VisualElement _updateFill;
        bool _isActive;
        float _nextScan;
        float _cookieReturn;
        float _nextNotification;
        float _nextUpdate;
        float _updateStart = -1f;
        int _notifications = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            _instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (_instance == null) {
                var go = new GameObject("Deplorable UI");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<DeplorableUi>();
            }
            _instance.Detach();
        }

        void Update() {
            bool shouldBeActive = SpoofGoofSettings.IsDeplorable;
            if (shouldBeActive && _root == null) Attach();
            if (!shouldBeActive && _isActive) Detach();
            if (!_isActive) return;

            float now = Time.unscaledTime;
            if (now >= _nextScan) {
                _nextScan = now + ScanInterval;
                Scan(_root);
            }

            UpdateWarning(now);
            UpdateSaving(now);
            UpdateCookie(now);
            UpdateChat(now);
            UpdateCursors(now);
            UpdateFakeUpdate(now);
            UpdateEvasion(now, Time.unscaledDeltaTime);
            UpdateAudioChaos(now);
        }

        void OnApplicationQuit() {
            RestoreVolumes();
        }

        // ---------- Mise en place ----------

        /// <summary>S'installe sur la UIDocument principale de la scène (la plus grande).</summary>
        void Attach() {
            VisualElement best = null;
            foreach (UIDocument document in FindObjectsByType<UIDocument>()) {
                VisualElement root = document.rootVisualElement;
                if (root?.panel == null) continue;
                if (best == null || root.childCount > best.childCount) best = root;
            }
            if (best == null) return;

            _root = best;
            _isActive = true;
            _fonts ??= LoadFonts();
            _nextNotification = Time.unscaledTime + 12f;
            _nextUpdate = Time.unscaledTime + Random.Range(25f, 45f);
            _nextAudioChaos = Time.unscaledTime + 3f;
            _savedVolumes ??= new[] { GameSettings.MasterVolume, GameSettings.MusicVolume, GameSettings.SfxVolume, GameSettings.MemeVolume };
            BuildLayer();

            _root.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            _root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
        }

        /// <summary>Retire tout et remet les éléments comme avant.</summary>
        void Detach() {
            if (_root != null) {
                _root.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                _root.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
                _root.UnregisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            }
            if (!SpoofGoofSettings.IsDeplorable) RestoreVolumes();
            _evasions.Clear();
            _pressed = null;
            foreach (KeyValuePair<VisualElement, Changes> pair in _processed) Restore(pair.Key, pair.Value);
            _processed.Clear();
            _cursors.Clear();
            _layer?.RemoveFromHierarchy();
            _layer = null;
            _root = null;
            _isActive = false;
            _updateStart = -1f;
        }

        static Font[] LoadFonts() {
            var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
            var fonts = new List<Font>();
            foreach (string name in PreferredFonts) {
                if (installed.Contains(name)) fonts.Add(Font.CreateDynamicFontFromOSFont(name, 32));
            }

            // Pas de Comic Sans : on mélange des polices du système au hasard, c'est tout aussi moche.
            if (fonts.Count == 0 && installed.Count > 0) {
                var names = new List<string>(installed);
                for (int i = 0; i < 3; i++) fonts.Add(Font.CreateDynamicFontFromOSFont(names[Random.Range(0, names.Count)], 32));
            }
            return fonts.ToArray();
        }

        // ---------- Dégradation des éléments existants ----------

        void Scan(VisualElement root) {
            root.Query<VisualElement>().ForEach(element => {
                if (element == root || _processed.ContainsKey(element) || IsExcluded(element)) return;
                _processed[element] = Degrade(element);
            });
        }

        bool IsExcluded(VisualElement element) {
            for (VisualElement current = element; current != null; current = current.parent) {
                if (current.name == LayerName || current.name == "cursor-trail") return true;
            }
            return false;
        }

        /// <summary>Penche, décale et enlaidit un élément. Ne touche jamais une propriété déjà réglée en code.</summary>
        Changes Degrade(VisualElement element) {
            var changes = Changes.None;
            bool isPanel = element is Button || element.ClassListContains("glass") || element.ClassListContains("options-row");

            if (isPanel || (element is Label && Random.value < 0.3f)) {
                if (element.style.rotate.keyword == StyleKeyword.Null) {
                    float range = isPanel ? 3.5f : 2f;
                    element.style.rotate = new Rotate(Random.Range(-range, range));
                    changes |= Changes.Rotate;
                }
                if (isPanel && element.style.translate.keyword == StyleKeyword.Null) {
                    var offset = new Vector2(Random.Range(-10f, 10f), Random.Range(-6f, 6f));
                    element.style.translate = new Translate(offset.x, offset.y);
                    changes |= Changes.Translate;
                    if (element is Button button) _evasions[button] = new Evasion { Base = offset };
                }
            }

            if (element is TextElement) {
                if (_fonts.Length > 0 && Random.value < 0.4f && element.style.unityFont.keyword == StyleKeyword.Null) {
                    Font font = _fonts[Random.Range(0, _fonts.Length)];
                    element.style.unityFont = font;
                    element.style.unityFontDefinition = FontDefinition.FromFont(font);
                    changes |= Changes.Font;
                }
                if (Random.value < 0.25f && element.style.opacity.keyword == StyleKeyword.Null) {
                    element.style.opacity = Random.Range(0.45f, 0.75f);
                    changes |= Changes.Opacity;
                }
                if (Random.value < 0.3f && element.style.letterSpacing.keyword == StyleKeyword.Null) {
                    element.style.letterSpacing = Random.Range(-1.5f, 6f);
                    changes |= Changes.Spacing;
                }
                if (Random.value < 0.3f && element.style.unityTextAlign.keyword == StyleKeyword.Null) {
                    element.style.unityTextAlign = Random.value < 0.5f ? TextAnchor.UpperLeft : TextAnchor.LowerRight;
                    changes |= Changes.Align;
                }
            }
            return changes;
        }

        static void Restore(VisualElement element, Changes changes) {
            if (changes.HasFlag(Changes.Rotate)) element.style.rotate = StyleKeyword.Null;
            if (changes.HasFlag(Changes.Translate)) element.style.translate = StyleKeyword.Null;
            if (changes.HasFlag(Changes.Font)) {
                element.style.unityFont = StyleKeyword.Null;
                element.style.unityFontDefinition = StyleKeyword.Null;
            }
            if (changes.HasFlag(Changes.Opacity)) element.style.opacity = StyleKeyword.Null;
            if (changes.HasFlag(Changes.Spacing)) element.style.letterSpacing = StyleKeyword.Null;
            if (changes.HasFlag(Changes.Align)) element.style.unityTextAlign = StyleKeyword.Null;
        }

        // ---------- Ajouts ----------

        void BuildLayer() {
            _layer = new VisualElement { name = LayerName, pickingMode = PickingMode.Ignore };
            _layer.AddToClassList("deplorable");
            _root.Add(_layer);

            _warning = Label(_layer, WarningText + WarningText, "deplorable__warning");

            _saving = Label(_layer, "", "deplorable__saving");
            _saving.AddToClassList("glass");

            _cookie = Element(_layer, "deplorable__cookie");
            _cookie.AddToClassList("glass");
            Label(_cookie, "Ce jeu utilise des cookies. (C'est faux. On aime juste demander.)", "deplorable__cookie-text");
            var buttons = Element(_cookie, "deplorable__cookie-buttons");
            Button(buttons, "OSEF", "deplorable__cookie-button", () => {
                _cookie.style.display = DisplayStyle.None;
                _cookieReturn = Time.unscaledTime + CookieReturnDelay;
            });
            Button(buttons, "Gérer les préférences", "deplorable__cookie-link", () => ShowSaving("Préférences enregistrées. Elles ont toutes été ignorées."));

            VisualElement chat = Button(_layer, "?", "deplorable__chat", () => {
                _notifications = 0;
                ShowSaving("Le support est fermé depuis 2009.");
            });
            _chatBadge = Label(chat, "1", "deplorable__badge");

            foreach (string cursorName in CursorNames) CreateFakeCursor(cursorName);

            _update = Element(_layer, "deplorable__update");
            _update.AddToClassList("glass");
            Label(_update, "Installation d'une mise à jour importante", "deplorable__update-title");
            Label(_update, "Ne quittez pas le jeu. Il n'y a aucune raison à cela.", "deplorable__update-text");
            VisualElement bar = Element(_update, "deplorable__update-bar");
            _updateFill = Element(bar, "deplorable__update-fill");
            _update.style.display = DisplayStyle.None;

            _confirm = Element(_layer, "deplorable__confirm");
            _confirm.pickingMode = PickingMode.Position;
            VisualElement panel = Element(_confirm, "deplorable__confirm-panel");
            panel.AddToClassList("glass");
            panel.pickingMode = PickingMode.Position;
            _confirmTitle = Label(panel, "", "deplorable__confirm-title");
            _confirmGrid = Element(panel, "deplorable__confirm-grid");
            _confirmGrid.pickingMode = PickingMode.Position;
            _confirm.style.display = DisplayStyle.None;

            // Le curseur osu! du menu reste au-dessus de tout.
            _root.Q("cursor-trail")?.BringToFront();
        }

        void CreateFakeCursor(string cursorName) {
            int index = _cursors.Count;
            VisualElement cursor = Element(_layer, "deplorable__cursor");
            var arrow = Element(cursor, "deplorable__cursor-arrow");
            arrow.generateVisualContent += context => DrawArrow(context.painter2D);
            Label tag = Label(cursor, cursorName, "deplorable__cursor-name");
            tag.style.backgroundColor = CursorColors[index % CursorColors.Length];
            _cursors.Add(cursor);
        }

        static void DrawArrow(Painter2D painter) {
            painter.fillColor = Color.white;
            painter.strokeColor = Color.black;
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(1, 1));
            painter.LineTo(new Vector2(1, 19));
            painter.LineTo(new Vector2(6, 15));
            painter.LineTo(new Vector2(10, 23));
            painter.LineTo(new Vector2(13, 22));
            painter.LineTo(new Vector2(9, 14));
            painter.LineTo(new Vector2(15, 14));
            painter.ClosePath();
            painter.Fill();
            painter.Stroke();
        }

        // ---------- Animations ----------

        void UpdateWarning(float now) {
            float width = _warning.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0f) return;
            _warning.style.translate = new Translate(-Mathf.Repeat(now * 90f, width / 2f), 0);
        }

        void UpdateSaving(float now) {
            if (_saving.userData is float until && now < until) return;

            int dots = (int)(now * 2.5f) % 4;
            _saving.text = "Enregistrement des modifications" + new string('.', dots);
        }

        void ShowSaving(string message) {
            _saving.text = message;
            _saving.userData = Time.unscaledTime + 3f;
        }

        void UpdateCookie(float now) {
            if (_cookie.style.display == DisplayStyle.None && now >= _cookieReturn) {
                _cookie.style.display = DisplayStyle.Flex;
            }
        }

        void UpdateChat(float now) {
            if (now >= _nextNotification) {
                _nextNotification = now + Random.Range(15f, 30f);
                _notifications++;
            }
            _chatBadge.text = _notifications > 99 ? "99+" : _notifications.ToString();
            _chatBadge.style.display = _notifications > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Les faux joueurs errent sur l'écran, chacun sur son propre bruit de Perlin.</summary>
        void UpdateCursors(float now) {
            Rect area = _layer.contentRect;
            if (area.width <= 0f) return;

            for (int i = 0; i < _cursors.Count; i++) {
                float seed = i * 13.7f + 2.3f;
                float x = Mathf.PerlinNoise(seed, now * 0.12f) * (area.width - 160f) + 40f;
                float y = Mathf.PerlinNoise(now * 0.12f, seed) * (area.height - 100f) + 40f;
                _cursors[i].style.left = x;
                _cursors[i].style.top = y;
            }
        }

        /// <summary>De temps en temps, une "mise à jour" qui monte à 99 %, y reste, puis disparaît sans rien faire.</summary>
        void UpdateFakeUpdate(float now) {
            if (_updateStart < 0f) {
                if (now < _nextUpdate) return;
                _updateStart = now;
                _update.style.display = DisplayStyle.Flex;
            }

            float t = (now - _updateStart) / UpdateDuration;
            _updateFill.style.width = Length.Percent(Mathf.Min(t * 1.6f, 0.99f) * 100f);

            if (t >= 1f) {
                _update.style.display = DisplayStyle.None;
                _updateStart = -1f;
                _nextUpdate = now + Random.Range(60f, 110f);
                ShowSaving("Mise à jour installée. Rien n'a changé.");
            }
        }

        // ---------- Boutons qui fuient ----------

        /// <summary>
        /// Un bouton approché par la souris s'enfuit, pendant <see cref="EvadeBudget"/> secondes en tout.
        /// Ensuite il se laisse attraper, et recommence <see cref="EvadeCooldown"/> secondes plus tard.
        /// </summary>
        void UpdateEvasion(float now, float deltaTime) {
            bool hasMouse = TryGetMouse(out Vector2 mouse);
            foreach (KeyValuePair<Button, Evasion> pair in _evasions) {
                Button button = pair.Key;
                Evasion evasion = pair.Value;
                Rect bounds = button.worldBound;
                if (button.panel == null || bounds.width <= 0f || float.IsNaN(bounds.width)) continue;

                Vector2 away = bounds.center - mouse;
                bool isNear = hasMouse && away.magnitude < EvadeRadius + bounds.width * 0.3f;
                if (isNear && evasion.Budget > 0f && now >= evasion.CooldownUntil) {
                    Vector2 direction = away.sqrMagnitude > 1f ? away.normalized : Random.insideUnitCircle.normalized;
                    evasion.Offset = Vector2.ClampMagnitude(evasion.Offset + direction * (EvadeSpeed * deltaTime), EvadeMaxOffset);
                    evasion.Budget -= deltaTime;
                    if (evasion.Budget <= 0f) evasion.CooldownUntil = now + EvadeCooldown;
                }
                else if (!isNear) {
                    evasion.Offset = Vector2.MoveTowards(evasion.Offset, Vector2.zero, 160f * deltaTime);
                    if (evasion.Budget <= 0f && now >= evasion.CooldownUntil) evasion.Budget = EvadeBudget;
                }

                Vector2 total = evasion.Base + evasion.Offset;
                if (total == evasion.Applied) continue;
                evasion.Applied = total;
                button.style.translate = new Translate(total.x, total.y);
            }
        }

        bool TryGetMouse(out Vector2 position) {
            position = default;
            Mouse mouse = Mouse.current;
            if (mouse == null || _root.panel == null) return false;

            Vector2 screen = mouse.position.ReadValue();
            position = RuntimePanelUtils.ScreenToPanel(_root.panel, new Vector2(screen.x, Screen.height - screen.y));
            return true;
        }

        // ---------- Confirmation à chaque clic ----------

        void OnPointerDown(PointerDownEvent e) {
            if (Random.value < 0.33f) SwitchMusicRandomly();

            Button button = FindButton(e.target as VisualElement);
            if (button == null || IsExcluded(button)) return;

            _pressed = button;
            e.StopImmediatePropagation();
        }

        void OnPointerUp(PointerUpEvent e) {
            if (_pressed == null) return;

            Button button = _pressed;
            _pressed = null;
            e.StopImmediatePropagation();
            if (FindButton(e.target as VisualElement) == button) OpenConfirm(button);
        }

        void OnClick(ClickEvent e) {
            Button button = FindButton(e.target as VisualElement);
            if (button != null && !IsExcluded(button)) e.StopImmediatePropagation();
        }

        static Button FindButton(VisualElement element) {
            for (VisualElement current = element; current != null; current = current.parent) {
                if (current is Button button) return button;
            }
            return null;
        }

        void OpenConfirm(Button target) {
            _confirmTarget = target;
            string text = string.IsNullOrWhiteSpace(target.text) ? "ce bouton" : $"« {target.text.Trim()} »";
            _confirmTitle.text = $"Es-tu sûr de vouloir cliquer sur {text} ?";
            FillConfirm();
            _confirm.style.display = DisplayStyle.Flex;
        }

        /// <summary>Des réponses hésitantes mélangées, et une seule qui accepte, à une place au hasard.</summary>
        void FillConfirm() {
            _confirmGrid.Clear();
            var doubts = new List<string>(Doubts);
            for (int i = doubts.Count - 1; i > 0; i--) {
                int j = Random.Range(0, i + 1);
                (doubts[i], doubts[j]) = (doubts[j], doubts[i]);
            }

            int yesIndex = Random.Range(0, ConfirmChoices);
            for (int i = 0; i < ConfirmChoices; i++) {
                bool isYes = i == yesIndex;
                string text = isYes ? Yeses[Random.Range(0, Yeses.Length)] : doubts[i % doubts.Count];
                Button choice = Button(_confirmGrid, text, "deplorable__choice", isYes ? Confirm : Doubt);
                choice.style.rotate = new Rotate(Random.Range(-6f, 6f));
                choice.style.fontSize = Random.Range(12f, 19f);
            }
        }

        void Confirm() {
            _confirm.style.display = DisplayStyle.None;
            Button target = _confirmTarget;
            _confirmTarget = null;
            if (target?.panel == null) return;

            using NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
            submit.target = target;
            target.SendEvent(submit);
        }

        void Doubt() {
            if (Random.value < 0.35f) {
                ShowSaving("Décide-toi.");
                FillConfirm();
                return;
            }
            _confirm.style.display = DisplayStyle.None;
            _confirmTarget = null;
            ShowSaving("Action annulée. Tu n'avais pas l'air sûr.");
        }

        // ---------- Son ----------

        static void SwitchMusicRandomly() {
            FindAnyObjectByType<MusicCarousel>()?.SelectRandom();
            LockerGameplay.Instance?.RandomTrack();
        }

        /// <summary>Les volumes changent tout seuls toutes les quelques secondes.</summary>
        void UpdateAudioChaos(float now) {
            if (now < _nextAudioChaos) return;
            _nextAudioChaos = now + Random.Range(2.5f, 5f);

            GameSettings.MasterVolume = Random.Range(0.4f, 1f);
            GameSettings.MusicVolume = Random.Range(0.1f, 1f);
            GameSettings.SfxVolume = Random.Range(0.1f, 1f);
            GameSettings.MemeVolume = Random.Range(0.1f, 1f);
        }

        void RestoreVolumes() {
            if (_savedVolumes == null) return;

            GameSettings.MasterVolume = _savedVolumes[0];
            GameSettings.MusicVolume = _savedVolumes[1];
            GameSettings.SfxVolume = _savedVolumes[2];
            GameSettings.MemeVolume = _savedVolumes[3];
            GameSettings.Save();
            _savedVolumes = null;
        }

        // ---------- Outils ----------

        static VisualElement Element(VisualElement parent, string className) {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            parent.Add(element);
            return element;
        }

        static Label Label(VisualElement parent, string text, string className) {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            parent.Add(label);
            return label;
        }

        /// <summary>Bouton cliquable mais qui ne prend pas le focus (Espace et Entrée restent au jeu).</summary>
        static Button Button(VisualElement parent, string text, string className, Action onClick) {
            var button = new Button(onClick) { text = text, focusable = false };
            button.AddToClassList(className);
            parent.Add(button);
            return button;
        }
    }
}
