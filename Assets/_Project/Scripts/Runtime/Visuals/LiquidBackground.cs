using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Fond "liquide" : de grosses taches de couleur floues qui dérivent lentement derrière le jeu.
    /// À placer au centre de la caméra. Les panneaux en verre du HUD prennent leur teinte par transparence.
    /// Le fond équipé dans le Casier peut changer les couleurs (<see cref="SetColors"/>) ou les faire tourner (<see cref="HueSpeed"/>).
    /// </summary>
    public class LiquidBackground : MonoBehaviour {
        [SerializeField] Sprite _blobSprite;
        [SerializeField] Color[] _colors = {
            new Color(0.49f, 0.36f, 1f, 0.55f),
            new Color(0.13f, 0.83f, 0.93f, 0.40f),
            new Color(1f, 0.31f, 0.85f, 0.40f),
            new Color(0.23f, 0.51f, 0.96f, 0.50f),
            new Color(0.55f, 0.2f, 0.95f, 0.45f),
        };
        [Tooltip("Zone (en unités) dans laquelle les taches se déplacent, centrée sur cet objet.")]
        [SerializeField] Vector2 _area = new Vector2(34f, 20f);
        [SerializeField] float _blobSize = 2.4f;
        [SerializeField] float _speed = 0.05f;
        [SerializeField] int _sortingOrder = -100;

        Transform[] _blobs;
        SpriteRenderer[] _renderers;
        float[] _seeds;

        /// <summary>Tours de roue chromatique par seconde (0 : couleurs fixes).</summary>
        public float HueSpeed { get; set; }

        /// <summary>Remplace les couleurs des taches. Un tableau vide : plus aucune tache.</summary>
        public void SetColors(Color[] colors) {
            _colors = colors;
            if (_blobs != null) CreateBlobs();
        }

        void Start() {
            CreateBlobs();
        }

        void CreateBlobs() {
            if (_blobs != null) {
                foreach (Transform blob in _blobs) Destroy(blob.gameObject);
            }

            _blobs = new Transform[_colors.Length];
            _renderers = new SpriteRenderer[_colors.Length];
            _seeds = new float[_colors.Length];

            for (int i = 0; i < _colors.Length; i++) {
                var blob = new GameObject($"Blob {i}");
                blob.transform.SetParent(transform, false);

                var spriteRenderer = blob.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = _blobSprite;
                spriteRenderer.color = _colors[i];
                spriteRenderer.sortingOrder = _sortingOrder;

                _blobs[i] = blob.transform;
                _renderers[i] = spriteRenderer;
                _seeds[i] = i * 17.3f + 3.1f;
            }
        }

        void Update() {
            float t = Time.unscaledTime * _speed;

            for (int i = 0; i < _blobs.Length; i++) {
                float seed = _seeds[i];
                float x = Mathf.PerlinNoise(seed, t) - 0.5f;
                float y = Mathf.PerlinNoise(t, seed) - 0.5f;
                _blobs[i].localPosition = new Vector3(x * _area.x, y * _area.y);

                float pulse = 1f + 0.25f * Mathf.Sin(t * 6f + seed);
                _blobs[i].localScale = Vector3.one * (_blobSize * pulse);

                if (HueSpeed > 0f) {
                    Color.RGBToHSV(_colors[i], out float hue, out float saturation, out float value);
                    Color color = Color.HSVToRGB(Mathf.Repeat(hue + Time.unscaledTime * HueSpeed + i * 0.2f, 1f), Mathf.Max(saturation, 0.7f), value);
                    color.a = _colors[i].a;
                    _renderers[i].color = color;
                }
            }
        }
    }
}
