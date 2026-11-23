using Tetris.Events;
using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Bruitages de la partie : un son à l'apparition d'une pièce (<see cref="BlocSpawnedEvent"/>)
    /// et un "pop" quand elle se pose (<see cref="BlocLockedEvent"/>). Volume réglé par <see cref="GameSettings.SfxVolume"/>.
    /// </summary>
    public class GameplaySfx : MonoBehaviour {
        [SerializeField] AudioClip _spawnSound;
        [SerializeField, Range(0f, 1f)] float _spawnVolume = 0.8f;
        [SerializeField] AudioClip _lockSound;
        [SerializeField, Range(0f, 1f)] float _lockVolume = 0.9f;
        [Tooltip("Variation aléatoire de la hauteur du pop, pour éviter un son trop répétitif (0.08 = +/- 8 %).")]
        [SerializeField, Range(0f, 0.3f)] float _lockPitchVariation = 0.08f;

        AudioSource _spawnSource;
        AudioSource _lockSource;

        void Awake() {
            _spawnSource = CreateSource();
            _lockSource = CreateSource();
        }

        void OnEnable() {
            EventBus<BlocSpawnedEvent>.Subscribe(OnBlocSpawned);
            EventBus<BlocLockedEvent>.Subscribe(OnBlocLocked);
        }

        void OnDisable() {
            EventBus<BlocSpawnedEvent>.Unsubscribe(OnBlocSpawned);
            EventBus<BlocLockedEvent>.Unsubscribe(OnBlocLocked);
        }

        void OnBlocSpawned(BlocSpawnedEvent e) {
            if (_spawnSound != null) {
                _spawnSource.pitch = SpoofGoofSettings.Pitch;
                _spawnSource.PlayOneShot(_spawnSound, _spawnVolume * GameSettings.SfxVolume);
            }
        }

        void OnBlocLocked(BlocLockedEvent e) {
            if (_lockSound == null) return;

            _lockSource.pitch = (1f + Random.Range(-_lockPitchVariation, _lockPitchVariation)) * SpoofGoofSettings.Pitch;
            _lockSource.PlayOneShot(_lockSound, _lockVolume * GameSettings.SfxVolume);
        }

        AudioSource CreateSource() {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }
    }
}
