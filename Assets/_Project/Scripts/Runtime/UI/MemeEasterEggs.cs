using Tetris.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Mèmes déclenchés par la partie (éléments "plankton", "chips-cat" et "scuba-cat" du HUD) :
    /// - Cursed Plankton et son gémissement toutes les <see cref="_planktonEveryLines"/> lignes ;
    /// - le chat aux Ruffles en image rémanente toutes les <see cref="_chipsCatEveryLines"/> lignes ;
    /// - Scuba Cat qui danse et rebondit sur les bords de l'écran comme le logo DVD quand la partie est perdue,
    ///   sur le son "scubbaaa" suivi d'une musique de fond en boucle.
    /// Volume des sons réglé par <see cref="GameSettings.MemeVolume"/>.
    /// </summary>
    public class MemeEasterEggs : MonoBehaviour {
        const string PlanktonHiddenClass = "meme-plankton--hidden";
        const string ChipsCatHiddenClass = "meme-chips--hidden";
        const string ScubaHiddenClass = "meme-scuba--hidden";

        [SerializeField] UIDocument _document;

        [Header("Plankton")]
        [SerializeField] AudioClip _planktonSound;
        [SerializeField, Min(1)] int _planktonEveryLines = 7;
        [Tooltip("Amplitude du tremblement de Plankton pendant le son, en pixels.")]
        [SerializeField] float _planktonShake = 6f;

        [Header("Chat aux Ruffles")]
        [SerializeField] Sprite[] _chipsCatFrames;
        [SerializeField] float _chipsCatFramesPerSecond = 10f;
        [SerializeField, Min(1)] int _chipsCatEveryLines = 10;
        [Tooltip("Durée d'affichage de l'image rémanente, en secondes.")]
        [SerializeField] float _chipsCatDuration = 4f;

        [Header("Scuba Cat")]
        [Tooltip("Joué une fois au game over.")]
        [SerializeField] AudioClip _scubaSound;
        [Tooltip("Enchaîne à la fin de Scuba Sound et tourne en boucle. Vide : Scuba Sound tourne en boucle.")]
        [SerializeField] AudioClip _scubaMusic;
        [SerializeField, Range(0f, 1f)] float _scubaMusicVolume = 0.8f;
        [SerializeField] Sprite[] _scubaFrames;
        [SerializeField] float _scubaFramesPerSecond = 20f;
        [Tooltip("Vitesse de déplacement, en pixels de référence (1920x1080) par seconde.")]
        [SerializeField] float _scubaSpeed = 900f;
        [Tooltip("Vitesse du rebond continu (oscillations par seconde).")]
        [SerializeField] float _scubaBounceFrequency = 3f;

        AudioSource _audioSource;
        AudioSource _musicSource;
        VisualElement _root;
        VisualElement _plankton;
        VisualElement _chipsCat;
        VisualElement _scubaCat;
        float _planktonEndTime = float.NegativeInfinity;
        float _chipsCatEndTime = float.NegativeInfinity;
        float _chipsCatStartTime;
        int _chipsCatFrame = -1;
        bool _isScubaActive;
        float _scubaStartTime;
        int _scubaFrame = -1;
        Vector2 _scubaPosition;
        Vector2 _scubaVelocity;

        void Awake() {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
        }

        void OnEnable() {
            EventBus<ScoreChangedEvent>.Subscribe(OnScoreChanged);
            EventBus<GameOverEvent>.Subscribe(OnGameOver);
        }

        void OnDisable() {
            EventBus<ScoreChangedEvent>.Unsubscribe(OnScoreChanged);
            EventBus<GameOverEvent>.Unsubscribe(OnGameOver);
        }

        void OnScoreChanged(ScoreChangedEvent e) {
            if (!EnsureInitialized()) return;

            int previousLines = e.Lines - e.LinesCleared;
            if (e.Lines / _planktonEveryLines > previousLines / _planktonEveryLines) {
                ShowPlankton();
            }
            if (e.Lines / _chipsCatEveryLines > previousLines / _chipsCatEveryLines) {
                ShowChipsCat();
            }
        }

        void ShowPlankton() {
            float duration = _planktonSound != null ? _planktonSound.length / SpoofGoofSettings.Pitch : 3f;
            _planktonEndTime = Time.unscaledTime + duration;
            _plankton.RemoveFromClassList(PlanktonHiddenClass);

            if (_planktonSound != null) {
                _audioSource.pitch = SpoofGoofSettings.Pitch;
                _audioSource.PlayOneShot(_planktonSound, GameSettings.MemeVolume);
            }
        }

        void ShowChipsCat() {
            _chipsCatStartTime = Time.unscaledTime;
            _chipsCatEndTime = Time.unscaledTime + _chipsCatDuration;
            _chipsCat.RemoveFromClassList(ChipsCatHiddenClass);
        }

        void OnGameOver(GameOverEvent e) {
            if (!EnsureInitialized()) return;

            _isScubaActive = true;
            _scubaStartTime = Time.unscaledTime;
            _scubaPosition = new Vector2(100f, 100f);
            _scubaVelocity = new Vector2(1f, 0.75f).normalized * _scubaSpeed;
            _scubaCat.RemoveFromClassList(ScubaHiddenClass);
            PlayScubaAudio();
        }

        void PlayScubaAudio() {
            float pitch = SpoofGoofSettings.Pitch;
            double startTime = AudioSettings.dspTime;
            if (_scubaSound != null) {
                _audioSource.pitch = pitch;
                _audioSource.PlayOneShot(_scubaSound, GameSettings.MemeVolume);
                startTime += _scubaSound.length / pitch;
            }

            _musicSource.pitch = pitch;
            _musicSource.clip = _scubaMusic != null ? _scubaMusic : _scubaSound;
            _musicSource.volume = (_scubaMusic != null ? _scubaMusicVolume : 1f) * GameSettings.MemeVolume;
            if (_musicSource.clip != null) {
                _musicSource.PlayScheduled(startTime);
            }
        }

        void Update() {
            if (_root == null) return;

            UpdatePlankton();
            UpdateChipsCat();
            if (_isScubaActive) {
                UpdateScubaCat(Time.unscaledDeltaTime);
            }
        }

        void UpdatePlankton() {
            if (_plankton.ClassListContains(PlanktonHiddenClass)) return;

            if (Time.unscaledTime >= _planktonEndTime) {
                _plankton.AddToClassList(PlanktonHiddenClass);
                _plankton.style.translate = new Translate(0, 0);
                return;
            }

            Vector2 shake = Random.insideUnitCircle * _planktonShake;
            _plankton.style.translate = new Translate(shake.x, shake.y);
        }

        void UpdateChipsCat() {
            if (_chipsCat.ClassListContains(ChipsCatHiddenClass)) return;

            if (Time.unscaledTime >= _chipsCatEndTime) {
                _chipsCat.AddToClassList(ChipsCatHiddenClass);
                return;
            }

            Animate(_chipsCat, _chipsCatFrames, Time.unscaledTime - _chipsCatStartTime, _chipsCatFramesPerSecond, ref _chipsCatFrame);
        }

        /// <summary>Déplacement façon logo DVD : rebond sur les bords et nouvelle teinte à chaque rebond.</summary>
        void UpdateScubaCat(float deltaTime) {
            Animate(_scubaCat, _scubaFrames, Time.unscaledTime - _scubaStartTime, _scubaFramesPerSecond, ref _scubaFrame);

            Rect area = _root.layout;
            Rect size = _scubaCat.layout;
            if (float.IsNaN(area.width) || float.IsNaN(size.width)) return;

            _scubaPosition += _scubaVelocity * deltaTime;
            float maxX = area.width - size.width;
            float maxY = area.height - size.height;
            bool bounced = false;

            if (_scubaPosition.x < 0f || _scubaPosition.x > maxX) {
                _scubaVelocity.x = -_scubaVelocity.x;
                _scubaPosition.x = Mathf.Clamp(_scubaPosition.x, 0f, maxX);
                bounced = true;
            }
            if (_scubaPosition.y < 0f || _scubaPosition.y > maxY) {
                _scubaVelocity.y = -_scubaVelocity.y;
                _scubaPosition.y = Mathf.Clamp(_scubaPosition.y, 0f, maxY);
                bounced = true;
            }
            if (bounced) {
                _scubaCat.style.unityBackgroundImageTintColor = Color.HSVToRGB(Random.value, 0.45f, 1f);
            }

            _scubaCat.style.left = _scubaPosition.x;
            _scubaCat.style.top = _scubaPosition.y;

            // Rebond continu : écrasement / étirement pendant qu'il se balade.
            float bounce = Mathf.Sin(Time.unscaledTime * _scubaBounceFrequency * Mathf.PI * 2f);
            _scubaCat.style.scale = new Scale(new Vector2(1f + bounce * 0.12f, 1f - bounce * 0.12f));
        }

        static void Animate(VisualElement element, Sprite[] frames, float time, float framesPerSecond, ref int currentFrame) {
            if (frames == null || frames.Length == 0) return;

            int frame = (int)(time * framesPerSecond) % frames.Length;
            if (frame == currentFrame) return;

            currentFrame = frame;
            element.style.backgroundImage = new StyleBackground(frames[frame]);
        }

        bool EnsureInitialized() {
            if (_root != null) return true;

            VisualElement root = _document != null ? _document.rootVisualElement : null;
            if (root == null) return false;

            _plankton = root.Q("plankton");
            _chipsCat = root.Q("chips-cat");
            _scubaCat = root.Q("scuba-cat");
            if (_plankton == null || _chipsCat == null || _scubaCat == null) return false;

            _root = root;
            return true;
        }
    }
}
