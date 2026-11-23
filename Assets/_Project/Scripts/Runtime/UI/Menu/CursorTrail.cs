using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Curseur rond façon skin osu! (le curseur système est caché) et traînée qui le suit dans le menu,
    /// aux couleurs du curseur équipé dans le Casier. Le cercle grossit pendant un clic.
    /// Façon osu! : tant que C est maintenue, le curseur laisse des traces qui restent à l'écran.
    /// Une barre façon combo de Maxwell montre le temps qui reste avant qu'elles s'effacent :
    /// pleine tant que C est maintenue, elle se vide après le relâchement, et tout s'efface quand elle est vide.
    /// Réappuyer sur C avant la fin remet le temps à zéro et garde le dessin.
    /// Ajouté tout seul à la scène qui contient le <see cref="MainMenu"/>. Ignoré pendant la saisie dans un champ de texte.
    /// </summary>
    public class CursorTrail : MonoBehaviour {
        const float TrailLifetime = 0.32f;
        const float TrailWidth = 9f;
        const float DrawWidth = 6f;
        const float MinDistance = 3f;
        const float KeepDuration = 3f;
        const float EraseDuration = 0.5f;
        const int MaxDrawPoints = 8000;
        const string BarHiddenClass = "osu-bar--hidden";
        const float CursorRadius = 15f;

        struct Point {
            public Vector2 Position;
            public float Time;
            public int Stroke;
        }

        readonly List<Point> _trail = new();
        readonly List<Point> _drawing = new();
        VisualElement _layer;
        VisualElement _bar;
        VisualElement _barFill;
        Label _barLabel;
        bool _isDrawing;
        bool _isBarVisible;
        float _releaseTime;
        float _eraseStart = -1f;
        int _stroke;
        LockerItem _style;
        Vector2 _cursor;
        bool _hasCursor;
        float _press;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            MainMenu menu = FindAnyObjectByType<MainMenu>();
            if (menu != null) menu.gameObject.AddComponent<CursorTrail>();
        }

        void Start() {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            _layer = new VisualElement { name = "cursor-trail", pickingMode = PickingMode.Ignore };
            _layer.style.position = Position.Absolute;
            _layer.style.left = 0;
            _layer.style.top = 0;
            _layer.style.right = 0;
            _layer.style.bottom = 0;
            _layer.generateVisualContent += Draw;
            root.Add(_layer);

            CreateBar(root);
        }

        /// <summary>Même habillage que la barre de combo de Maxwell (classes osu-bar dans MainMenu.uss).</summary>
        void CreateBar(VisualElement root) {
            _bar = new VisualElement { pickingMode = PickingMode.Ignore };
            _bar.AddToClassList("osu-bar");
            _bar.AddToClassList(BarHiddenClass);

            _barLabel = new Label("DESSIN") { pickingMode = PickingMode.Ignore };
            _barLabel.AddToClassList("osu-bar__label");
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.AddToClassList("osu-bar__track");
            _barFill = new VisualElement { pickingMode = PickingMode.Ignore };
            _barFill.AddToClassList("osu-bar__fill");
            var shine = new VisualElement { pickingMode = PickingMode.Ignore };
            shine.AddToClassList("osu-bar__shine");

            _barFill.Add(shine);
            track.Add(_barFill);
            _bar.Add(_barLabel);
            _bar.Add(track);
            root.Add(_bar);

            var hint = new Label("MAINTIENS C POUR DESSINER") { pickingMode = PickingMode.Ignore };
            hint.AddToClassList("osu-hint");
            root.Add(hint);
        }

        void OnDisable() {
            UnityEngine.Cursor.visible = true;
        }

        void Update() {
            if (_layer?.panel == null) return;

            UnityEngine.Cursor.visible = false;
            Mouse pointer = Mouse.current;
            bool isPressed = pointer != null && pointer.leftButton.isPressed;
            _press = Mathf.MoveTowards(_press, isPressed ? 1f : 0f, Time.unscaledDeltaTime * 10f);

            _style = PlayerInventory.GetEquipped(LockerCategory.Cursor);
            float now = Time.unscaledTime;
            UpdateDrawingKey(now);
            AddPoint(now);

            // Traînée : chaque point vit un court instant.
            while (_trail.Count > 0 && now - _trail[0].Time > TrailLifetime) _trail.RemoveAt(0);

            // Dessin : effacé en fondu quand le temps est écoulé.
            if (!_isDrawing && _drawing.Count > 0 && _eraseStart < 0f && now - _releaseTime >= KeepDuration) {
                _eraseStart = now;
            }
            if (_eraseStart >= 0f && now - _eraseStart >= EraseDuration) {
                _drawing.Clear();
                _eraseStart = -1f;
            }

            UpdateBar(now);
            _layer.MarkDirtyRepaint();
        }

        void UpdateDrawingKey(float now) {
            Keyboard keyboard = Keyboard.current;
            bool isPressed = keyboard != null && keyboard.cKey.isPressed && !IsTyping();

            if (isPressed && !_isDrawing) {
                _isDrawing = true;
                _eraseStart = -1f;
                _stroke++;
            }
            else if (!isPressed && _isDrawing) {
                _isDrawing = false;
                _releaseTime = now;
            }
        }

        void AddPoint(float now) {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screen = mouse.position.ReadValue();
            Vector2 position = RuntimePanelUtils.ScreenToPanel(_layer.panel, new Vector2(screen.x, Screen.height - screen.y));
            var point = new Point { Position = position, Time = now, Stroke = _stroke };
            _cursor = position;
            _hasCursor = true;

            if (_trail.Count == 0 || Vector2.Distance(_trail[_trail.Count - 1].Position, position) >= MinDistance) {
                _trail.Add(point);
            }

            if (!_isDrawing || _drawing.Count >= MaxDrawPoints) return;

            int last = _drawing.Count - 1;
            if (last < 0 || _drawing[last].Stroke != _stroke || Vector2.Distance(_drawing[last].Position, position) >= MinDistance) {
                _drawing.Add(point);
            }
        }

        void UpdateBar(float now) {
            bool isVisible = _isDrawing || _drawing.Count > 0;
            if (isVisible != _isBarVisible) {
                _isBarVisible = isVisible;
                _bar.EnableInClassList(BarHiddenClass, !isVisible);
            }
            if (!isVisible) return;

            float timeLeft = _isDrawing ? KeepDuration : Mathf.Max(KeepDuration - (now - _releaseTime), 0f);
            float fill = timeLeft / KeepDuration;
            _barFill.style.width = Length.Percent(Mathf.Max(fill, 0.02f) * 100f);
            _barFill.style.backgroundColor = _isDrawing
                ? Color.HSVToRGB(now * 1.5f % 1f, 0.85f, 1f)
                : Color.HSVToRGB(Mathf.Lerp(0f, 0.33f, fill), 0.85f, 1f);
            _barLabel.text = _isDrawing
                ? "DESSIN EN COURS"
                : $"EFFACEMENT DANS {timeLeft.ToString("0.0", CultureInfo.InvariantCulture)} S";
        }

        bool IsTyping() {
            for (var element = _layer.panel.focusController.focusedElement as VisualElement; element != null; element = element.parent) {
                if (element is TextField) return true;
            }
            return false;
        }

        // ---------- Rendu ----------

        void Draw(MeshGenerationContext context) {
            if (_style == null) return;

            Painter2D painter = context.painter2D;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            float now = Time.unscaledTime;

            float drawingAlpha = _eraseStart >= 0f ? 1f - (now - _eraseStart) / EraseDuration : 1f;
            for (int i = 1; i < _drawing.Count; i++) {
                if (_drawing[i].Stroke != _drawing[i - 1].Stroke) continue;

                Color color = DrawingColor(i, now);
                color.a *= drawingAlpha;
                Segment(painter, _drawing[i - 1].Position, _drawing[i].Position, DrawWidth, color);
            }

            bool hasTrail = _style.IsRainbow || (_style.Colors != null && _style.Colors.Length > 0);
            if (hasTrail) {
                for (int i = 1; i < _trail.Count; i++) {
                    float age = (now - _trail[i].Time) / TrailLifetime;
                    Color color = TrailColor(age, now);
                    color.a *= 1f - age;
                    Segment(painter, _trail[i - 1].Position, _trail[i].Position, Mathf.Lerp(TrailWidth, 1f, age), color);
                }
            }

            DrawCursor(painter, now, hasTrail);
        }

        /// <summary>Cercle façon osu! : anneau coloré, halo et point central. Il grossit pendant un clic.</summary>
        void DrawCursor(Painter2D painter, float now, bool hasTrail) {
            if (!_hasCursor) return;

            Color color = hasTrail ? TrailColor(0f, now) : Color.white;
            float radius = CursorRadius * (1f + 0.3f * _press) * (_isDrawing ? 0.8f : 1f);

            Color glow = color;
            glow.a = 0.22f;
            Circle(painter, _cursor, radius + 7f, glow);
            Circle(painter, _cursor, radius, new Color(0f, 0f, 0f, 0.35f));

            painter.strokeColor = color;
            painter.lineWidth = 4f;
            painter.BeginPath();
            painter.Arc(_cursor, radius, 0f, 360f);
            painter.Stroke();

            Circle(painter, _cursor, 3.5f, Color.white);
        }

        static void Circle(Painter2D painter, Vector2 center, float radius, Color color) {
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f);
            painter.Fill();
        }

        static void Segment(Painter2D painter, Vector2 from, Vector2 to, float width, Color color) {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(to);
            painter.Stroke();
        }

        /// <summary>Couleur de la traînée : de la tête (0) à la queue (1).</summary>
        Color TrailColor(float age, float now) {
            if (_style.IsRainbow) return Color.HSVToRGB(Mathf.Repeat(now * 0.6f + age * 0.5f, 1f), 0.8f, 1f);
            return Gradient(_style.Colors, age, now);
        }

        /// <summary>Le dessin garde une couleur par point ; arc-en-ciel le long du trait par défaut.</summary>
        Color DrawingColor(int index, float now) {
            if (_style.IsRainbow || _style.Colors == null || _style.Colors.Length == 0) {
                return Color.HSVToRGB(Mathf.Repeat(index * 0.004f, 1f), 0.75f, 1f);
            }
            return Gradient(_style.Colors, Mathf.PingPong(index * 0.01f, 1f), now);
        }

        Color Gradient(Color[] colors, float t, float now) {
            if (colors.Length == 1) return colors[0];

            float scaled = Mathf.Clamp01(t) * (colors.Length - 1);
            int index = Mathf.Min((int)scaled, colors.Length - 2);
            Color color = Color.Lerp(colors[index], colors[index + 1], scaled - index);

            // Paillettes : scintillement aléatoire.
            if (_style.Id == "cursor.gold") color *= 0.75f + 0.5f * Mathf.PerlinNoise(t * 40f, now * 8f);
            color.a = 1f;
            return color;
        }
    }
}
