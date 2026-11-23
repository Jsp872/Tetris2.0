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

    /// <summary>Moment de la partie où un son se joue. Le Casier équipe un son par moment.</summary>
    public enum SoundSlot { Spawn, Move, Rotate, Lock, Line, Tetris, GameOver, SoftDrop }

    /// <summary>
    /// Un objet du Casier. Les champs utilisés dépendent de la catégorie :
    /// musique (<see cref="Resource"/> ou <see cref="LibraryIndex"/>), sons (<see cref="Resource"/> et <see cref="Slot"/>),
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

        /// <summary>Pack de la boutique : ne s'équipe pas, il donne les sons de <see cref="Contents"/>.</summary>
        public bool IsPack;
        public List<LockerItem> Contents = new();
        /// <summary>Son : pack qui le donne.</summary>
        public string PackId;
        /// <summary>Son : moment de la partie où il se joue.</summary>
        public SoundSlot Slot;

        public string Resource;
        public int LibraryIndex = -1;
        public string Artist;
        /// <summary>Musique ou son : garde le clip réglé dans la scène de jeu (Subway Surfers, pop d'origine...).</summary>
        public bool IsSceneDefault;

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

        /// <summary>Vendu tel quel dans la boutique (les sons s'obtiennent avec leur pack).</summary>
        public bool IsSellable => !IsFree && PackId == null;
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

        /// <summary>Son équipé par défaut pour un moment : l'original s'il existe, sinon le silence.</summary>
        public static LockerItem DefaultSound(SoundSlot slot) {
            foreach (LockerItem item in Items) {
                if (item.Category == LockerCategory.Sounds && item.IsFree && item.Slot == slot) return item;
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

            MusicLibrarySO library = LockerContent.Library;
            if (library != null && library.tracks != null) {
                for (int i = 0; i < library.tracks.Length; i++) {
                    LockerItem item = Free(LockerCategory.Music, $"music.lib.{i}", library.tracks[i].title.ToUpperInvariant(), "Déjà dans le menu, maintenant en jeu.");
                    item.LibraryIndex = i;
                    item.Artist = library.tracks[i].artist;
                }
            }
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
            // Sons de base : les sons d'origine pour l'apparition et la pose, et un silence pour chaque moment.
            foreach (SoundSlot slot in System.Enum.GetValues(typeof(SoundSlot))) {
                if (slot is SoundSlot.Spawn or SoundSlot.Lock) {
                    LockerItem original = Free(LockerCategory.Sounds, $"sfx.original.{slot}", "ORIGINAL", "Le son d'origine du jeu.");
                    original.Slot = slot;
                    original.IsSceneDefault = true;
                    original.Resource = $"Locker/Sfx/original/{(slot == SoundSlot.Lock ? "lock" : "spawn")}_original";
                    original.Tagline = slot == SoundSlot.Lock ? "POP" : "BLOUP";
                }
                LockerItem silence = Free(LockerCategory.Sounds, $"sfx.none.{slot}", "SILENCE", "Rien. Le calme absolu.");
                silence.Slot = slot;
                silence.Tagline = "...";
            }

            LockerItem elevator = Free(LockerCategory.Sounds, "sfx.original.elevator", "MUSIQUE D'ASCENSEUR", "Pour descendre en douceur.");
            elevator.Slot = SoundSlot.SoftDrop;
            elevator.Resource = "Locker/Sfx/original/softdrop_elevator";
            elevator.Tagline = "DING DONG";

            Pack("descente", "BANDES-SONS DE DESCENTE", Rarity.Legendary, "YAKETY SAX",
                "Une musique qui tourne tant que tu fais descendre la pièce : Yakety Sax, Déjà Vu, Gas Gas Gas, et le générique de Oui-Oui par Paulok.");

            Pack("politique", "POLITIQUE FRANÇAISE", Rarity.Mythic, "FOR SURE",
                "Zemmour tousse, Macron valide, Lassalle s'emballe et Sarkozy s'indigne. Qui aurait pu prédire ?");
            Pack("fnaf", "FIVE NIGHTS AT FREDDY'S", Rarity.Legendary, "HONK",
                "Le nez de Freddy, la porte blindée, le carillon de 6 h. Et le game over... surprise.");
            Pack("memes", "MÈMES", Rarity.Legendary, "VINE BOOM",
                "Vine boom, tuyau en métal, FBI open up, emotional damage. Le pack du connaisseur.");
            Pack("amongus", "AMONG US", Rarity.Epic, "AMOGUS",
                "AMOGUS saturé à fond, réunion d'urgence et kill. Quelqu'un est très sus.");
            Pack("pokemon", "POKÉMON", Rarity.Epic, "PIKA PIKA",
                "Pikachu, Rondoudou, Psykokwak, et la Team Rocket qui s'envole au game over.");
            Pack("minecraft", "MINECRAFT", Rarity.Rare, "OOF",
                "Steve a mal, le villageois n'est pas convaincu et le creeper fait son travail.");
            Pack("windows", "WINDOWS XP", Rarity.Rare, "ERREUR",
                "Chaque pièce est une erreur critique. Le Tetris démarre Windows.");

            Sound("amongus", SoundSlot.Spawn, "spawn_reveal", "RÉVÉLATION DU RÔLE");
            Sound("amongus", SoundSlot.Rotate, "rotate_sus", "SUS");
            Sound("amongus", SoundSlot.Lock, "lock_kill", "KILL");
            Sound("amongus", SoundSlot.Line, "line_button", "BOUTON D'URGENCE");
            Sound("amongus", SoundSlot.Line, "line_drip", "DRIP");
            Sound("amongus", SoundSlot.Tetris, "tetris_amogus", "AMOGUS (SATURÉ)");
            Sound("amongus", SoundSlot.Tetris, "tetris_meeting", "RÉUNION D'URGENCE");
            Sound("amongus", SoundSlot.GameOver, "gameover_impostor", "L'IMPOSTEUR");
            Sound("amongus", SoundSlot.GameOver, "gameover_impostorwin", "VICTOIRE DE L'IMPOSTEUR");
            Sound("pokemon", SoundSlot.Spawn, "spawn_whosthat", "QUEL EST CE POKÉMON ?");
            Sound("pokemon", SoundSlot.Spawn, "spawn_ichooseyou", "JE TE CHOISIS");
            Sound("pokemon", SoundSlot.Rotate, "rotate_pikapika", "PIKA PIKA");
            Sound("pokemon", SoundSlot.Rotate, "rotate_psyduck", "PSYKOKWAK");
            Sound("pokemon", SoundSlot.Lock, "lock_pikachu", "C'EST PIKACHU");
            Sound("pokemon", SoundSlot.Lock, "lock_pokeball", "POKÉBALL");
            Sound("pokemon", SoundSlot.Line, "line_levelup", "NIVEAU SUPÉRIEUR");
            Sound("pokemon", SoundSlot.Line, "line_jigglypuff", "RONDOUDOU");
            Sound("pokemon", SoundSlot.Tetris, "tetris_center", "CENTRE POKÉMON");
            Sound("pokemon", SoundSlot.GameOver, "gameover_teamrocket", "LA TEAM ROCKET S'ENVOLE");
            Sound("pokemon", SoundSlot.GameOver, "gameover_lowhp", "PV BAS");
            Sound("fnaf", SoundSlot.Spawn, "spawn_hello", "HELLO HELLO");
            Sound("fnaf", SoundSlot.Move, "move_foxyrun", "FOXY COURT");
            Sound("fnaf", SoundSlot.Rotate, "rotate_honk", "HONK");
            Sound("fnaf", SoundSlot.Lock, "lock_nose", "NEZ DE FREDDY");
            Sound("fnaf", SoundSlot.Lock, "lock_door", "PORTE BLINDÉE");
            Sound("fnaf", SoundSlot.Line, "line_laugh", "RIRE DE FREDDY");
            Sound("fnaf", SoundSlot.Line, "line_goldenlaugh", "RIRE DE GOLDEN FREDDY");
            Sound("fnaf", SoundSlot.Tetris, "tetris_sixam", "6 HEURES DU MATIN");
            Sound("fnaf", SoundSlot.Tetris, "tetris_yay", "YAAAY");
            Sound("fnaf", SoundSlot.GameOver, "gameover_scream", "JUMPSCARE");
            Sound("fnaf", SoundSlot.GameOver, "gameover_foxy", "JUMPSCARE DE FOXY");
            Sound("politique", SoundSlot.Spawn, "spawn_rue", "JE TRAVERSE LA RUE");
            Sound("politique", SoundSlot.Rotate, "rotate_hein", "HEIN ?");
            Sound("politique", SoundSlot.Rotate, "rotate_benvoyons", "BEN VOYONS");
            Sound("politique", SoundSlot.Lock, "lock_tousse", "ZEMMOUR TOUSSE");
            Sound("politique", SoundSlot.Lock, "lock_pommes", "J'AIME LES POMMES");
            Sound("politique", SoundSlot.Line, "line_forsure", "FOR SURE");
            Sound("politique", SoundSlot.Line, "line_perlimpinpin", "POUDRE DE PERLIMPINPIN");
            Sound("politique", SoundSlot.Line, "line_lassalle", "SALUT C'EST LASSALLE");
            Sound("politique", SoundSlot.Tetris, "tetris_projet", "C'EST NOTRE PROJET");
            Sound("politique", SoundSlot.Tetris, "tetris_republique", "LA RÉPUBLIQUE C'EST MOI");
            Sound("politique", SoundSlot.Tetris, "tetris_ohmygod", "OH MY GOD");
            Sound("politique", SoundSlot.GameOver, "gameover_predire", "QUI AURAIT PU PRÉDIRE");
            Sound("politique", SoundSlot.GameOver, "gameover_indignite", "QUELLE INDIGNITÉ");
            Sound("politique", SoundSlot.GameOver, "gameover_cassetoi", "CASSE-TOI");
            Sound("minecraft", SoundSlot.Spawn, "spawn_teleport", "TÉLÉPORTATION");
            Sound("minecraft", SoundSlot.Rotate, "rotate_villager", "HMM");
            Sound("minecraft", SoundSlot.Lock, "lock_oof", "OOF");
            Sound("minecraft", SoundSlot.Lock, "lock_eat", "MIAM");
            Sound("minecraft", SoundSlot.Line, "line_orb", "ORBE D'XP");
            Sound("minecraft", SoundSlot.Tetris, "tetris_levelup", "NIVEAU SUPÉRIEUR");
            Sound("minecraft", SoundSlot.GameOver, "gameover_creeper", "CREEPER");
            Sound("minecraft", SoundSlot.GameOver, "gameover_ghast", "GHAST");
            Sound("windows", SoundSlot.Spawn, "spawn_ding", "DING");
            Sound("windows", SoundSlot.Rotate, "rotate_usb", "USB BRANCHÉ");
            Sound("windows", SoundSlot.Lock, "lock_error", "ERREUR");
            Sound("windows", SoundSlot.Lock, "lock_unplug", "USB DÉBRANCHÉ");
            Sound("windows", SoundSlot.Line, "line_tada", "TADAAA");
            Sound("windows", SoundSlot.Tetris, "tetris_startup", "DÉMARRAGE");
            Sound("windows", SoundSlot.GameOver, "gameover_critical", "ERREUR CRITIQUE");
            Sound("windows", SoundSlot.GameOver, "gameover_shutdown", "ARRÊT");
            Sound("memes", SoundSlot.Spawn, "spawn_tacobell", "TACO BELL");
            Sound("memes", SoundSlot.Move, "move_dryfart", "PROUT SEC");
            Sound("memes", SoundSlot.Rotate, "rotate_fart", "PROUT PARFAIT");
            Sound("memes", SoundSlot.Lock, "lock_vineboom", "VINE BOOM");
            Sound("memes", SoundSlot.Lock, "lock_pipe", "TUYAU EN MÉTAL");
            Sound("memes", SoundSlot.Line, "line_wow", "ANIME WOW");
            Sound("memes", SoundSlot.Line, "line_rizz", "RIZZ");
            Sound("memes", SoundSlot.Line, "line_airhorn", "AIRHORN");
            Sound("memes", SoundSlot.Tetris, "tetris_yeahboi", "YEAH BOIII");
            Sound("memes", SoundSlot.Tetris, "tetris_fbi", "FBI OPEN UP");
            Sound("memes", SoundSlot.GameOver, "gameover_bruh", "BRUH");
            Sound("memes", SoundSlot.GameOver, "gameover_damage", "EMOTIONAL DAMAGE");
            Sound("memes", SoundSlot.GameOver, "gameover_continued", "TO BE CONTINUED");
            Sound("memes", SoundSlot.GameOver, "gameover_violin", "VIOLON TRISTE");
            Sound("memes", SoundSlot.GameOver, "gameover_ouinan", "OUI NAN NAAAAN");

            Sound("descente", SoundSlot.SoftDrop, "softdrop_ouioui", "GÉNÉRIQUE OUI-OUI (PAULOK)");
            Sound("descente", SoundSlot.SoftDrop, "softdrop_yakety", "YAKETY SAX");
            Sound("descente", SoundSlot.SoftDrop, "softdrop_dejavu", "DÉJÀ VU");
            Sound("descente", SoundSlot.SoftDrop, "softdrop_gas", "GAS GAS GAS");
            Sound("descente", SoundSlot.SoftDrop, "softdrop_ninety", "RUNNING IN THE 90S");
            Sound("descente", SoundSlot.SoftDrop, "softdrop_tokyo", "TOKYO DRIFT");
            Sound("descente", SoundSlot.SoftDrop, "softdrop_mariostar", "ÉTOILE DE MARIO");
            Sound("descente", SoundSlot.SoftDrop, "softdrop_sonic", "SONIC SE NOIE");
            Sound("descente", SoundSlot.SoftDrop, "softdrop_mission", "MISSION IMPOSSIBLE");
            Sound("descente", SoundSlot.SoftDrop, "softdrop_jeopardy", "JEOPARDY");
            Sound("descente", SoundSlot.SoftDrop, "softdrop_curb", "CURB YOUR ENTHUSIASM");
            Sound("fnaf", SoundSlot.SoftDrop, "softdrop_musicbox", "BOÎTE À MUSIQUE");
            Sound("windows", SoundSlot.SoftDrop, "softdrop_errorsong", "LA CHANSON DES ERREURS");
            Sound("amongus", SoundSlot.SoftDrop, "softdrop_drip", "DRIP");
            Sound("memes", SoundSlot.SoftDrop, "softdrop_bennyhill", "BENNY HILL");
        }

        /// <summary>Un pack de la boutique : il donne tous les sons dont <see cref="LockerItem.PackId"/> est son identifiant.</summary>
        static void Pack(string folder, string name, Rarity rarity, string tagline, string description) {
            LockerItem item = Shop(LockerCategory.Sounds, $"pack.{folder}", name, rarity, description);
            item.IsPack = true;
            item.Tagline = tagline;
        }

        /// <summary>Un son d'un pack, pour un moment de la partie. Fichier : Resources/Locker/Sfx/pack/fichier.ogg.</summary>
        static void Sound(string pack, SoundSlot slot, string file, string name) {
            LockerItem packItem = _items.Find(item => item.Id == $"pack.{pack}");
            LockerItem item = Shop(LockerCategory.Sounds, $"sfx.{pack}.{file}", name, packItem.Rarity, $"Du pack {packItem.Name}.");
            item.Slot = slot;
            item.PackId = packItem.Id;
            item.Resource = $"Locker/Sfx/{pack}/{file}";
            item.Tagline = name;
            packItem.Contents.Add(item);
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
