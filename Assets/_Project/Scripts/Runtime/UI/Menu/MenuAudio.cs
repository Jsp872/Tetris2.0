using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Sons du Casier et de la boutique : musique de la boutique (Wii Shop Channel), aperçus des musiques et des sons,
    /// et sons de la simulation de partie du Casier (effets, boucle de descente, musique).
    /// Baisse la musique du menu pendant ce temps (<see cref="MusicPlayer.SetDucked"/>).
    /// </summary>
    public class MenuAudio : MonoBehaviour {
        const string ShopMusic = "Locker/Music/wiishop";
        const float MusicPreviewDuration = 9f;
        const float ShopMusicVolume = 0.5f;
        /// <summary>Part du volume de la musique de la boutique gardée pendant l'écoute d'un son.</summary>
        const float SoundPreviewShopVolume = 0.08f;

        AudioSource _preview;
        AudioSource _shopMusic;
        AudioSource _effects;
        AudioSource _loop;
        AudioSource _simulationMusic;
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
            _effects = gameObject.AddComponent<AudioSource>();
            _effects.playOnAwake = false;
            _loop = gameObject.AddComponent<AudioSource>();
            _loop.playOnAwake = false;
            _loop.loop = true;
            _simulationMusic = gameObject.AddComponent<AudioSource>();
            _simulationMusic.playOnAwake = false;
            _simulationMusic.loop = true;
        }

        /// <summary>Son ponctuel de la simulation (se superpose aux autres).</summary>
        public void PlayEffect(AudioClip clip) {
            if (clip == null) return;
            _effects.pitch = SpoofGoofSettings.Pitch;
            _effects.PlayOneShot(clip, GameSettings.SfxVolume);
        }

        /// <summary>Boucle de descente de la simulation. Null : arrêt.</summary>
        public void SetLoop(AudioClip clip) {
            if (clip == null) {
                _loop.Stop();
                return;
            }
            if (_loop.isPlaying && _loop.clip == clip) return;

            _loop.clip = clip;
            _loop.pitch = SpoofGoofSettings.Pitch;
            _loop.volume = GameSettings.SfxVolume;
            _loop.Play();
        }

        /// <summary>Musique de fond de la simulation (en boucle). Null : arrêt.</summary>
        public void SetSimulationMusic(AudioClip clip) {
            if (clip == null) {
                _simulationMusic.Stop();
                return;
            }
            _simulationMusic.clip = clip;
            _simulationMusic.pitch = SpoofGoofSettings.Pitch;
            _simulationMusic.volume = GameSettings.MusicVolume * 0.6f;
            _simulationMusic.Play();
        }

        /// <summary>Coupe tout ce que joue la simulation.</summary>
        public void StopSimulation() {
            _loop.Stop();
            _simulationMusic.Stop();
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

            bool isPreviewing = _preview.isPlaying || _simulationMusic.isPlaying;
            if (_menuMusic != null) _menuMusic.SetDucked(_isShopOpen || isPreviewing);

            // Pendant un aperçu, la musique de la boutique se coupe (musique) ou passe très bas (son).
            bool isMusic = _simulationMusic.isPlaying || _isMusicPreview;
            float previewFactor = !isPreviewing ? 1f : isMusic ? 0f : SoundPreviewShopVolume;
            float target = _isShopOpen ? ShopMusicVolume * GameSettings.MusicVolume * previewFactor : 0f;
            float speed = isPreviewing ? 6f : 1.5f;
            _shopMusic.volume = Mathf.MoveTowards(_shopMusic.volume, target, Time.unscaledDeltaTime * speed);
            _shopMusic.pitch = SpoofGoofSettings.Pitch;
            if (!_isShopOpen && _shopMusic.isPlaying && _shopMusic.volume <= 0f) _shopMusic.Stop();
        }
    }
}
