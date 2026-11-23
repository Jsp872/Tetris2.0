using System;
using System.Collections.Generic;
using UnityEngine;
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
    /// </list>
    /// Tout reste utilisable : les ajouts ne bloquent pas les clics et ne prennent pas le focus.
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

        [Flags]
        enum Changes { None = 0, Rotate = 1, Translate = 2, Font = 4, Opacity = 8, Spacing = 16, Align = 32 }

        static DeplorableUi _instance;

        readonly Dictionary<VisualElement, Changes> _processed = new();
        readonly List<VisualElement> _cursors = new();
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
            BuildLayer();
        }

        /// <summary>Retire tout et remet les éléments comme avant.</summary>
        void Detach() {
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
                    element.style.translate = new Translate(Random.Range(-10f, 10f), Random.Range(-6f, 6f));
                    changes |= Changes.Translate;
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
