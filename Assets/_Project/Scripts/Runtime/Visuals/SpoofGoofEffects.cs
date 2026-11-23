using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Effets visuels des options Spoof Goof pendant une partie (<see cref="SpoofGoofSettings"/>).
    /// Ajouté tout seul à chaque scène qui contient un <see cref="GameManager"/> quand une option est active :
    /// rien à placer dans la scène.
    /// <list type="bullet">
    /// <item>Inversion : le HUD est retourné (la ligne de limite est gérée par BoardVisual, la défaite par le GameManager).</item>
    /// <item>Mal de mer : la caméra et le HUD tanguent ensemble.</item>
    /// <item>Disco : vague arc-en-ciel sur la pièce, son fantôme et la pile.</item>
    /// <item>Pièces bourrées : les cases de la pièce titubent, et restent de travers une fois posées.</item>
    /// <item>Poisson rouge : les blocs posés s'effacent peu à peu.</item>
    /// </list>
    /// </summary>
    public class SpoofGoofEffects : MonoBehaviour {
        const float GoldfishDelay = 2.5f;
        const float GoldfishFadeDuration = 1.5f;
        const float GoldfishMinAlpha = 0.05f;
        const float CleanupInterval = 2f;

        struct LockedCell {
            public SpriteRenderer Renderer;
            public Color Color;
            public float LockTime;
        }

        readonly List<VisualElement> _uiRoots = new();
        readonly Dictionary<Transform, LockedCell> _lockedCells = new();
        readonly List<Transform> _destroyedCells = new();
        Board _board;
        Transform _camera;
        Quaternion _cameraRotation;
        float _nextCleanup;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (!SpoofGoofSettings.AnyActive || FindAnyObjectByType<GameManager>() == null) return;

            new GameObject("Spoof Goof").AddComponent<SpoofGoofEffects>();
        }

        void Start() {
            _board = FindAnyObjectByType<Board>();

            Camera mainCamera = Camera.main;
            if (mainCamera != null) {
                _camera = mainCamera.transform;
                _cameraRotation = _camera.rotation;
            }

            foreach (UIDocument document in FindObjectsByType<UIDocument>()) {
                if (document.rootVisualElement != null) _uiRoots.Add(document.rootVisualElement);
            }

            if (SpoofGoofSettings.IsInverted) {
                foreach (VisualElement root in _uiRoots) {
                    root.style.scale = new Scale(new Vector3(1f, -1f, 1f));
                }
            }
        }

        void Update() {
            float time = Time.unscaledTime;
            UpdateSeasickness(time);
            UpdateCurrentPiece(time);
            UpdateLockedCells(time);
        }

        /// <summary>Deux sinus de fréquences différentes : un roulis irrégulier, comme sur un bateau.</summary>
        void UpdateSeasickness(float time) {
            float maxAngle = SpoofGoofSettings.SwayAngle;
            if (maxAngle <= 0f) return;

            float angle = (Mathf.Sin(time * 0.7f) + 0.35f * Mathf.Sin(time * 1.9f)) / 1.35f * maxAngle;
            if (_camera != null) {
                _camera.rotation = _cameraRotation * Quaternion.Euler(0f, 0f, angle);
            }
            foreach (VisualElement root in _uiRoots) {
                root.style.rotate = new Rotate(angle);
            }
        }

        void UpdateCurrentPiece(float time) {
            Bloc bloc = Bloc.Current;
            float drunkAngle = SpoofGoofSettings.DrunkAngle;
            bool isDisco = SpoofGoofSettings.IsDisco;
            if (bloc == null || (drunkAngle <= 0f && !isDisco)) return;

            Color ghostColor = bloc.Color;
            ghostColor.a = Bloc.GhostAlpha;

            for (int i = 0; i < bloc.Cells.Count; i++) {
                StyleCell(bloc.Cells[i], i, time, drunkAngle, isDisco, bloc.Color);
                StyleCell(bloc.GhostCells[i], i, time, drunkAngle, isDisco, ghostColor);
            }
        }

        static void StyleCell(Transform cell, int index, float time, float drunkAngle, bool isDisco, Color baseColor) {
            if (drunkAngle > 0f) {
                float wobble = Mathf.Sin(time * 5f + index * 1.9f);
                cell.localRotation = Quaternion.Euler(0f, 0f, wobble * drunkAngle);
                cell.localScale = Vector3.one * (1f + 0.004f * drunkAngle * Mathf.Sin(time * 7.3f + index));
            }

            if (isDisco) {
                Color color = DiscoColor(cell.position, time);
                color.a = baseColor.a;
                cell.GetComponent<SpriteRenderer>().color = color;
            }
        }

        void UpdateLockedCells(float time) {
            bool isDisco = SpoofGoofSettings.IsDisco;
            bool isGoldfish = SpoofGoofSettings.IsGoldfish;
            if (_board == null || (!isDisco && !isGoldfish)) return;

            float now = Time.time;
            for (int x = 0; x < _board.Width; x++) {
                for (int y = 0; y < _board.Height; y++) {
                    Transform cell = _board.GetCell(x, y);
                    if (cell == null) continue;

                    if (!_lockedCells.TryGetValue(cell, out LockedCell info)) {
                        var spriteRenderer = cell.GetComponent<SpriteRenderer>();
                        info = new LockedCell { Renderer = spriteRenderer, Color = spriteRenderer.color, LockTime = now };
                        _lockedCells.Add(cell, info);
                    }

                    Color color = isDisco ? DiscoColor(cell.position, time) : info.Color;
                    color.a = info.Color.a;
                    if (isGoldfish) {
                        color.a *= Mathf.Lerp(1f, GoldfishMinAlpha, (now - info.LockTime - GoldfishDelay) / GoldfishFadeDuration);
                    }
                    info.Renderer.color = color;
                }
            }

            if (now >= _nextCleanup) {
                _nextCleanup = now + CleanupInterval;
                RemoveDestroyedCells();
            }
        }

        /// <summary>Oublie les cases détruites par les lignes effacées.</summary>
        void RemoveDestroyedCells() {
            _destroyedCells.Clear();
            foreach (Transform cell in _lockedCells.Keys) {
                if (cell == null) _destroyedCells.Add(cell);
            }
            foreach (Transform cell in _destroyedCells) {
                _lockedCells.Remove(cell);
            }
        }

        /// <summary>Teinte qui défile en diagonale sur la grille.</summary>
        static Color DiscoColor(Vector3 position, float time) {
            float hue = Mathf.Repeat(time * 0.35f + (position.x + position.y) * 0.06f, 1f);
            return Color.HSVToRGB(hue, 0.7f, 1f);
        }
    }
}
