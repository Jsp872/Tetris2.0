using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Tetris {
    public enum UiSound { Hover, Click, Back, Tick, Accept, Equip, Buy, Wrong }

    /// <summary>
    /// Sons de l'interface, sur toutes les UIDocument de toutes les scènes (rien à placer dans les scènes) :
    /// survol ou sélection d'un bouton, clic, retour (Échap), réglage gauche / droite d'une ligne d'options.
    /// Les autres sons (<see cref="UiSound.Accept"/>, <see cref="UiSound.Buy"/>...) se jouent avec <see cref="Play"/>.
    /// Les petits sons sont générés en code ; la caisse enregistreuse et le buzzer viennent de Resources/Locker/Ui.
    /// </summary>
    public class UiSounds : MonoBehaviour {
        const int SampleRate = 44100;

        static UiSounds _instance;

        readonly HashSet<VisualElement> _roots = new();
        readonly Dictionary<UiSound, AudioClip> _clips = new();
        AudioSource _source;
        VisualElement _hovered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() {
            _instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (_instance == null) {
                var go = new GameObject("UI Sounds");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<UiSounds>();
            }
            _instance.HookDocuments();
        }

        public static void Play(UiSound sound, float volume = 1f) {
            if (_instance != null) _instance.PlayClip(sound, volume);
        }

        void Awake() {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            _clips[UiSound.Hover] = Tone("Hover", 0.035f, 1500f, 1900f, 0.18f, 0f);
            _clips[UiSound.Click] = Tone("Click", 0.08f, 620f, 260f, 0.35f, 0.3f);
            _clips[UiSound.Back] = Tone("Back", 0.11f, 420f, 180f, 0.32f, 0.3f);
            _clips[UiSound.Tick] = Tone("Tick", 0.025f, 1100f, 1100f, 0.2f, 0f);
            _clips[UiSound.Accept] = Arpeggio("Accept", 0.07f, 0.3f, 880f, 1320f);
            _clips[UiSound.Equip] = Arpeggio("Equip", 0.06f, 0.3f, 660f, 880f, 1175f);
            _clips[UiSound.Buy] = LockerContent.GetUiClip("buy");
            _clips[UiSound.Wrong] = LockerContent.GetUiClip("wrong");
        }

        void HookDocuments() {
            _roots.RemoveWhere(root => root.panel == null);
            foreach (UIDocument document in FindObjectsByType<UIDocument>()) {
                VisualElement root = document.rootVisualElement;
                if (root == null || !_roots.Add(root)) continue;

                root.RegisterCallback<PointerOverEvent>(OnPointerOver);
                root.RegisterCallback<ClickEvent>(OnClick);
                root.RegisterCallback<FocusInEvent>(OnFocusIn);
                root.RegisterCallback<NavigationCancelEvent>(_ => PlayClip(UiSound.Back, 1f));
                root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            }
        }

        void OnPointerOver(PointerOverEvent e) {
            VisualElement interactive = FindInteractive(e.target as VisualElement);
            if (interactive == _hovered) return;

            _hovered = interactive;
            if (interactive != null && interactive.enabledInHierarchy) PlayClip(UiSound.Hover, 1f);
        }

        void OnFocusIn(FocusInEvent e) {
            VisualElement interactive = FindInteractive(e.target as VisualElement);
            if (interactive == null || interactive == _hovered) return;

            _hovered = interactive;
            PlayClip(UiSound.Hover, 1f);
        }

        void OnClick(ClickEvent e) {
            if (FindInteractive(e.target as VisualElement) != null) PlayClip(UiSound.Click, 1f);
        }

        void OnNavigationMove(NavigationMoveEvent e) {
            bool isHorizontal = e.direction == NavigationMoveEvent.Direction.Left || e.direction == NavigationMoveEvent.Direction.Right;
            var target = e.target as VisualElement;
            if (isHorizontal && target != null && target.ClassListContains("options-row")) PlayClip(UiSound.Tick, 1f);
        }

        /// <summary>Le bouton (ou la ligne d'options) sous l'élément, ou null.</summary>
        static VisualElement FindInteractive(VisualElement element) {
            for (VisualElement current = element; current != null; current = current.parent) {
                if (current is Button || current.ClassListContains("options-row")) return current;
            }
            return null;
        }

        void PlayClip(UiSound sound, float volume) {
            if (_clips.TryGetValue(sound, out AudioClip clip) && clip != null) {
                _source.pitch = SpoofGoofSettings.Pitch;
                _source.PlayOneShot(clip, volume * GameSettings.SfxVolume);
            }
        }

        // ---------- Sons générés ----------

        /// <summary>Note qui glisse de <paramref name="from"/> à <paramref name="to"/> Hz, avec une harmonique pour un son plus "plastique".</summary>
        static AudioClip Tone(string name, float duration, float from, float to, float volume, float harmonic) {
            int count = Mathf.CeilToInt(duration * SampleRate);
            var samples = new float[count];
            double phase = 0;

            for (int i = 0; i < count; i++) {
                float t = (float)i / count;
                phase += Mathf.Lerp(from, to, t) / SampleRate;
                float wave = Mathf.Sin((float)(phase * 2.0 * Mathf.PI)) + harmonic * Mathf.Sin((float)(phase * 4.0 * Mathf.PI));
                float envelope = Mathf.Min(t * 40f, 1f) * (1f - t) * (1f - t);
                samples[i] = wave * envelope * volume;
            }
            return CreateClip(name, samples);
        }

        static AudioClip Arpeggio(string name, float noteDuration, float volume, params float[] notes) {
            int noteCount = Mathf.CeilToInt(noteDuration * SampleRate);
            var samples = new float[noteCount * notes.Length + noteCount * 2];

            for (int n = 0; n < notes.Length; n++) {
                int length = noteCount * (n == notes.Length - 1 ? 3 : 1);
                for (int i = 0; i < length && n * noteCount + i < samples.Length; i++) {
                    float t = (float)i / length;
                    float wave = Mathf.Sin(2f * Mathf.PI * notes[n] * i / SampleRate);
                    samples[n * noteCount + i] += wave * Mathf.Min(t * 30f, 1f) * (1f - t) * volume;
                }
            }
            return CreateClip(name, samples);
        }

        static AudioClip CreateClip(string name, float[] samples) {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
