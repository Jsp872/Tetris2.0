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
    /// <item>Musique : N passe à la musique suivante parmi celles possédées (et l'équipe), avec un bandeau "en cours".</item>
    /// </list>
    /// Les blocs, le plateau, les sons et la musique de départ sont appliqués par Bloc, BoardVisual, GameplaySfx et MusicPlayer.
    /// </summary>
    public class LockerGameplay : MonoBehaviour {
        const float ToastDuration = 2.6f;
        const float CoverBrightness = 0.42f;

        MusicPlayer _player;
        LiquidBackground _liquid;
        SpriteRenderer _cover;
        Label _toast;
        IVisualElementScheduledItem _hideToast;
        bool _usesCover;
        LockerItem _coverMusic;

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
            ApplyBackground(PlayerInventory.GetEquipped(LockerCategory.Background));
            CreateToast();
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

            _coverMusic = LockerContent.CurrentGameplayMusic;
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
            if (_usesCover && _coverMusic != LockerContent.CurrentGameplayMusic) {
                RefreshCover();
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.nKey.wasPressedThisFrame) {
                NextMusic();
            }
        }

        /// <summary>Passe à la musique possédée suivante, l'équipe et l'annonce.</summary>
        void NextMusic() {
            if (_player == null) return;

            List<LockerItem> owned = LockerContent.GetOwnedMusic();
            if (owned.Count == 0) return;

            int index = owned.IndexOf(LockerContent.CurrentGameplayMusic);
            LockerItem next = owned[(index + 1) % owned.Count];
            LockerContent.CurrentGameplayMusic = next;
            PlayerInventory.Equip(next);

            AudioClip clip = next.IsSceneDefault ? _player.DefaultMusic : LockerContent.GetMusicClip(next);
            if (clip != null) _player.Play(clip);

            ShowToast(next);
        }

        void CreateToast() {
            var document = FindAnyObjectByType<HUD>()?.GetComponent<UIDocument>();
            if (document == null || document.rootVisualElement == null) return;

            _toast = new Label { pickingMode = PickingMode.Ignore };
            _toast.AddToClassList("glass");
            _toast.style.position = Position.Absolute;
            _toast.style.top = 34;
            _toast.style.alignSelf = Align.Center;
            _toast.style.paddingTop = 12;
            _toast.style.paddingBottom = 12;
            _toast.style.paddingLeft = 30;
            _toast.style.paddingRight = 30;
            _toast.style.fontSize = 22;
            _toast.style.color = Color.white;
            _toast.style.unityTextAlign = TextAnchor.MiddleCenter;
            _toast.style.opacity = 0f;
            _toast.style.transitionProperty = new List<StylePropertyName> { "opacity", "translate" };
            _toast.style.transitionDuration = new List<TimeValue> { new(0.25f), new(0.35f) };
            _toast.style.translate = new Translate(0, -20);
            document.rootVisualElement.Add(_toast);
        }

        void ShowToast(LockerItem music) {
            if (_toast == null) return;

            string artist = string.IsNullOrEmpty(music.Artist) ? "" : $"  ·  {music.Artist}";
            _toast.text = $"EN COURS : {music.Name}{artist}";
            _toast.style.opacity = 1f;
            _toast.style.translate = new Translate(0, 0);

            _hideToast?.Pause();
            _hideToast = _toast.schedule.Execute(() => {
                _toast.style.opacity = 0f;
                _toast.style.translate = new Translate(0, -20);
            }).StartingIn((long)(ToastDuration * 1000f));
        }
    }
}
