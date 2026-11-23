using System.Collections.Generic;
using UnityEngine;

namespace Tetris {
    /// <summary>Catégories du Casier : ce que le joueur peut personnaliser en jeu.</summary>
    public enum LockerCategory { Music, Background, Blocks, Board, Sounds, Cursor }

    /// <summary>Rareté d'un objet de la boutique. Sa valeur est son prix en compliments.</summary>
    public enum Rarity { Common = 1, Rare = 2, Epic = 3, Legendary = 4, Mythic = 5 }

    /// <summary>Forme des cases des pièces. Classic : le sprite du prefab Bloc (CellGlossy).</summary>
    public enum CellStyle { Classic, Flat, Bubble, Pixel }

    public enum BackgroundStyle { Liquid, Cover, Void, Disco }

    /// <summary>
    /// Un objet du Casier. Les champs utilisés dépendent de la catégorie :
    /// musique (<see cref="Resource"/> ou <see cref="LibraryIndex"/>), sons (<see cref="Resource"/> : dossier du pack),
    /// fond, blocs et curseur (<see cref="Colors"/>), plateau (couleurs Board*).
    /// </summary>
    public class LockerItem {
        public string Id;
        public LockerCategory Category;
        public string Name;
        public string Description;
        /// <summary>Texte affiché en grand sur la vignette (packs de sons).</summary>
        public string Tagline;
        public Rarity Rarity = Rarity.Common;
        /// <summary>Objet de base : disponible dans le Casier sans passer par la boutique.</summary>
        public bool IsFree;

        public string Resource;
        public int LibraryIndex = -1;
        public string Artist;
        /// <summary>Musique : garde la musique de la scène de jeu (Subway Surfers).</summary>
        public bool IsSceneDefault;
        /// <summary>Musique : une musique possédée au hasard à chaque partie.</summary>
        public bool IsRandom;

        public Color[] Colors;
        public CellStyle CellStyle;
        public BackgroundStyle BackgroundStyle;
        /// <summary>Curseur : la traînée passe par toutes les couleurs.</summary>
        public bool IsRainbow;

        public bool HasBoardColors;
        public Color BoardFill;
        public Color BoardOutline;
        public Color BoardSlot;
        public Color BoardLimit;

        public int Price => IsFree ? 0 : (int)Rarity;
    }

    /// <summary>
    /// Tous les objets du Casier et de la boutique. Les objets gratuits (<see cref="LockerItem.IsFree"/>) sont
    /// dans le Casier dès le départ ; les autres s'achètent en compliments (<see cref="PlayerInventory"/>).
    /// Le contenu (sons, musiques, covers) est dans Resources/Locker, chargé par <see cref="LockerContent"/>.
    /// Les couleurs des pièces sont dans l'ordre I, J, L, O, S, T, Z.
    /// </summary>
    public static class LockerCatalog {
        public const string PieceOrder = "IJLOSTZ";

        static List<LockerItem> _items;
        static Dictionary<string, LockerItem> _byId;

        public static IReadOnlyList<LockerItem> Items {
            get {
                if (_items == null) Build();
                return _items;
            }
        }

        public static LockerItem Get(string id) {
            if (_items == null) Build();
            return id != null && _byId.TryGetValue(id, out LockerItem item) ? item : null;
        }

        /// <summary>Objet équipé par défaut dans la catégorie (le premier objet gratuit).</summary>
        public static LockerItem Default(LockerCategory category) {
            foreach (LockerItem item in Items) {
                if (item.Category == category && item.IsFree) return item;
            }
            return null;
        }

