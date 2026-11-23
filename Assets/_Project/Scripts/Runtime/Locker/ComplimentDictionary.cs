using System.Collections.Generic;
using System.Text;

namespace Tetris {
    public enum ComplimentCheck {
        Accepted,
        Empty,
        /// <summary>Plusieurs mots : un compliment, c'est un seul mot.</summary>
        SeveralWords,
        /// <summary>Pas dans la liste des compliments acceptés.</summary>
        Unknown,
        /// <summary>Déjà dépensé dans un achat précédent.</summary>
        AlreadySpent,
        /// <summary>Déjà dans le panier de l'achat en cours.</summary>
        InBasket,
    }

    /// <summary>
    /// Liste des compliments acceptés comme monnaie par la boutique.
    /// La comparaison ignore la casse et les accents, et accepte le féminin et le pluriel
    /// ("brillante", "géniaux", "heureuses"...) : toutes ces formes ont la même clé, celle du mot de la liste.
    /// Un compliment dépensé l'est donc sous toutes ses formes.
    /// </summary>
    public static class ComplimentDictionary {
        const string Words = @"
admirable adorable affectueux agréable aimable altruiste ambitieux amical amusant angélique appliqué ardent
artistique astucieux attachant attentif attentionné audacieux authentique avenant badass bienveillant beau
belle bienfaisant blagueur bluffant bon bosseur brave brillant bouleversant calme canon captivant chaleureux
charismatique charmant chic chill chouette classe classy clairvoyant cocasse cohérent colossal combatif
comique compatissant compétent complice concentré confiant consciencieux cool coquet cordial cosmique
courageux courtois créatif culte cultivé curieux débrouillard décontracté délicat délicieux dévoué digne
dingue diplomate discret distingué divin doué doux douce drôle dynamique ébouriffant éblouissant éclatant
efficace élégant éloquent émouvant empathique enchanteur enchanteresse endurant énergique engagé enjoué
ensorcelant enthousiaste envoûtant épanoui épatant épique équilibré espiègle estimable étincelant étonnant
exceptionnel excellent exemplaire expressif exquis extraordinaire fabuleux fantastique faramineux fascinant
féerique fiable fidèle fier flamboyant fort fougueux formidable frais fraîche franc franche fringant fun futé
galactique galant généreux génial génialissime gentil glamour gracieux grandiose gourmand habile harmonieux
héroïque historique honnête honorable humble humain hilarant hypnotique iconique idéal illustre imaginatif
imbattable impeccable impérial impressionnant incomparable incroyable indestructible indulgent inégalable
infaillible infatigable ingénieux inimitable inoubliable inarrêtable inspirant intègre intelligent intrépide
intuitif invincible inventif irréprochable irrésistible jovial joyeux juste légendaire lucide loyal lumineux
magique magistral magnanime magnétique magnifique majestueux malin maligne marrant mémorable merveilleux
méritant méticuleux mignon minutieux mirifique mirobolant modeste monumental motivé motivant mythique naturel
noble observateur olympien optimiste original organisé ouf parfait passionnant passionné patient pédagogue
percutant perfectionniste performant persévérant perspicace pétillant pharaonique phénoménal photogénique
pimpant plaisant poétique poli ponctuel positif précieux prestigieux prévenant princier prodigieux productif
professionnel prometteur protecteur prudent pur radieux raffiné rassurant ravissant rayonnant réconfortant
redoutable réfléchi remarquable renversant reposant resplendissant respectueux responsable révolutionnaire
rigolo rigoureux robuste romantique royal rusé sage sain savant savoureux sculptural séduisant sensass
sensationnel sensible serein serviable sexy sidérant sincère smart sociable soigné solaire solide solidaire
somptueux souriant spirituel splendide spontané sportif stable stellaire stupéfiant stylé sublime subtil
succulent super superbe svelte swag sympa sympathique talentueux tenace tendre titanesque tolérant top
touchant tranquille travailleur unique utile vaillant valeureux vif vigilant visionnaire vivant volontaire zen
chanmé fou folle trop-fort ange amour artiste as beauté bijou bombe boss bonheur cador caïd champion chef
crack crème cœur colosse délice diamant étoile fée génie gentleman guerrier héros héroïne icône joyau légende
lumière maestro magicien magicienne maître maîtresse merveille modèle monument muse ninja orchidée pépite
perle phénomène phare pilier poète prince princesse prodige pro reine roi rockstar samouraï soleil star
superstar surdoué talent titan tornade trésor virtuose winner crème-de-la-crème masterclass banger goat chad
gigachad sigma based king queen slay boss-final emblématique épique-ultime enivrant exaltant fédérateur
frappant gigantesque grandissime haletant immense imposant inouï miraculeux notable palpitant phénix prodigue
puissant pétulant rocambolesque saisissant scintillant spectaculaire stratosphérique suprême surpuissant
transcendant triomphant ultime vertigineux vibrant voluptueux wow zénith astral aérien bucolique câlin
chevaleresque chouchou dégourdi délectable élancé émerveillant enflammé exubérant fervent fin finaud fleuri
florissant fortiche galvanisant goûteux gracile hardi hospitalier idyllique illuminé impavide impérissable
indomptable ineffable intarissable joli jolie juvénile lettré limpide mélodieux moelleux nickel novateur
onirique opulent pacifique paisible perfectionné philosophe pittoresque pragmatique preux primesautier racé
rafraîchissant rêveur rieur sémillant sérieux soyeux stoïque studieux sûr tonique vaporeux velouté vertueux
vivifiant zélé";

