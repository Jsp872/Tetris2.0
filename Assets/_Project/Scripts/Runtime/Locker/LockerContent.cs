using System.Collections.Generic;
using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Charge le contenu des objets du Casier depuis Resources : musiques et covers, sons,
    /// sprites des cases (générés en code), couleurs des pièces. Tout est mis en cache.
    /// </summary>
    public static class LockerContent {
        const int CellTextureSize = 64;
        /// <summary>Taille d'une case générée, en unités (un peu moins de 1 pour laisser un joint entre les cases).</summary>
        const float CellWorldSize = 0.95f;

        static MusicLibrarySO _library;
        static readonly Dictionary<CellStyle, Texture2D> _cellTextures = new();
        static readonly Dictionary<CellStyle, Sprite> _cellSprites = new();
        static readonly Dictionary<string, Sprite> _coverSprites = new();

        /// <summary>Musique choisie pour la partie en cours (après tirage si "Aléatoire").</summary>
        public static LockerItem CurrentGameplayMusic { get; set; }

        /// <summary>Musiques du menu (Resources/MusicLibrary).</summary>
        public static MusicLibrarySO Library => _library != null ? _library : _library = Resources.Load<MusicLibrarySO>("MusicLibrary");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() {
            _coverSprites.Clear();
            CurrentGameplayMusic = null;
        }

        // ---------- Musique ----------

        /// <summary>Clip de la musique. Null pour la musique d'origine de la scène et pour "Aléatoire".</summary>
        public static AudioClip GetMusicClip(LockerItem item) {
            if (item == null) return null;
            if (item.LibraryIndex >= 0 && Library != null) return Library.tracks[item.LibraryIndex].clip;
            return item.Resource != null ? Resources.Load<AudioClip>(item.Resource) : null;
        }

        /// <summary>Cover 16:9 de la musique, ou null.</summary>
        public static Texture2D GetCover(LockerItem item) {
            if (item == null) return null;
            if (item.LibraryIndex >= 0 && Library != null) {
                Sprite cover = Library.tracks[item.LibraryIndex].cover;
                return cover != null ? cover.texture : null;
            }
            return item.Resource != null && item.Category == LockerCategory.Music ? Resources.Load<Texture2D>(item.Resource) : null;
        }

        public static Sprite GetCoverSprite(LockerItem item) {
            if (item == null) return null;
            if (item.LibraryIndex >= 0 && Library != null) return Library.tracks[item.LibraryIndex].cover;

            if (_coverSprites.TryGetValue(item.Id, out Sprite sprite) && sprite != null) return sprite;
            Texture2D texture = GetCover(item);
            if (texture == null) return null;

            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            _coverSprites[item.Id] = sprite;
            return sprite;
        }

        /// <summary>Musiques possédées, sans "Aléatoire".</summary>
        public static List<LockerItem> GetOwnedMusic() {
            var result = new List<LockerItem>();
            foreach (LockerItem item in LockerCatalog.InCategory(LockerCategory.Music)) {
                if (!item.IsRandom && PlayerInventory.Owns(item)) result.Add(item);
            }
            return result;
        }

        /// <summary>
        /// Musique de la partie : celle équipée dans le Casier (tirée au hasard pour "Aléatoire").
        /// <paramref name="sceneDefault"/> sert pour la musique d'origine et si le clip est introuvable.
        /// </summary>
        public static AudioClip ResolveGameplayMusic(AudioClip sceneDefault) {
            LockerItem item = PlayerInventory.GetEquipped(LockerCategory.Music);
            if (item.IsRandom) {
                List<LockerItem> owned = GetOwnedMusic();
                item = owned[Random.Range(0, owned.Count)];
            }

            CurrentGameplayMusic = item;
            AudioClip clip = item.IsSceneDefault ? null : GetMusicClip(item);
            return clip != null ? clip : sceneDefault;
        }

        // ---------- Sons ----------

        /// <summary>
        /// Clip d'un son. Null pour le silence et pour le son d'origine (que GameplaySfx prend dans la scène).
        /// Pour un pack : un de ses sons au hasard (aperçu).
        /// </summary>
        public static AudioClip GetSoundClip(LockerItem item) {
            if (item == null) return null;
            if (item.IsPack) {
                return item.Contents.Count > 0 ? GetSoundClip(item.Contents[Random.Range(0, item.Contents.Count)]) : null;
            }
            return item.Resource != null ? Resources.Load<AudioClip>(item.Resource) : null;
        }

        public static string SlotName(SoundSlot slot) => slot switch {
            SoundSlot.Spawn => "APPARITION",
            SoundSlot.Move => "DÉPLACEMENT",
            SoundSlot.Rotate => "ROTATION",
            SoundSlot.Lock => "POSE",
            SoundSlot.Line => "LIGNE",
            SoundSlot.Tetris => "TETRIS",
            _ => "GAME OVER",
        };

        public static AudioClip GetUiClip(string name) {
            return Resources.Load<AudioClip>($"Locker/Ui/{name}");
        }

        // ---------- Blocs ----------

        /// <summary>Couleur de la pièce avec le skin équipé (ou <paramref name="skin"/>).</summary>
        public static Color GetPieceColor(BlocSO data, LockerItem skin = null) {
            skin ??= PlayerInventory.GetEquipped(LockerCategory.Blocks);
            int index = PieceIndex(data);
            return skin.Colors != null && index >= 0 && index < skin.Colors.Length ? skin.Colors[index] : data.color;
        }

        /// <summary>Index de la pièce dans <see cref="LockerCatalog.PieceOrder"/>, d'après le nom de l'asset (Bloc_T...).</summary>
        public static int PieceIndex(BlocSO data) {
            if (data == null || string.IsNullOrEmpty(data.name)) return -1;
            return LockerCatalog.PieceOrder.IndexOf(char.ToUpperInvariant(data.name[data.name.Length - 1]));
        }

        /// <summary>Sprite de case du skin, ou <paramref name="classic"/> pour le style d'origine.</summary>
        public static Sprite GetCellSprite(LockerItem skin, Sprite classic) {
            skin ??= PlayerInventory.GetEquipped(LockerCategory.Blocks);
            if (skin.CellStyle == CellStyle.Classic) return classic;

            if (!_cellSprites.TryGetValue(skin.CellStyle, out Sprite sprite) || sprite == null) {
                Texture2D texture = GetCellTexture(skin.CellStyle);
                sprite = Sprite.Create(texture, new Rect(0, 0, CellTextureSize, CellTextureSize), new Vector2(0.5f, 0.5f),
                    CellTextureSize / CellWorldSize);
                _cellSprites[skin.CellStyle] = sprite;
            }
            return sprite;
        }

        /// <summary>Texture d'une case (pour les vignettes du menu) : CellGlossy pour le style d'origine, générée sinon.</summary>
        public static Texture2D GetCellTexture(CellStyle style) {
            if (_cellTextures.TryGetValue(style, out Texture2D texture) && texture != null) return texture;

            texture = style == CellStyle.Classic ? Resources.Load<Texture2D>("Locker/CellGlossy") : GenerateCell(style);
            _cellTextures[style] = texture;
            return texture;
        }

        /// <summary>Case en niveaux de gris (teintée ensuite par la couleur de la pièce).</summary>
        static Texture2D GenerateCell(CellStyle style) {
            int size = CellTextureSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) {
                filterMode = style == CellStyle.Pixel ? FilterMode.Point : FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    float shade;
                    float alpha;

                    switch (style) {
                        case CellStyle.Bubble: {
                            float distance = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f));
                            alpha = Mathf.Clamp01((0.48f - distance) * size);
                            float highlight = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.36f, 0.66f)) * 5f);
                            shade = Mathf.Lerp(0.7f, 1f, v) * (distance > 0.4f ? 0.82f : 1f);
                            shade = Mathf.Lerp(shade, 1f, highlight) + highlight * 0.4f;
                            break;
                        }
                        case CellStyle.Pixel: {
                            int border = size / 10;
                            bool isBorder = x < border || y < border || x >= size - border || y >= size - border;
                            bool isLight = !isBorder && (x < border * 2 || y >= size - border * 2);
                            bool isDark = !isBorder && (x >= size - border * 2 || y < border * 2);
                            shade = isBorder ? 0.3f : isLight ? 1f : isDark ? 0.62f : 0.82f;
                            alpha = 1f;
                            break;
                        }
                        default: {
                            float radius = 0.16f;
                            float dx = Mathf.Max(Mathf.Abs(u - 0.5f) - (0.5f - radius), 0f);
                            float dy = Mathf.Max(Mathf.Abs(v - 0.5f) - (0.5f - radius), 0f);
                            alpha = Mathf.Clamp01((radius - Mathf.Sqrt(dx * dx + dy * dy)) * size);
                            shade = Mathf.Lerp(0.86f, 1f, v);
                            break;
                        }
                    }

                    byte value = (byte)(Mathf.Clamp01(shade) * 255f);
                    pixels[y * size + x] = new Color32(value, value, value, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