        public static IEnumerable<LockerItem> InCategory(LockerCategory category) {
            foreach (LockerItem item in Items) {
                if (item.Category == category) yield return item;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() {
            _items = null;
            _byId = null;
        }

        static void Build() {
            _items = new List<LockerItem>();
            AddMusic();
            AddBackgrounds();
            AddBlocks();
            AddBoards();
            AddSounds();
            AddCursors();

            _byId = new Dictionary<string, LockerItem>();
            foreach (LockerItem item in _items) _byId[item.Id] = item;
        }

        // ---------- Musique ----------

        static void AddMusic() {
            Free(LockerCategory.Music, "music.default", "SUBWAY SURFERS", "La musique d'origine. Un classique de la salle d'attente.").IsSceneDefault = true;
            Free(LockerCategory.Music, "music.random", "ALÉATOIRE", "Une de tes musiques au hasard à chaque partie.").IsRandom = true;

            MusicLibrarySO library = LockerContent.Library;
            if (library != null && library.tracks != null) {
                for (int i = 0; i < library.tracks.Length; i++) {
                    LockerItem item = Free(LockerCategory.Music, $"music.lib.{i}", library.tracks[i].title.ToUpperInvariant(), "Déjà dans le menu, maintenant en jeu.");
                    item.LibraryIndex = i;
                    item.Artist = library.tracks[i].artist;
                }
            }

            Music("ouioui", "OUI-OUI ET LA POUDRE MAGIQUE", "Paulok", Rarity.Mythic, "Le chef-d'œuvre. Oui-Oui n'a jamais été aussi en forme.");
            Music("hitete", "LE HIT DE L'ÉTÉ", "Paulok", Rarity.Legendary, "45 secondes de pur génie, en boucle.");
            Music("megalovania", "MEGALOVANIA", "Toby Fox", Rarity.Legendary, "Tu vas passer un sale quart d'heure.");
            Music("trololo", "TROLOLO", "Eduard Khil", Rarity.Epic, "Ohohoho. Ahahaha. Ohohoho.");
            Music("coffindance", "COFFIN DANCE", "Vicetone & Tony Igy", Rarity.Epic, "Pour les parties qui finissent mal.");
            Music("pokemonfr", "ATTRAPEZ-LES TOUS", "Générique Pokémon", Rarity.Epic, "Attraper les lignes, c'est ma quête.");
            Music("dragostea", "DRAGOSTEA DIN TEI", "O-Zone", Rarity.Epic, "Ma-ia-hii, ma-ia-huu, ma-ia-hoo.");
            Music("crabrave", "CRAB RAVE", "Noisestorm", Rarity.Rare, "Les lignes sont parties, les crabes arrivent.");
            Music("caramelldansen", "CARAMELLDANSEN", "Caramella Girls", Rarity.Rare, "Dansez avec les mains sur la tête.");
            Music("toreador", "TOREADOR MARCH", "Freddy Fazbear", Rarity.Rare, "Il est 6 heures moins une.");
            Music("wiishop", "WII SHOP CHANNEL", "Nintendo", Rarity.Rare, "La musique de la boutique, pour toujours.");
            Music("nyancat", "NYAN CAT", "daniwell", Rarity.Common, "Nyan nyan nyan nyan nyan nyan.");
        }

        static void Music(string file, string name, string artist, Rarity rarity, string description) {
            LockerItem item = Shop(LockerCategory.Music, $"music.{file}", name, rarity, description);
            item.Resource = $"Locker/Music/{file}";
            item.Artist = artist;
        }

        // ---------- Fond ----------

        static void AddBackgrounds() {
            Free(LockerCategory.Background, "bg.liquid", "LIQUIDE", "Le fond d'origine : du verre et des bulles de couleur.");

            Palette("bg.sunset", "COUCHER DE SOLEIL", Rarity.Common, "Pour jouer comme dans un clip des années 80.",
                "#FF6B6B8C", "#FFB34773", "#FF4FA373", "#7A4CFF66", "#FFD16659");
            Palette("bg.matrix", "MATRIX", Rarity.Common, "Pilule rouge ou pilule bleue ? Pilule Tetris.",
                "#00FF4166", "#008F1173", "#003B0099", "#39FF144D", "#0D5C0D80");
            Palette("bg.abyss", "ABYSSES", Rarity.Common, "Tetris à 4000 mètres de profondeur.",
                "#0077B68C", "#00B4D866", "#023E8A99", "#48CAE44D", "#03045E99");
            Palette("bg.lava", "LAVE", Rarity.Rare, "Le sol est de la lave. Le plateau aussi.",
                "#FF24008C", "#FF7B0073", "#8B000099", "#FFB00059", "#D0000080");

            Shop(LockerCategory.Background, "bg.void", "LE NÉANT", Rarity.Common, "Rien. Absolument rien. Pour les puristes.")
                .BackgroundStyle = BackgroundStyle.Void;
            Shop(LockerCategory.Background, "bg.cover", "POCHETTE", Rarity.Epic, "La pochette de la musique en cours, en fond d'écran.")
                .BackgroundStyle = BackgroundStyle.Cover;
            Shop(LockerCategory.Background, "bg.disco", "BOULE À FACETTES", Rarity.Legendary, "Les couleurs tournent. Ta tête aussi.")
                .BackgroundStyle = BackgroundStyle.Disco;
        }

        static void Palette(string id, string name, Rarity rarity, string description, params string[] colors) {
            Shop(LockerCategory.Background, id, name, rarity, description).Colors = Hex(colors);
        }

        // ---------- Blocs ----------

        static void AddBlocks() {
            Free(LockerCategory.Blocks, "blocks.classic", "CLASSIQUE", "Les pièces d'origine, brillantes comme au premier jour.")
                .Colors = Hex("#00FFFF", "#0000FF", "#FF8000", "#FFFF00", "#00FF00", "#9900FF", "#FF0000");

            Blocks("blocks.pastel", "PASTEL", Rarity.Common, CellStyle.Flat, "Doux comme un macaron.",
                "#8FE3F0", "#9DB4FF", "#FFC59E", "#FFF1A1", "#B8F2B0", "#D5B3FF", "#FFA9B8");
            Blocks("blocks.mono", "NOIR ET BLANC", Rarity.Common, CellStyle.Flat, "Pour les parties en mode film d'auteur.",
                "#F2F2F2", "#9A9A9A", "#C8C8C8", "#FFFFFF", "#7A7A7A", "#B0B0B0", "#5E5E5E");
            Blocks("blocks.neon", "NÉON", Rarity.Rare, CellStyle.Flat, "Ça pique les yeux, c'est voulu.",
                "#00F0FF", "#3D5CFF", "#FF8A00", "#FFF200", "#39FF14", "#C000FF", "#FF1E56");
            Blocks("blocks.bubble", "BULLES", Rarity.Rare, CellStyle.Bubble, "Des billes. Qui s'empilent. Logique.",
                "#4DD9FF", "#4D7CFF", "#FFA64D", "#FFE14D", "#66E06B", "#B266FF", "#FF5C5C");
            Blocks("blocks.gameboy", "GAME BOY", Rarity.Epic, CellStyle.Pixel, "Quatre nuances de vert, comme en 1989.",
                "#9BBC0F", "#306230", "#8BAC0F", "#9BBC0F", "#306230", "#0F380F", "#8BAC0F");
            Blocks("blocks.gold", "LINGOTS", Rarity.Legendary, CellStyle.Classic, "Chaque ligne effacée vaut une fortune.",
                "#FFD700", "#E6B800", "#FFC627", "#FFE066", "#D4A017", "#F5C542", "#C9971C");
            Blocks("blocks.invisible", "INVISIBLES", Rarity.Mythic, CellStyle.Flat, "Les plus chers. On ne les voit presque pas. La classe ultime.",
                "#FFFFFF14", "#FFFFFF14", "#FFFFFF14", "#FFFFFF14", "#FFFFFF14", "#FFFFFF14", "#FFFFFF14");
        }

        static void Blocks(string id, string name, Rarity rarity, CellStyle style, string description, params string[] colors) {
            LockerItem item = Shop(LockerCategory.Blocks, id, name, rarity, description);
            item.CellStyle = style;
            item.Colors = Hex(colors);
        }

        // ---------- Plateau ----------

        static void AddBoards() {
            Free(LockerCategory.Board, "board.glass", "VERRE", "Le plateau d'origine, en verre poli.");

            Board("board.paper", "CAHIER", Rarity.Common, "Grands carreaux, marge rouge. Sors ta trousse.",
                "#FDFDF6EB", "#2B59C3E6", "#6FA0FF40", "#E53935E6");
            Board("board.void", "SANS PLATEAU", Rarity.Common, "Le plateau ? Quel plateau ?",
                "#00000000", "#00000000", "#00000000", "#FFFFFF73");
            Board("board.neon", "NÉON", Rarity.Rare, "Tetris dans une boîte de nuit.",
                "#05010FCC", "#00F0FFE6", "#00F0FF10", "#FF2E88CC");
            Board("board.terminal", "TERMINAL", Rarity.Epic, "sudo tetris --hard",
                "#000000E0", "#39FF14D9", "#39FF1412", "#39FF14B3");
            Board("board.gold", "CADRE DORÉ", Rarity.Legendary, "Velours rouge et dorures, comme à Versailles.",
                "#3A0A12D9", "#FFD24AFF", "#FFD24A12", "#FFD24ACC");
        }

        static void Board(string id, string name, Rarity rarity, string description, string fill, string outline, string slot, string limit) {
            LockerItem item = Shop(LockerCategory.Board, id, name, rarity, description);
            item.HasBoardColors = true;
            item.BoardFill = Hex(fill);
            item.BoardOutline = Hex(outline);
            item.BoardSlot = Hex(slot);
            item.BoardLimit = Hex(limit);
        }

        // ---------- Sons ----------

        static void AddSounds() {
            Free(LockerCategory.Sounds, "sfx.classic", "CLASSIQUE", "Le pop et le poisson-globe d'origine.").Tagline = "POP";

            Sounds("politique", "POLITIQUE FRANÇAISE", Rarity.Mythic, "FOR SURE",
                "Zemmour tousse à chaque pièce, Macron valide tes lignes. Qui aurait pu prédire ?");
            Sounds("fnaf", "FIVE NIGHTS AT FREDDY'S", Rarity.Legendary, "HONK",
                "Le nez de Freddy à chaque pièce, le carillon de 6 h pour les Tetris. Et le game over... surprise.");
            Sounds("pokemon", "POKÉMON", Rarity.Epic, "PIKA PIKA",
                "Pikachu pose tes pièces, la Team Rocket s'envole au game over.");
            Sounds("memes", "MÈMES", Rarity.Epic, "VINE BOOM",
                "Vine boom, anime wow, tuyau en métal. Le pack du connaisseur.");
            Sounds("minecraft", "MINECRAFT", Rarity.Rare, "OOF",
                "Steve a mal à chaque pièce. Le villageois n'est pas convaincu.");
            Sounds("windows", "WINDOWS XP", Rarity.Rare, "ERREUR",
                "Chaque pièce est une erreur critique. Le Tetris démarre Windows.");
            Sounds("amongus", "AMONG US", Rarity.Rare, "SUS",
                "Réunion d'urgence à chaque Tetris. Quelqu'un est l'imposteur.");
        }

        static void Sounds(string folder, string name, Rarity rarity, string tagline, string description) {
            LockerItem item = Shop(LockerCategory.Sounds, $"sfx.{folder}", name, rarity, description);
            item.Resource = $"Locker/Sfx/{folder}";
            item.Tagline = tagline;
        }

        // ---------- Curseur ----------

        static void AddCursors() {
            Free(LockerCategory.Cursor, "cursor.comet", "COMÈTE", "Une traînée blanche et bleue, sobre et élégante.")
                .Colors = Hex("#FFFFFF", "#7DE6FF");
            Free(LockerCategory.Cursor, "cursor.none", "AUCUNE", "Pas de traînée. Le dessin à la touche C marche toujours.")
                .Colors = new Color[0];

            Shop(LockerCategory.Cursor, "cursor.toxic", "TOXIQUE", Rarity.Common, "Ne pas toucher. Ne pas lécher.")
                .Colors = Hex("#D4FF3B", "#00C853");
            Shop(LockerCategory.Cursor, "cursor.rainbow", "ARC-EN-CIEL", Rarity.Rare, "Toutes les couleurs, tout le temps.")
                .IsRainbow = true;
            Shop(LockerCategory.Cursor, "cursor.fire", "FEU", Rarity.Epic, "Ton curseur est en feu. Littéralement.")
                .Colors = Hex("#FFF176", "#FF6D00", "#D50000");
            Shop(LockerCategory.Cursor, "cursor.gold", "PAILLETTES", Rarity.Legendary, "Ça brille, ça scintille, ça coûte cher.")
                .Colors = Hex("#FFF8D0", "#FFD24A", "#C9971C");
        }

        // ---------- Outils ----------

        static LockerItem Free(LockerCategory category, string id, string name, string description) {
            var item = new LockerItem { Id = id, Category = category, Name = name, Description = description, IsFree = true };
            _items.Add(item);
            return item;
        }

        static LockerItem Shop(LockerCategory category, string id, string name, Rarity rarity, string description) {
            var item = new LockerItem { Id = id, Category = category, Name = name, Description = description, Rarity = rarity };
            _items.Add(item);
            return item;
        }

        static Color[] Hex(params string[] colors) {
            var result = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++) result[i] = Hex(colors[i]);
            return result;
        }

        static Color Hex(string color) {
            return ColorUtility.TryParseHtmlString(color, out Color result) ? result : Color.magenta;
        }
    }
}