        static Dictionary<string, string> _formToKey;
        static int _count;

        /// <summary>Nombre de compliments différents acceptés.</summary>
        public static int Count {
            get {
                EnsureBuilt();
                return _count;
            }
        }

        /// <summary>Vérifie un mot saisi. <paramref name="key"/> : sa clé si c'est un compliment.</summary>
        public static ComplimentCheck Check(string input, ICollection<string> basketKeys, out string key) {
            key = null;
            string trimmed = input?.Trim() ?? "";
            if (trimmed.Length == 0) return ComplimentCheck.Empty;
            if (trimmed.IndexOf(' ') >= 0 || trimmed.IndexOf('\t') >= 0) return ComplimentCheck.SeveralWords;

            if (!TryGetKey(trimmed, out key)) return ComplimentCheck.Unknown;
            if (basketKeys != null && basketKeys.Contains(key)) return ComplimentCheck.InBasket;
            if (PlayerInventory.IsSpent(key)) return ComplimentCheck.AlreadySpent;
            return ComplimentCheck.Accepted;
        }

        public static bool TryGetKey(string word, out string key) {
            EnsureBuilt();
            return _formToKey.TryGetValue(Simplify(word), out key);
        }

        /// <summary>Minuscules, sans accents ni ponctuation : "Génial !" devient "genial".</summary>
        public static string Simplify(string text) {
            var builder = new StringBuilder(text.Length);
            foreach (char c in text.ToLowerInvariant()) {
                switch (c) {
                    case 'à': case 'â': case 'ä': case 'á': case 'ã': case 'å': builder.Append('a'); break;
                    case 'ç': builder.Append('c'); break;
                    case 'é': case 'è': case 'ê': case 'ë': builder.Append('e'); break;
                    case 'î': case 'ï': case 'í': case 'ì': builder.Append('i'); break;
                    case 'ô': case 'ö': case 'ó': case 'ò': case 'õ': builder.Append('o'); break;
                    case 'û': case 'ü': case 'ú': case 'ù': builder.Append('u'); break;
                    case 'ÿ': case 'ý': builder.Append('y'); break;
                    case 'ñ': builder.Append('n'); break;
                    case 'œ': builder.Append("oe"); break;
                    case 'æ': builder.Append("ae"); break;
                    default:
                        if (c >= 'a' && c <= 'z') builder.Append(c);
                        break;
                }
            }
            return builder.ToString();
        }

        static void EnsureBuilt() {
            if (_formToKey != null) return;

            _formToKey = new Dictionary<string, string>();
            var keys = new HashSet<string>();
            foreach (string word in Words.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries)) {
                string key = Simplify(word);
                if (key.Length == 0 || !keys.Add(key)) continue;

                foreach (string form in Inflect(key)) {
                    _formToKey.TryAdd(form, key);
                }
            }
            _count = keys.Count;
        }

        /// <summary>Formes féminines et plurielles probables d'un mot (sans accents). Les formes en trop ne gênent pas.</summary>
        static IEnumerable<string> Inflect(string word) {
            yield return word;
            yield return word + "s";

            if (word.EndsWith("x")) {
                string stem = word.Substring(0, word.Length - 1);
                yield return stem + "se";
                yield return stem + "ses";
                yield break;
            }
            if (word.EndsWith("eau")) {
                yield return word + "x";
                yield break;
            }
            if (word.EndsWith("e")) yield break;

            yield return word + "e";
            yield return word + "es";

            string[,] endings = {
                { "teur", "trice" }, { "eur", "euse" }, { "if", "ive" }, { "el", "elle" }, { "en", "enne" },
                { "on", "onne" }, { "et", "ette" }, { "il", "ille" }, { "er", "ere" },
            };
            for (int i = 0; i < endings.GetLength(0); i++) {
                string masculine = endings[i, 0];
                if (!word.EndsWith(masculine)) continue;

                string feminine = word.Substring(0, word.Length - masculine.Length) + endings[i, 1];
                yield return feminine;
                yield return feminine + "s";
            }
            if (word.EndsWith("al")) {
                yield return word.Substring(0, word.Length - 2) + "aux";
            }
        }
    }
}
