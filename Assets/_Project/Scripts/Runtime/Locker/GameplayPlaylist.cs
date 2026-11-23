using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Playlist en jeu : le même carrousel que le menu principal (élément "music-carousel" du HUD),
    /// avec les musiques possédées dans le Casier. Flèches, molette ou clic sur une cover pour changer,
    /// ou N (suivante) et B (précédente) au clavier. La musique choisie est jouée tout de suite et équipée
    /// (sauf si "Aléatoire" est équipé : il le reste).
    /// Les boutons ne prennent pas le focus, pour que Espace et Entrée restent aux commandes du jeu.
    /// </summary>
    public class GameplayPlaylist {
        const string ShiftUpClass = "carousel--shift-up";
        const string ShiftDownClass = "carousel--shift-down";

        readonly MusicPlayer _player;
        readonly List<LockerItem> _tracks;
        readonly VisualElement _carousel;
        readonly VisualElement _previous;
        readonly VisualElement _current;
        readonly VisualElement _next;
        readonly Label _placeholder;
        readonly Label _title;
        readonly Label _artist;
        readonly VisualElement[] _equalizerBars;
        int _index;

        /// <summary>Le HUD a bien le carrousel : la playlist est utilisable.</summary>
        public bool IsValid => _carousel != null;

        public GameplayPlaylist(VisualElement root, MusicPlayer player) {
            _player = player;
            _tracks = LockerContent.GetOwnedMusic();
            _carousel = root.Q("music-carousel");
            if (_carousel == null || _tracks.Count == 0) {
                _carousel = null;
                return;
            }

            _previous = _carousel.Q("music-previous");
            _current = _carousel.Q("music-current");
            _next = _carousel.Q("music-next");
            _placeholder = _carousel.Q<Label>("music-placeholder");
            _title = _carousel.Q<Label>("music-title");
            _artist = _carousel.Q<Label>("music-artist");
            _equalizerBars = _carousel.Query(className: "equalizer__bar").ToList().ToArray();

            Button previousButton = _carousel.Q<Button>("music-previous-button");
            Button nextButton = _carousel.Q<Button>("music-next-button");
            previousButton.focusable = false;
            nextButton.focusable = false;
            previousButton.clicked += Previous;
            nextButton.clicked += Next;
            _previous.RegisterCallback<ClickEvent>(_ => Previous());
            _next.RegisterCallback<ClickEvent>(_ => Next());
            _carousel.RegisterCallback<WheelEvent>(e => {
                if (e.delta.y > 0) Next();
                else Previous();
            });

            _carousel.RemoveFromClassList("carousel--inactive");
            SyncWithCurrentMusic();
        }

        public void Next() => Select(_index + 1, 1);

        public void Previous() => Select(_index - 1, -1);

        /// <summary>Se place sur la musique choisie au lancement (MusicPlayer.Start), sans la relancer.</summary>
        public void SyncWithCurrentMusic() {
            int index = _tracks.IndexOf(LockerContent.CurrentGameplayMusic);
            _index = index >= 0 ? index : 0;
            Refresh();
        }

        void Select(int index, int direction) {
            if (_carousel == null) return;

            _index = (index % _tracks.Count + _tracks.Count) % _tracks.Count;
            LockerItem track = _tracks[_index];
            LockerContent.CurrentGameplayMusic = track;
            if (!PlayerInventory.GetEquipped(LockerCategory.Music).IsRandom) PlayerInventory.Equip(track);

            AudioClip clip = track.IsSceneDefault ? _player.DefaultMusic : LockerContent.GetMusicClip(track);
            if (clip != null) _player.Play(clip);

            // Petite glissade dans le sens du défilement, comme dans le menu.
            string shift = direction > 0 ? ShiftUpClass : ShiftDownClass;
            _carousel.AddToClassList(shift);
            _carousel.schedule.Execute(() => {
                Refresh();
                _carousel.RemoveFromClassList(shift);
            }).StartingIn(110);
        }

        void Refresh() {
            LockerItem track = _tracks[_index];
            SetCover(_current, track);
            SetCover(_previous, _tracks[(_index - 1 + _tracks.Count) % _tracks.Count]);
            SetCover(_next, _tracks[(_index + 1) % _tracks.Count]);
            _placeholder.text = LockerContent.GetCover(track) == null ? track.Name : "";
            _title.text = track.Name;
            _artist.text = string.IsNullOrEmpty(track.Artist) ? "Musique d'origine" : track.Artist;
        }

        static void SetCover(VisualElement element, LockerItem track) {
            Texture2D cover = LockerContent.GetCover(track);
            element.style.backgroundImage = cover != null ? new StyleBackground(Background.FromTexture2D(cover)) : new StyleBackground(StyleKeyword.None);
        }

        /// <summary>Égaliseur décoratif, comme dans le menu.</summary>
        public void Update() {
            if (_carousel == null) return;

            for (int i = 0; i < _equalizerBars.Length; i++) {
                float t = Time.unscaledTime * (5f + i * 1.7f) + i * 1.3f;
                float height = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(t) * Mathf.Cos(t * 0.37f));
                _equalizerBars[i].style.height = Length.Percent(height * 100f);
            }
        }
    }
}
