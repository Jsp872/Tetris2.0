using UnityEngine;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Carrousel vertical de musiques du menu principal : illustration de la musique en cours au centre,
    /// précédente au-dessus, suivante en dessous. Flèches, molette ou clic sur une illustration pour changer.
    /// Le fond du menu suit la musique avec un fondu enchaîné (deux calques qui alternent).
    /// La musique de départ est tirée au hasard.
    /// </summary>
    public class MusicCarousel : MonoBehaviour {
        const string ShiftUpClass = "carousel--shift-up";
        const string ShiftDownClass = "carousel--shift-down";
        const string LayerHiddenClass = "menu-background__layer--hidden";

        [SerializeField] UIDocument _document;
        [SerializeField] MusicLibrarySO _library;
        [SerializeField] MusicPlayer _player;
        [Tooltip("Fournit la vidéo pour les musiques qui l'utilisent comme illustration.")]
        [SerializeField] MenuVideoBackground _menuVideo;

        VisualElement _carousel;
        VisualElement _previous;
        VisualElement _current;
        VisualElement _next;
        Label _title;
        Label _artist;
        VisualElement[] _equalizerBars;
        VisualElement[] _backgroundLayers;
        int _activeLayer = -1;
        bool _isBackgroundVideo;
        int _index;

        int Count => _library != null && _library.tracks != null ? _library.tracks.Length : 0;

        void Start() {
            if (Count == 0) return;

            VisualElement root = _document.rootVisualElement;
            _carousel = root.Q("music-carousel");
            _previous = root.Q("music-previous");
            _current = root.Q("music-current");
            _next = root.Q("music-next");
            _title = root.Q<Label>("music-title");
            _artist = root.Q<Label>("music-artist");
            _equalizerBars = root.Query(className: "equalizer__bar").ToList().ToArray();
            _backgroundLayers = new[] { root.Q("background-a"), root.Q("background-b") };

            root.Q<Button>("music-previous-button").clicked += () => Select(_index - 1);
            root.Q<Button>("music-next-button").clicked += () => Select(_index + 1);
            _previous.RegisterCallback<ClickEvent>(_ => Select(_index - 1));
            _next.RegisterCallback<ClickEvent>(_ => Select(_index + 1));
            _carousel.RegisterCallback<WheelEvent>(e => Select(_index + (e.delta.y > 0 ? 1 : -1)));

            _index = Random.Range(0, Count);
            Refresh();
            ShowBackground(_library.tracks[_index]);
            _player.Play(_library.tracks[_index].clip);
        }

        /// <summary>Passe à une musique au hasard (interface déplorable).</summary>
        public void SelectRandom() {
            if (Count > 1) Select(_index + Random.Range(1, Count));
        }

        void Select(int index) {
            int direction = index > _index ? 1 : -1;
            _index = (index % Count + Count) % Count;

            MusicLibrarySO.Track track = _library.tracks[_index];
            _player.Play(track.clip);
            ShowBackground(track);

            // Petite glissade dans le sens du défilement, puis mise à jour des illustrations.
            string shift = direction > 0 ? ShiftUpClass : ShiftDownClass;
            _carousel.AddToClassList(shift);
            _carousel.schedule.Execute(() => {
                Refresh();
                _carousel.RemoveFromClassList(shift);
            }).StartingIn(110);
        }

        void Refresh() {
            MusicLibrarySO.Track track = _library.tracks[_index];
            _current.style.backgroundImage = GetImage(track);
            _previous.style.backgroundImage = GetImage(_library.tracks[(_index - 1 + Count) % Count]);
            _next.style.backgroundImage = GetImage(_library.tracks[(_index + 1) % Count]);
            _title.text = track.title;
            _artist.text = track.artist;
        }

        /// <summary>
        /// Fondu enchaîné : le nouveau fond passe au premier plan et apparaît (avec un léger dézoom)
        /// pendant que l'ancien s'efface. Les durées sont dans MainMenu.uss (.menu-background__layer).
        /// </summary>
        void ShowBackground(MusicLibrarySO.Track track) {
            int newLayer = (_activeLayer + 1) % _backgroundLayers.Length;
            VisualElement layer = _backgroundLayers[newLayer];
            VisualElement image = layer.Q(className: "menu-background__image");

            image.style.backgroundImage = GetImage(track);
            image.style.opacity = 1f;
            layer.BringToFront();
            layer.RemoveFromClassList(LayerHiddenClass);

            if (_activeLayer >= 0) {
                _backgroundLayers[_activeLayer].AddToClassList(LayerHiddenClass);
            }

            _activeLayer = newLayer;
            _isBackgroundVideo = UsesVideo(track);
        }

        StyleBackground GetImage(MusicLibrarySO.Track track) {
            return UsesVideo(track)
                ? new StyleBackground(Background.FromRenderTexture(_menuVideo.Texture))
                : new StyleBackground(track.cover);
        }

        bool UsesVideo(MusicLibrarySO.Track track) {
            return track.useMenuVideoAsCover && _menuVideo != null && _menuVideo.Texture != null;
        }

        void Update() {
            if (_equalizerBars == null) return;

            // Fondu à chaque boucle quand le fond est la vidéo.
            if (_isBackgroundVideo) {
                _backgroundLayers[_activeLayer].Q(className: "menu-background__image").style.opacity = _menuVideo.LoopFade;
            }

            // Égaliseur décoratif à côté du titre.
            for (int i = 0; i < _equalizerBars.Length; i++) {
                float t = Time.unscaledTime * (5f + i * 1.7f) + i * 1.3f;
                float height = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(t) * Mathf.Cos(t * 0.37f));
                _equalizerBars[i].style.height = Length.Percent(height * 100f);
            }
        }
    }
}
