using System.Collections.Generic;
using Tetris.Events;
using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Crée les pièces et garde une file des prochaines à venir.
    /// Lire <see cref="NextBlocs"/> ou écouter <see cref="NextBlocsChangedEvent"/> pour afficher la preview.
    /// </summary>
    public class Spawner : MonoBehaviour {
        [SerializeField] Bloc _prefab;
        [SerializeField] BlocSO[] _pool;
        [SerializeField] Board _board;
        [Tooltip("Nombre de prochaines pièces connues à l'avance.")]
        [SerializeField, Min(1)] int _previewCount = 3;

        readonly List<BlocSO> _nextBlocs = new();

        /// <summary>Prochaines pièces, de la plus proche (index 0) à la plus lointaine.</summary>
        public IReadOnlyList<BlocSO> NextBlocs => _nextBlocs;

        void Awake() {
            FillQueue();
        }

        /// <summary>Crée la prochaine pièce en haut du Board et fait avancer la file.</summary>
        public Bloc SpawnNext() {
            BlocSO data = _nextBlocs[0];
            _nextBlocs.RemoveAt(0);
            FillQueue();

            Bloc bloc = Instantiate(_prefab);
            bloc.Init(data, _board);

            EventBus<NextBlocsChangedEvent>.Publish(new NextBlocsChangedEvent(_nextBlocs));
            EventBus<BlocSpawnedEvent>.Publish(new BlocSpawnedEvent());
            return bloc;
        }

        void FillQueue() {
            while (_nextBlocs.Count < _previewCount) {
                _nextBlocs.Add(_pool[Random.Range(0, _pool.Length)]);
            }
        }
    }
}
