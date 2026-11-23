using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Applique le Casier pendant une partie. Ajouté tout seul aux scènes qui ont un <see cref="GameManager"/> :
    /// <list type="bullet">
    /// <item>Fond : couleurs du fond liquide, néant, boule à facettes, ou cover de la musique en cours derrière le plateau.</item>
    /// <item>Musique : playlist façon carrousel du menu, à droite (<see cref="GameplayPlaylist"/>), N et B au clavier.</item>
    /// </list>
    /// Les blocs, le plateau, les sons et la musique de départ sont appliqués par Bloc, BoardVisual, GameplaySfx et MusicPlayer.
    /// </summary>
    public class LockerGameplay : MonoBehaviour {
        const float CoverBrightness = 0.42f;

        MusicPlayer _player;
        LiquidBackground _liquid;
        SpriteRenderer _cover;
        GameplayPlaylist _playlist;
        LockerItem _syncedMusic;
        bool _hasSelected;
        bool _usesCover;
        Vector3 _cameraOrigin;

        /// <summary>Composant de la partie en cours, ou null hors partie.</summary>
        public static LockerGameplay Instance { get; private set; }

        /// <summary>Change la musique au hasard (interface déplorable).</summary>
        public void RandomTrack() => _playlist?.SelectRandom();

        void Awake() => Instance = this;

        void OnDestroy() {
            if (Instance == this) Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (FindAnyObjectByType<GameManager>() == null) return;

            new GameObject("Locker").AddComponent<LockerGameplay>();
        }

        void Start() {
            _player = FindAnyObjectByType<MusicPlayer>();
            _liquid = FindAnyObjectByType<LiquidBackground>();
            if (Camera.main != null) _cameraOrigin = Camera.main.transform.position;
            ApplyBackground(PlayerInventory.GetEquipped(LockerCategory.Background));
            CreatePlaylist();
        }

        void ApplyBackground(LockerItem background) {
            switch (background.BackgroundStyle) {
                case BackgroundStyle.Void:
                    if (_liquid != null) _liquid.SetColors(new Color[0]);
                    break;
                case BackgroundStyle.Disco:
                    if (_liquid != null) _liquid.HueSpeed = 0.15f;
                    break;
                case BackgroundStyle.Cover:
                    _usesCover = true;
                    CreateCover();
                    break;
                default:
                    if (background.Colors != null && _liquid != null) _liquid.SetColors(background.Colors);
                    break;
            }
        }

        /// <summary>Image plein écran derrière tout le reste, assombrie pour garder le plateau lisible.</summary>
        void CreateCover() {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            var cover = new GameObject("Cover Background");
            cover.transform.SetParent(mainCamera.transform, false);
            cover.transform.localPosition = new Vector3(0f, 0f, 20f);
            _cover = cover.AddComponent<SpriteRenderer>();
            _cover.sortingOrder = -200;
            _cover.color = new Color(CoverBrightness, CoverBrightness, CoverBrightness, 1f);
            RefreshCover();
        }

        void RefreshCover() {
            if (_cover == null) return;

            Sprite sprite = LockerContent.GetCoverSprite(LockerContent.CurrentGameplayMusic);
            _cover.sprite = sprite;
            if (sprite == null) return;

            // Remplit l'écran comme un "cover" CSS.
            Camera mainCamera = Camera.main;
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            Vector2 size = sprite.bounds.size;
            float scale = Mathf.Max(width / size.x, height / size.y);
            _cover.transform.localScale = Vector3.one * scale;
        }

        void Update() {
            // La musique est choisie dans MusicPlayer.Start, qui peut passer après ce composant.
            if (_syncedMusic != LockerContent.CurrentGameplayMusic) {
                _syncedMusic = LockerContent.CurrentGameplayMusic;
                if (_usesCover) RefreshCover();
                if (_playlist != null && !_hasSelected) _playlist.SyncWithCurrentMusic();
            }

            Keyboard keyboard = Keyboard.current;
            if (_playlist != null && keyboard != null) {
                if (keyboard.nKey.wasPressedThisFrame) Select(true);
                else if (keyboard.bKey.wasPressedThisFrame) Select(false);
            }
            _playlist?.Update();
            _playlist?.AdvanceIfFinished();
        }

        void Select(bool next) {
            _hasSelected = true;
            if (next) _playlist.Next();
            else _playlist.Previous();
        }

        /// <summary>
        /// Active la playlist du HUD : le bloc de jeu se décale à gauche pour laisser la place au carrousel,
        /// et la caméra se décale d'autant pour que le plateau reste dans son cadre.
        /// </summary>
        void CreatePlaylist() {
            HUD hud = FindAnyObjectByType<HUD>();
            VisualElement root = hud != null ? hud.GetComponent<UIDocument>().rootVisualElement : null;
            if (root == null || _player == null) return;

            var playlist = new GameplayPlaylist(root, _player);
            if (!playlist.IsValid) return;
            _playlist = playlist;

            VisualElement layout = root.Q("root");
            VisualElement boardSpace = root.Q(className: "board-space");
            if (layout == null || boardSpace == null) return;

            // Sans la playlist, le plateau est centré à l'écran : le décalage est l'écart au centre.
            layout.AddToClassList("root--playlist");
            boardSpace.RegisterCallback<GeometryChangedEvent>(_ => ShiftCamera(root, root.worldBound.center.x - boardSpace.worldBound.center.x));
        }

        /// <summary>Décale la caméra de <paramref name="panelOffset"/> pixels de l'interface vers la droite (le plateau part à gauche).</summary>
        void ShiftCamera(VisualElement root, float panelOffset) {
            Camera mainCamera = Camera.main;
            float panelWidth = root.worldBound.width;
            if (mainCamera == null || panelWidth <= 0f) return;

            float worldPerPanelPixel = mainCamera.orthographicSize * 2f * mainCamera.aspect / panelWidth;
            Vector3 position = _cameraOrigin + mainCamera.transform.right * (panelOffset * worldPerPanelPixel);
            mainCamera.transform.position = position;
        }
    }
}
