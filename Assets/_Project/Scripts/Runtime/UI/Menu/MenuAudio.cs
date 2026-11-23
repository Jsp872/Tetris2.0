using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Sons du Casier et de la boutique : musique de la boutique (Wii Shop Channel) et aperçus des musiques et des packs.
    /// Baisse la musique du menu pendant ce temps (<see cref="MusicPlayer.SetDucked"/>).
    /// </summary>
    public class MenuAudio : MonoBehaviour {
        const string ShopMusic = "Locker/Music/wiishop";
        const float MusicPreviewDuration = 9f;
        const float ShopMusicVolume = 0.5f;

        AudioSource _preview;
        AudioSource _shopMusic;
        MusicPlayer _menuMusic;
        float _previewEnd;
        bool _isMusicPreview;
        bool _isShopOpen;

        void Awake() {
            _preview = gameObject.AddComponent<AudioSource>();
            _preview.playOnAwake = false;
            _shopMusic = gameObject.AddComponent<AudioSource>();
            _shopMusic.playOnAwake = false;
            _shopMusic.loop = true;
        }

        void Start() {
            _menuMusic = FindAnyObjectByType<MusicPlayer>();
        }

        public void SetShopOpen(bool isOpen) {
            _isShopOpen = isOpen;
            if (isOpen && !_shopMusic.isPlaying) {
                _shopMusic.clip = Resources.Load<AudioClip>(ShopMusic);
                _shopMusic.volume = 0f;
                if (_shopMusic.clip != null) _shopMusic.Play();
            }
        }

        /// <summary>Joue un extrait de la musique (à partir du quart du morceau).</summary>
        public void PreviewMusic(AudioClip clip) {
            if (clip == null) return;

            StartPreview(clip, true);
            _preview.time = clip.length * 0.25f;
            _previewEnd = Time.unscaledTime + MusicPreviewDuration;
        }

        public void PreviewSound(AudioClip clip) {
            if (clip == null) return;

            StartPreview(clip, false);
            _previewEnd = Time.unscaledTime + clip.length / SpoofGoofSettings.Pitch;
        }

        public void StopPreview() {
            _preview.Stop();
        }

        void StartPreview(AudioClip clip, bool isMusic) {
            _isMusicPreview = isMusic;
            _preview.Stop();
            _preview.clip = clip;
            _preview.pitch = SpoofGoofSettings.Pitch;
            _preview.volume = isMusic ? GameSettings.MusicVolume * 0.8f : GameSettings.SfxVolume;
            _preview.Play();
        }

        void Update() {
            if (_preview.isPlaying && Time.unscaledTime >= _previewEnd) {
                _preview.Stop();
            }

            bool isPreviewing = _preview.isPlaying;
            if (_menuMusic != null) _menuMusic.SetDucked(_isShopOpen || isPreviewing);

            // La musique de la boutique s'efface pendant un aperçu de musique.
            float target = _isShopOpen ? ShopMusicVolume * GameSettings.MusicVolume * (isPreviewing && _isMusicPreview ? 0f : 1f) : 0f;
            _shopMusic.volume = Mathf.MoveTowards(_shopMusic.volume, target, Time.unscaledDeltaTime * 1.5f);
            _shopMusic.pitch = SpoofGoofSettings.Pitch;
            if (!_isShopOpen && _shopMusic.isPlaying && _shopMusic.volume <= 0f) _shopMusic.Stop();
        }
    }
}
