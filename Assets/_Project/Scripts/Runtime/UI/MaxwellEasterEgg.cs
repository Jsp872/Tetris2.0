using System.Globalization;
using Tetris.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Easter egg : tant que l'action Misc (J) est maintenue, Maxwell tourne au centre de l'écran sur le son "oiia".
    /// Combo : chaque nouvel appui accélère la rotation. Sans nouvel appui pendant
    /// <see cref="_comboResetDelay"/> secondes après le relâchement, la vitesse revient à 1.
    /// Une barre de combo affiche la vitesse et le temps restant avant la remise à zéro.
    /// Utilise les éléments "maxwell" et "combo" du HUD. Fonctionne aussi pendant la pause (temps non affecté par Time.timeScale).
    /// </summary>
    public class MaxwellEasterEgg : MonoBehaviour {
        const string HiddenClass = "maxwell--hidden";
        const string ComboHiddenClass = "combo--hidden";
        const string ComboPopClass = "combo__label--pop";

        [SerializeField] UIDocument _document;
        [SerializeField] Sprite[] _frames;
        [SerializeField] AudioClip _sound;
        [Tooltip("Tours complets du chat par seconde à vitesse x1 (le GIF fait un tour en 187 images).")]
        [SerializeField] float _rotationsPerSecond = 3f;
        [Tooltip("Limite d'avance par image affichée, en fraction de tour. Au-delà, l'effet stroboscopique " +
                 "ferait paraître la rotation plus lente.")]
        [SerializeField, Range(0.1f, 0.45f)] float _maxTurnPerFrame = 0.35f;
        [Tooltip("Vitesse de rotation du halo derrière le chat, en degrés par seconde.")]
        [SerializeField] float _haloSpeed = 180f;

        [Header("Combo")]
        [Tooltip("Vitesse de rotation ajoutée à chaque nouvel appui.")]
        [SerializeField] float _comboStep = 0.1f;
        [SerializeField] float _maxSpeed = 10f;
        [Tooltip("Temps sans appui, après le relâchement, avant que la vitesse revienne à 1.")]
        [SerializeField] float _comboResetDelay = 1f;
        [Tooltip("Hauteur du son ajoutée par point de vitesse (0 = le son ne change pas).")]
        [SerializeField] float _pitchPerSpeed = 0.04f;
        [Tooltip("Hauteur maximale du son (1 = normal).")]
        [SerializeField] float _maxPitch = 1.3f;

        AudioSource _audioSource;
        VisualElement _container;
        VisualElement _cat;
        VisualElement _halo;
        VisualElement _combo;
        Label _comboLabel;
        VisualElement _comboFill;
        VisualElement _comboTimerFill;
        bool _isActive;
        bool _isComboVisible;
        float _speed = 1f;
        float _releaseTime = float.NegativeInfinity;
        float _framePosition;
        float _haloAngle;
        float _hueTime;
        int _frameIndex = -1;

        void Awake() {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = _sound;
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
        }

        void OnEnable() {
            EventBus<MiscInputEvent>.Subscribe(OnMisc);
        }

        void OnDisable() {
            EventBus<MiscInputEvent>.Unsubscribe(OnMisc);
            SetActive(false);
        }

        void OnMisc(MiscInputEvent e) {
            if (!EnsureInitialized()) return;

            if (e.IsPressed) {
                AddCombo();
            }
            SetActive(e.IsPressed);
        }

        void AddCombo() {
            bool comboExpired = Time.unscaledTime - _releaseTime > _comboResetDelay;
            _speed = comboExpired ? 1f : Mathf.Min(_speed + _comboStep, _maxSpeed);
            _audioSource.pitch = Mathf.Min(1f + (_speed - 1f) * _pitchPerSpeed, _maxPitch);

            _comboLabel.text = $"SPEED x{_speed.ToString("0.0", CultureInfo.InvariantCulture)}";
            _comboLabel.AddToClassList(ComboPopClass);
            _comboLabel.schedule.Execute(() => _comboLabel.RemoveFromClassList(ComboPopClass)).StartingIn(80);
        }

        void SetActive(bool isActive) {
            if (isActive == _isActive || !EnsureInitialized()) return;

            _isActive = isActive;
            _container.EnableInClassList(HiddenClass, !isActive);

            if (isActive) {
                _audioSource.Play();
            }
            else {
                _releaseTime = Time.unscaledTime;
                _audioSource.Stop();
            }
        }

        void Update() {
            if (_container == null) return;

            UpdateComboBar();

            if (!_isActive || _frames.Length == 0) return;

            float deltaTime = Time.unscaledDeltaTime;
            float frameStep = deltaTime * _rotationsPerSecond * _speed * _frames.Length;
            frameStep = Mathf.Min(frameStep, _maxTurnPerFrame * _frames.Length);
            _framePosition = (_framePosition + frameStep) % _frames.Length;
            _haloAngle = (_haloAngle + deltaTime * _haloSpeed * _speed) % 360f;
            _hueTime += deltaTime * _speed;

            int frameIndex = (int)_framePosition;
            if (frameIndex != _frameIndex) {
                _frameIndex = frameIndex;
                _cat.style.backgroundImage = new StyleBackground(_frames[frameIndex]);
            }

            _halo.style.rotate = new Rotate(_haloAngle);
            _halo.style.unityBackgroundImageTintColor = Color.HSVToRGB(_hueTime * 0.3f % 1f, 0.55f, 1f);
        }

        /// <summary>
        /// Barre principale : vitesse actuelle (vert, jaune, rouge, puis arc-en-ciel au maximum).
        /// Petite barre : temps restant avant la remise à zéro, pleine tant que la touche est maintenue.
        /// </summary>
        void UpdateComboBar() {
            float timeLeft = _isActive ? 1f : 1f - (Time.unscaledTime - _releaseTime) / _comboResetDelay;

            if (!_isActive && timeLeft <= 0f) {
                _speed = 1f;
            }

            bool isVisible = _isActive || timeLeft > 0f;
            if (isVisible != _isComboVisible) {
                _isComboVisible = isVisible;
                _combo.EnableInClassList(ComboHiddenClass, !isVisible);
            }
            if (!isVisible) return;

            float fill = _maxSpeed > 1f ? (_speed - 1f) / (_maxSpeed - 1f) : 1f;
            Color color = fill >= 1f
                ? Color.HSVToRGB(Time.unscaledTime * 1.5f % 1f, 0.85f, 1f)
                : Color.HSVToRGB(Mathf.Lerp(0.33f, 0f, fill), 0.85f, 1f);

            _comboFill.style.width = Length.Percent(Mathf.Max(fill, 0.02f) * 100f);
            _comboFill.style.backgroundColor = color;
            _comboTimerFill.style.width = Length.Percent(Mathf.Clamp01(timeLeft) * 100f);
        }

        bool EnsureInitialized() {
            if (_container != null) return true;

            VisualElement root = _document != null ? _document.rootVisualElement : null;
            if (root == null) return false;

            _container = root.Q("maxwell");
            _cat = root.Q("maxwell-cat");
            _halo = root.Q("maxwell-halo");
            _combo = root.Q("combo");
            _comboLabel = root.Q<Label>("combo-label");
            _comboFill = root.Q("combo-fill");
            _comboTimerFill = root.Q("combo-timer-fill");
            return _container != null && _combo != null;
        }
    }
}
