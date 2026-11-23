using Tetris.Events;
using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Bruitages de la partie : un son par moment (<see cref="SoundSlot"/>), celui équipé dans le Casier.
    /// Apparition (<see cref="BlocSpawnedEvent"/>), déplacement, rotation, pose (<see cref="BlocLockedEvent"/>),
    /// ligne (1 à 3 lignes), Tetris (4 lignes) et game over.
    /// Les sons "ORIGINAL" de l'apparition et de la pose sont ceux réglés ici dans l'inspecteur.
    /// Les moments fréquents (apparition, déplacement, rotation, pose) ont chacun leur source : un nouveau son coupe
    /// le précédent au lieu de s'empiler. Lignes, Tetris et game over se superposent.
    /// La descente (<see cref="SoundSlot.SoftDrop"/>) tourne en boucle tant que la touche est maintenue.
    /// Volume réglé par <see cref="GameSettings.SfxVolume"/>.
    /// </summary>
    public class GameplaySfx : MonoBehaviour {
        [SerializeField] AudioClip _spawnSound;
        [SerializeField, Range(0f, 1f)] float _spawnVolume = 0.8f;
        [SerializeField] AudioClip _lockSound;
        [SerializeField, Range(0f, 1f)] float _lockVolume = 0.9f;
        [Tooltip("Variation aléatoire de la hauteur du son de pose, pour éviter un son trop répétitif (0.08 = +/- 8 %).")]
        [SerializeField, Range(0f, 0.3f)] float _lockPitchVariation = 0.08f;
        [Tooltip("Volume des sons du Casier (hors sons d'origine).")]
        [SerializeField, Range(0f, 1f)] float _lockerVolume = 0.9f;

        readonly AudioClip[] _clips = new AudioClip[System.Enum.GetValues(typeof(SoundSlot)).Length];
        readonly float[] _volumes = new float[System.Enum.GetValues(typeof(SoundSlot)).Length];
        readonly AudioSource[] _slotSources = new AudioSource[System.Enum.GetValues(typeof(SoundSlot)).Length];
        AudioSource _eventSource;
        AudioSource _softDropSource;

        void Awake() {
            _eventSource = CreateSource();
            _softDropSource = CreateSource();
            _softDropSource.loop = true;
            foreach (SoundSlot slot in new[] { SoundSlot.Spawn, SoundSlot.Move, SoundSlot.Rotate, SoundSlot.Lock }) {
                _slotSources[(int)slot] = CreateSource();
            }

            foreach (SoundSlot slot in System.Enum.GetValues(typeof(SoundSlot))) {
                LockerItem item = PlayerInventory.GetEquippedSound(slot);
                bool isOriginal = item != null && item.IsSceneDefault;
                _clips[(int)slot] = isOriginal
                    ? slot == SoundSlot.Spawn ? _spawnSound : _lockSound
                    : LockerContent.GetSoundClip(item);
                _volumes[(int)slot] = !isOriginal ? _lockerVolume : slot == SoundSlot.Spawn ? _spawnVolume : _lockVolume;
            }
        }

        void OnEnable() {
            EventBus<BlocSpawnedEvent>.Subscribe(OnBlocSpawned);
            EventBus<BlocLockedEvent>.Subscribe(OnBlocLocked);
            EventBus<BlocMovedEvent>.Subscribe(OnBlocMoved);
            EventBus<BlocRotatedEvent>.Subscribe(OnBlocRotated);
            EventBus<ScoreChangedEvent>.Subscribe(OnScoreChanged);
            EventBus<GameOverEvent>.Subscribe(OnGameOver);
            EventBus<DownInputEvent>.Subscribe(OnDown);
        }

        void OnDisable() {
            EventBus<BlocSpawnedEvent>.Unsubscribe(OnBlocSpawned);
            EventBus<BlocLockedEvent>.Unsubscribe(OnBlocLocked);
            EventBus<BlocMovedEvent>.Unsubscribe(OnBlocMoved);
            EventBus<BlocRotatedEvent>.Unsubscribe(OnBlocRotated);
            EventBus<ScoreChangedEvent>.Unsubscribe(OnScoreChanged);
            EventBus<GameOverEvent>.Unsubscribe(OnGameOver);
            EventBus<DownInputEvent>.Unsubscribe(OnDown);
        }

        void OnBlocSpawned(BlocSpawnedEvent e) => Play(SoundSlot.Spawn, 0f);

        void OnBlocLocked(BlocLockedEvent e) => Play(SoundSlot.Lock, _lockPitchVariation);

        void OnBlocMoved(BlocMovedEvent e) => Play(SoundSlot.Move, 0f);

        void OnBlocRotated(BlocRotatedEvent e) => Play(SoundSlot.Rotate, 0f);

        void OnScoreChanged(ScoreChangedEvent e) {
            if (e.LinesCleared <= 0) return;

            bool isTetris = e.LinesCleared >= 4 && _clips[(int)SoundSlot.Tetris] != null;
            Play(isTetris ? SoundSlot.Tetris : SoundSlot.Line, 0f);
        }

        void OnGameOver(GameOverEvent e) {
            _softDropSource.Stop();
            Play(SoundSlot.GameOver, 0f);
        }

        /// <summary>Boucle de descente : démarre à l'appui, s'arrête au relâchement.</summary>
        void OnDown(DownInputEvent e) {
            AudioClip clip = _clips[(int)SoundSlot.SoftDrop];
            if (!e.IsPressed || clip == null) {
                _softDropSource.Stop();
                return;
            }

            _softDropSource.clip = clip;
            _softDropSource.pitch = SpoofGoofSettings.Pitch;
            _softDropSource.volume = _volumes[(int)SoundSlot.SoftDrop] * GameSettings.SfxVolume;
            _softDropSource.Play();
        }

        void Play(SoundSlot slot, float pitchVariation) {
            AudioClip clip = _clips[(int)slot];
            if (clip == null) return;

            float pitch = (1f + Random.Range(-pitchVariation, pitchVariation)) * SpoofGoofSettings.Pitch;
            float volume = _volumes[(int)slot] * GameSettings.SfxVolume;
            AudioSource slotSource = _slotSources[(int)slot];

            if (slotSource != null) {
                slotSource.Stop();
                slotSource.clip = clip;
                slotSource.pitch = pitch;
                slotSource.volume = volume;
                slotSource.Play();
            }
            else {
                _eventSource.pitch = pitch;
                _eventSource.PlayOneShot(clip, volume);
            }
        }

        AudioSource CreateSource() {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }
    }
}
