using Tetris.Events;
using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Bruitages de la partie. Sons d'origine : un son à l'apparition d'une pièce (<see cref="BlocSpawnedEvent"/>)
    /// et un "pop" quand elle se pose (<see cref="BlocLockedEvent"/>).
    /// Avec un pack de sons équipé dans le Casier, chaque événement prend le son du pack s'il en a un :
    /// spawn, move, rotate, lock, line (1 à 3 lignes), tetris (4 lignes), gameover.
    /// Volume réglé par <see cref="GameSettings.SfxVolume"/>.
    /// </summary>
    public class GameplaySfx : MonoBehaviour {
        [SerializeField] AudioClip _spawnSound;
        [SerializeField, Range(0f, 1f)] float _spawnVolume = 0.8f;
        [SerializeField] AudioClip _lockSound;
        [SerializeField, Range(0f, 1f)] float _lockVolume = 0.9f;
        [Tooltip("Variation aléatoire de la hauteur du pop, pour éviter un son trop répétitif (0.08 = +/- 8 %).")]
        [SerializeField, Range(0f, 0.3f)] float _lockPitchVariation = 0.08f;
        [Tooltip("Volume des sons des packs du Casier.")]
        [SerializeField, Range(0f, 1f)] float _packVolume = 0.9f;

        AudioSource _spawnSource;
        AudioSource _lockSource;
        AudioSource _eventSource;
        SoundPack _pack;

        void Awake() {
            _spawnSource = CreateSource();
            _lockSource = CreateSource();
            _eventSource = CreateSource();
            _pack = LockerContent.GetSoundPack(PlayerInventory.GetEquipped(LockerCategory.Sounds));
        }

        void OnEnable() {
            EventBus<BlocSpawnedEvent>.Subscribe(OnBlocSpawned);
            EventBus<BlocLockedEvent>.Subscribe(OnBlocLocked);
            EventBus<BlocMovedEvent>.Subscribe(OnBlocMoved);
            EventBus<BlocRotatedEvent>.Subscribe(OnBlocRotated);
            EventBus<ScoreChangedEvent>.Subscribe(OnScoreChanged);
            EventBus<GameOverEvent>.Subscribe(OnGameOver);
        }

        void OnDisable() {
            EventBus<BlocSpawnedEvent>.Unsubscribe(OnBlocSpawned);
            EventBus<BlocLockedEvent>.Unsubscribe(OnBlocLocked);
            EventBus<BlocMovedEvent>.Unsubscribe(OnBlocMoved);
            EventBus<BlocRotatedEvent>.Unsubscribe(OnBlocRotated);
            EventBus<ScoreChangedEvent>.Unsubscribe(OnScoreChanged);
            EventBus<GameOverEvent>.Unsubscribe(OnGameOver);
        }

        void OnBlocSpawned(BlocSpawnedEvent e) {
            Play(_spawnSource, PackOr("spawn", _spawnSound), _spawnVolume, 0f);
        }

        void OnBlocLocked(BlocLockedEvent e) {
            Play(_lockSource, PackOr("lock", _lockSound), _lockVolume, _lockPitchVariation);
        }

        void OnBlocMoved(BlocMovedEvent e) {
            Play(_eventSource, _pack?.Get("move"), _packVolume, 0f);
        }

        void OnBlocRotated(BlocRotatedEvent e) {
            Play(_eventSource, _pack?.Get("rotate"), _packVolume, 0f);
        }

        void OnScoreChanged(ScoreChangedEvent e) {
            if (e.LinesCleared <= 0) return;

            AudioClip clip = e.LinesCleared >= 4 ? _pack?.Get("tetris") ?? _pack?.Get("line") : _pack?.Get("line");
            Play(_eventSource, clip, _packVolume, 0f);
        }

        void OnGameOver(GameOverEvent e) {
            Play(_eventSource, _pack?.Get("gameover"), _packVolume, 0f);
        }

        /// <summary>Son du pack pour l'événement, sinon le son d'origine (un pack sans ce son garde l'original).</summary>
        AudioClip PackOr(string soundEvent, AudioClip original) {
            AudioClip clip = _pack?.Get(soundEvent);
            return clip != null ? clip : original;
        }

        static void Play(AudioSource source, AudioClip clip, float volume, float pitchVariation) {
            if (clip == null) return;

            source.pitch = (1f + Random.Range(-pitchVariation, pitchVariation)) * SpoofGoofSettings.Pitch;
            source.PlayOneShot(clip, volume * GameSettings.SfxVolume);
        }

        AudioSource CreateSource() {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }
    }
}
