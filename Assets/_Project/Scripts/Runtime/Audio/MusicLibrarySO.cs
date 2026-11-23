using System;
using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Liste de musiques avec leur illustration (image 16:9), utilisée par le carrousel du menu principal.
    /// </summary>
    [CreateAssetMenu(fileName = "MusicLibrary", menuName = "Tetris/Music Library")]
    public class MusicLibrarySO : ScriptableObject {
        [Serializable]
        public class Track {
            public string title;
            public string artist;
            public AudioClip clip;
            [Tooltip("Illustration au format 16:9.")]
            public Sprite cover;
            [Tooltip("Affiche la vidéo de fond du menu à la place de l'illustration.")]
            public bool useMenuVideoAsCover;
        }

        public Track[] tracks;
    }
}
