using Tetris.Events;
using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Musique de fond en boucle avec fondus. <see cref="Play"/> change de musique (fondu sortant puis entrant).
    /// Peut s'arrêter en fondu au game over (pour laisser la place à la musique du Scuba Cat).
    /// Le volume suit <see cref="GameSettings.MusicVolume"/> et la hauteur suit <see cref="SpoofGoofSettings.Pitch"/>,
    /// en direct (réglables pendant que la musique joue).
    /// En jeu (scène avec un GameManager), la musique de départ est celle équipée dans le Casier.
    /// <see cref="SetDucked"/> baisse la musique pendant un aperçu ou dans la boutique.
    /// </summary>
    public class MusicPlayer : MonoBehaviour {
        [Tooltip("Musique lancée au démarrage. Vide : rien tant que Play n'est pas appelé.")]
        [SerializeField] AudioClip _music;
        [SerializeField, Range(0f, 1f)] float _volume = 0.6f;
        [SerializeField] float _fadeInDuration = 1.5f;
        [SerializeField] float _fadeOutDuration = 0.8f;
        [Tooltip("Durée du fondu sortant quand on change de musique avec Play.")]
        [SerializeField] float _switchFadeDuration = 0.35f;
        [Tooltip("Coupe la musique en fondu quand GameOverEvent est publié.")]
        [SerializeField] bool _stopOnGameOver = true;

        AudioSource _source;
        AudioClip _pendingClip;
        float _fade;
        float _targetFade;
        float _fadeSpeed;
        float _duck = 1f;
        float _duckTarget = 1f;

        /// <summary>Musique de départ réglée dans l'inspecteur.</summary>
        public AudioClip DefaultMusic => _music;

        /// <summary>Clip en cours (ou celui qui va démarrer après le fondu).</summary>
        public AudioClip CurrentClip => _pendingClip != null ? _pendingClip : _source.clip;

        void Awake() {
            _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.volume = 0f;
        }

        void OnEnable() {
            EventBus<GameOverEvent>.Subscribe(OnGameOver);
        }

        void OnDisable() {
            EventBus<GameOverEvent>.Unsubscribe(OnGameOver);
        }

        void Start() {
            if (_music != null && _pendingClip == null && !_source.isPlaying) {
                bool isGameplay = FindAnyObjectByType<GameManager>() != null;
                Play(isGameplay ? LockerContent.ResolveGameplayMusic(_music) : _music);
            }
        }

        /// <summary>Joue <paramref name="clip"/> en boucle. Si une musique joue déjà, elle s'arrête d'abord en fondu.</summary>
        public void Play(AudioClip clip) {
            if (clip == null) return;

            _pendingClip = clip;
            if (_source.isPlaying && _fade > 0f) {
                FadeTo(0f, _switchFadeDuration);
            }
            else {
                StartPendingClip();
            }
        }

        void StartPendingClip() {
            _source.clip = _pendingClip;
            _pendingClip = null;
            _source.Play();
            FadeTo(1f, _fadeInDuration);
        }

        void OnGameOver(GameOverEvent e) {
            if (_stopOnGameOver) {
                _pendingClip = null;
                FadeTo(0f, _fadeOutDuration);
            }
        }

        /// <summary>Baisse la musique presque à zéro (aperçu, boutique), ou la remet.</summary>
        public void SetDucked(bool isDucked) {
            _duckTarget = isDucked ? 0f : 1f;
        }

        /// <summary>Fondu vers <paramref name="fade"/> (0 : silence, 1 : volume normal).</summary>
        void FadeTo(float fade, float duration) {
            _targetFade = fade;
            _fadeSpeed = Mathf.Abs(fade - _fade) / Mathf.Max(duration, 0.01f);
        }

        void Update() {
            _fade = Mathf.MoveTowards(_fade, _targetFade, _fadeSpeed * Time.unscaledDeltaTime);
            _duck = Mathf.MoveTowards(_duck, _duckTarget, Time.unscaledDeltaTime * 2.5f);
            _source.volume = _fade * _duck * _volume * GameSettings.MusicVolume;
            _source.pitch = SpoofGoofSettings.Pitch;

            if (_targetFade > 0f || _fade > 0f) return;

            if (_pendingClip != null) {
                StartPendingClip();
            }
            else if (_source.isPlaying) {
                _source.Stop();
            }
        }
    }
}
