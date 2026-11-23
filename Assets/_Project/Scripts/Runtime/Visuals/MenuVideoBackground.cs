using UnityEngine;
using UnityEngine.Video;

namespace Tetris {
    /// <summary>
    /// Lit une vidéo en boucle dans une texture (<see cref="Texture"/>), utilisée comme fond et illustration par le carrousel.
    /// <see cref="LoopFade"/> donne un fondu à chaque boucle (0 au début et à la fin, 1 au milieu)
    /// pour masquer le saut du retour au début.
    /// </summary>
    public class MenuVideoBackground : MonoBehaviour {
        [SerializeField] VideoClip _clip;
        [Tooltip("Durée du fondu d'entrée et de sortie à chaque boucle, en secondes.")]
        [SerializeField] float _fadeDuration = 0.6f;

        VideoPlayer _player;
        RenderTexture _texture;

        /// <summary>Image de la vidéo en cours de lecture (null si aucune vidéo).</summary>
        public RenderTexture Texture => _texture;

        /// <summary>Opacité conseillée pour la vidéo à cet instant de la boucle.</summary>
        public float LoopFade {
            get {
                if (_player == null || !_player.isPlaying) return 0f;

                float length = (float)_clip.length;
                float time = (float)(_player.time % length);
                return Mathf.Clamp01(Mathf.Min(time, length - time) / Mathf.Max(_fadeDuration, 0.01f));
            }
        }

        void Awake() {
            if (_clip == null) return;

            _texture = new RenderTexture((int)_clip.width, (int)_clip.height, 0);
            _player = gameObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.clip = _clip;
            _player.isLooping = true;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.targetTexture = _texture;
            _player.audioOutputMode = VideoAudioOutputMode.None;
        }

        void Start() {
            _player?.Play();
        }

        void OnDestroy() {
            if (_texture != null) {
                _texture.Release();
                Destroy(_texture);
            }
        }
    }
}
