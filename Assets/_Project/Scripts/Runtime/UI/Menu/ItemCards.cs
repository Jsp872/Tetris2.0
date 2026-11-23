using UnityEngine;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Cartes d'objets du Casier et de la boutique, façon boutique Fortnite : couleur de rareté, vignette,
    /// nom, catégorie et un pied de carte (prix, "POSSÉDÉ", "ÉQUIPÉ"...). Styles "item-card" dans MainMenu.uss.
    /// Les vignettes sont dessinées en code selon la catégorie : cover, palette, mini-pièces, mini-plateau, traînée...
    /// </summary>
    public static class ItemCards {
        public static string RarityName(Rarity rarity) => rarity switch {
            Rarity.Rare => "RARE",
            Rarity.Epic => "ÉPIQUE",
            Rarity.Legendary => "LÉGENDAIRE",
            Rarity.Mythic => "MYTHIQUE",
            _ => "COMMUN",
        };

        public static Color RarityColor(Rarity rarity) => rarity switch {
            Rarity.Rare => new Color(0.23f, 0.55f, 1f),
            Rarity.Epic => new Color(0.65f, 0.27f, 1f),
            Rarity.Legendary => new Color(1f, 0.54f, 0.12f),
            Rarity.Mythic => new Color(1f, 0.79f, 0.23f),
            _ => new Color(0.37f, 0.75f, 0.23f),
        };

        public static string CategoryName(LockerCategory category) => category switch {
            LockerCategory.Music => "MUSIQUE",
            LockerCategory.Background => "FOND",
            LockerCategory.Blocks => "BLOCS",
            LockerCategory.Board => "PLATEAU",
            LockerCategory.Sounds => "SONS",
            _ => "CURSEUR",
        };

        /// <summary>Carte cliquable. Les objets de base ont la couleur "commun" mais sans mention de rareté.</summary>
        public static Button CreateCard(LockerItem item, bool isBig = false) {
            Color rarity = item.IsFree ? new Color(0.55f, 0.6f, 0.75f) : RarityColor(item.Rarity);

            var card = new Button();
            card.AddToClassList("item-card");
            if (isBig) card.AddToClassList("item-card--big");
            card.style.backgroundColor = Color.Lerp(rarity, Color.black, 0.55f);
            SetBorder(card, rarity);

            VisualElement thumbnail = CreateThumbnail(item);
            thumbnail.AddToClassList("item-card__thumb");
            thumbnail.style.backgroundColor = Color.Lerp(rarity, Color.black, 0.25f);
            card.Add(thumbnail);

            var info = new VisualElement { pickingMode = PickingMode.Ignore };
            info.AddToClassList("item-card__info");
            info.Add(CreateLabel(item.Name, "item-card__name"));
            string subtitle = item.IsFree ? $"{CategoryName(item.Category)}  ·  DE BASE" : $"{CategoryName(item.Category)}  ·  {RarityName(item.Rarity)}";
            Label category = CreateLabel(subtitle, "item-card__category");
            category.style.color = Color.Lerp(rarity, Color.white, 0.35f);
            info.Add(category);
            card.Add(info);

            Label footer = CreateLabel("", "item-card__footer");
            footer.name = "footer";
            card.Add(footer);
            return card;
        }

        /// <summary>Texte du pied de carte, avec une classe de style optionnelle (item-card__footer--owned, --equipped...).</summary>
        public static void SetFooter(VisualElement card, string text, string modifier = null) {
            Label footer = card.Q<Label>("footer");
            footer.text = text;
            footer.RemoveFromClassList("item-card__footer--owned");
            footer.RemoveFromClassList("item-card__footer--equipped");
            footer.RemoveFromClassList("item-card__footer--price");
            if (modifier != null) footer.AddToClassList(modifier);
            card.EnableInClassList("item-card--equipped", modifier == "item-card__footer--equipped");
        }

        public static string PriceText(int price) => price <= 1 ? "1 COMPLIMENT" : $"{price} COMPLIMENTS";

        // ---------- Vignettes ----------

        public static VisualElement CreateThumbnail(LockerItem item) {
            var thumbnail = new VisualElement { pickingMode = PickingMode.Ignore };
            switch (item.Category) {
                case LockerCategory.Music: MusicThumbnail(thumbnail, item); break;
                case LockerCategory.Background: BackgroundThumbnail(thumbnail, item); break;
                case LockerCategory.Blocks: BlocksThumbnail(thumbnail, item); break;
                case LockerCategory.Board: BoardThumbnail(thumbnail, item); break;
                case LockerCategory.Sounds: BigText(thumbnail, item.Tagline ?? item.Name); break;
                default: CursorThumbnail(thumbnail, item); break;
            }
            return thumbnail;
        }

        static void MusicThumbnail(VisualElement thumbnail, LockerItem item) {
            Texture2D cover = LockerContent.GetCover(item);
            if (cover != null) {
                thumbnail.style.backgroundImage = Background.FromTexture2D(cover);
                thumbnail.AddToClassList("item-card__thumb--cover");
                return;
            }
            BigText(thumbnail, item.IsRandom ? "?" : item.Name);
        }

        static void BackgroundThumbnail(VisualElement thumbnail, LockerItem item) {
            switch (item.BackgroundStyle) {
                case BackgroundStyle.Void:
                    thumbnail.style.backgroundColor = Color.black;
                    BigText(thumbnail, "...");
                    return;
                case BackgroundStyle.Cover:
                    LockerItem music = LockerCatalog.Get("music.lib.0");
                    Texture2D cover = LockerContent.GetCover(music);
                    if (cover != null) {
                        thumbnail.style.backgroundImage = Background.FromTexture2D(cover);
                        thumbnail.AddToClassList("item-card__thumb--cover");
                    }
                    BigText(thumbnail, "TA MUSIQUE");
                    return;
            }

            Color[] colors = item.Colors ?? new[] {
                new Color(0.49f, 0.36f, 1f), new Color(0.13f, 0.83f, 0.93f), new Color(1f, 0.31f, 0.85f),
                new Color(0.23f, 0.51f, 0.96f), new Color(0.55f, 0.2f, 0.95f),
            };
            thumbnail.style.backgroundColor = new Color(0.04f, 0.05f, 0.12f);
            for (int i = 0; i < colors.Length; i++) {
                Color color = item.BackgroundStyle == BackgroundStyle.Disco ? Color.HSVToRGB(i / (float)colors.Length, 0.8f, 1f) : colors[i];
                color.a = Mathf.Max(color.a, 0.55f);
                float size = 70f + (i * 37 % 50);
                var blob = new VisualElement { pickingMode = PickingMode.Ignore };
                blob.AddToClassList("item-thumb__blob");
                blob.style.width = size;
                blob.style.height = size;
                blob.style.left = Length.Percent(i * 21 % 90);
                blob.style.top = Length.Percent(i * 37 % 70);
                blob.style.backgroundColor = color;
                thumbnail.Add(blob);
            }
        }

        /// <summary>Trois mini-pièces (T, S, I) avec le sprite et les couleurs du skin.</summary>
        static void BlocksThumbnail(VisualElement thumbnail, LockerItem item) {
            thumbnail.style.backgroundColor = new Color(0.04f, 0.05f, 0.12f);
            Texture2D texture = LockerContent.GetCellTexture(item.CellStyle);
            const float cell = 22f;

            (int piece, Vector2Int[] cells, Vector2 origin)[] pieces = {
                (5, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1) }, new Vector2(18, 70)),
                (4, new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }, new Vector2(98, 22)),
                (0, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) }, new Vector2(118, 92)),
            };

            foreach ((int piece, Vector2Int[] cells, Vector2 origin) in pieces) {
                Color color = item.Colors != null && piece < item.Colors.Length ? item.Colors[piece] : Color.white;
                color.a = Mathf.Max(color.a, 0.12f);
                foreach (Vector2Int position in cells) {
                    var square = new VisualElement { pickingMode = PickingMode.Ignore };
                    square.style.position = Position.Absolute;
                    square.style.left = origin.x + position.x * cell;
                    square.style.top = origin.y - position.y * cell;
                    square.style.width = cell - 1f;
                    square.style.height = cell - 1f;
                    if (texture != null) {
                        square.style.backgroundImage = Background.FromTexture2D(texture);
                        square.style.unityBackgroundImageTintColor = color;
                    }
                    else {
                        square.style.backgroundColor = color;
                    }
                    thumbnail.Add(square);
                }
            }
        }

        /// <summary>Mini-plateau : fond, contour, cases et ligne de limite aux couleurs du skin.</summary>
        static void BoardThumbnail(VisualElement thumbnail, LockerItem item) {
            thumbnail.style.backgroundColor = new Color(0.1f, 0.08f, 0.25f);
            Color fill = item.HasBoardColors ? item.BoardFill : new Color(0.04f, 0.05f, 0.12f, 0.55f);
            Color outline = item.HasBoardColors ? item.BoardOutline : new Color(1f, 1f, 1f, 0.35f);
            Color slot = item.HasBoardColors ? item.BoardSlot : new Color(1f, 1f, 1f, 0.04f);
            Color limit = item.HasBoardColors ? item.BoardLimit : new Color(1f, 1f, 1f, 0.45f);
            slot.a = Mathf.Max(slot.a, 0.12f);

            var board = new VisualElement { pickingMode = PickingMode.Ignore };
            board.AddToClassList("item-thumb__board");
            board.style.backgroundColor = fill;
            SetBorder(board, outline);
            thumbnail.Add(board);

            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 6; x++) {
                    var square = new VisualElement { pickingMode = PickingMode.Ignore };
                    square.style.position = Position.Absolute;
                    square.style.left = 4 + x * 13;
                    square.style.top = 4 + y * 13;
                    square.style.width = 11;
                    square.style.height = 11;
                    square.style.borderTopLeftRadius = square.style.borderTopRightRadius =
                        square.style.borderBottomLeftRadius = square.style.borderBottomRightRadius = 3;
                    square.style.backgroundColor = slot;
                    board.Add(square);
                }
            }

            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.style.position = Position.Absolute;
            line.style.left = 3;
            line.style.right = 3;
            line.style.top = 29;
            line.style.height = 2;
            line.style.backgroundColor = limit;
            board.Add(line);
        }

        /// <summary>Une courbe de points qui rétrécissent, aux couleurs de la traînée.</summary>
        static void CursorThumbnail(VisualElement thumbnail, LockerItem item) {
            thumbnail.style.backgroundColor = new Color(0.04f, 0.05f, 0.12f);
            if (!item.IsRainbow && (item.Colors == null || item.Colors.Length == 0)) {
                BigText(thumbnail, "AUCUNE");
                return;
            }

            const int count = 16;
            for (int i = 0; i < count; i++) {
                float t = i / (float)(count - 1);
                Color color = item.IsRainbow ? Color.HSVToRGB(t, 0.8f, 1f) : SampleGradient(item.Colors, t);
                color.a = 1f - t * 0.7f;
                float size = Mathf.Lerp(26f, 6f, t);

                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.AddToClassList("item-thumb__blob");
                dot.style.width = size;
                dot.style.height = size;
                dot.style.left = Length.Percent(78f - t * 66f);
                dot.style.top = Length.Percent(40f + Mathf.Sin(t * Mathf.PI * 2f) * 24f);
                dot.style.backgroundColor = color;
                thumbnail.Add(dot);
            }
        }

        static Color SampleGradient(Color[] colors, float t) {
            if (colors.Length == 1) return colors[0];
            float scaled = t * (colors.Length - 1);
            int index = Mathf.Min((int)scaled, colors.Length - 2);
            return Color.Lerp(colors[index], colors[index + 1], scaled - index);
        }

        static void BigText(VisualElement thumbnail, string text) {
            Label label = CreateLabel(text, "item-thumb__text");
            thumbnail.Add(label);
        }

        static void SetBorder(VisualElement element, Color color) {
            element.style.borderTopColor = element.style.borderBottomColor =
                element.style.borderLeftColor = element.style.borderRightColor = color;
        }

        public static Label CreateLabel(string text, string className) {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            return label;
        }
    }
}
