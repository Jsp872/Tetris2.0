using UnityEngine;

namespace Tetris {
    /// <summary>Options absurdes de la catégorie "Spoof Goof" du menu Options.</summary>
    public enum SpoofGoof {
        /// <summary>0 : aucune, 1 : verticale (HUD à l'envers, ligne de limite en bas : la pièce la traverse en tombant).</summary>
        Inversion,
        /// <summary>0 : normale, 1 : cassée (gauche et droite inversées).</summary>
        Compass,
        /// <summary>0 : lune, 1 : terre, 2 : jupiter (vitesse de chute).</summary>
        Gravity,
        /// <summary>0 : chipmunk, 1 : normale, 2 : démon (hauteur de tous les sons, musique comprise).</summary>
        Voice,
        /// <summary>0 : aucun, 1 : bateau, 2 : tempête (la caméra et le HUD tanguent).</summary>
        Seasickness,
        /// <summary>0 : non, 1 : oui (vague arc-en-ciel sur toutes les pièces).</summary>
        Disco,
        /// <summary>0 : sobres, 1 : pompettes, 2 : bourrées (la pièce titube, la pile reste de travers).</summary>
        Drunk,
        /// <summary>0 : éléphant, 1 : poisson rouge (les blocs posés s'effacent peu à peu).</summary>
        Goldfish,
        /// <summary>0 : correcte, 1 : déplorable (<see cref="DeplorableUi"/>).</summary>
        Interface,
    }

    /// <summary>
    /// Valeurs des options <see cref="SpoofGoof"/>, enregistrées sur la machine (PlayerPrefs).
    /// Chaque option est un index dans sa liste de choix ; <see cref="Default"/> donne le choix "normal".
    /// Les effets visuels sont appliqués en partie par <see cref="SpoofGoofEffects"/>.
    /// </summary>
    public static class SpoofGoofSettings {
        static readonly int[] Defaults = { 0, 0, 1, 1, 0, 0, 0, 0, 0 };
        static readonly int[] Values = (int[])Defaults.Clone();

        public static int Get(SpoofGoof option) => Values[(int)option];

        public static void Set(SpoofGoof option, int value) {
            Values[(int)option] = value;
            PlayerPrefs.SetInt(Key(option), value);
        }

        public static int Default(SpoofGoof option) => Defaults[(int)option];

        public static bool IsActive(SpoofGoof option) => Get(option) != Default(option);

        public static bool AnyActive {
            get {
                for (int i = 0; i < Values.Length; i++) {
                    if (Values[i] != Defaults[i]) return true;
                }
                return false;
            }
        }

        /// <summary>Remet toutes les options à leur valeur normale.</summary>
        public static void ResetAll() {
            for (int i = 0; i < Values.Length; i++) {
                Set((SpoofGoof)i, Defaults[i]);
            }
        }

        /// <summary>Les options qui changent le résultat d'une partie : pas de record tant qu'elles sont actives.</summary>
        public static bool DisablesRecords => IsActive(SpoofGoof.Inversion) || IsActive(SpoofGoof.Gravity);

        public static bool IsInverted => IsActive(SpoofGoof.Inversion);

        public static bool IsCompassBroken => IsActive(SpoofGoof.Compass);

        /// <summary>Multiplicateur du temps entre deux descentes (plus grand : chute plus lente).</summary>
        public static float FallIntervalFactor => Get(SpoofGoof.Gravity) switch {
            0 => 2.5f,
            2 => 0.4f,
            _ => 1f,
        };

        /// <summary>Hauteur de tous les sons (1 : normale).</summary>
        public static float Pitch => Get(SpoofGoof.Voice) switch {
            0 => 1.5f,
            2 => 0.65f,
            _ => 1f,
        };

        /// <summary>Angle maximal du tangage, en degrés.</summary>
        public static float SwayAngle => Get(SpoofGoof.Seasickness) switch {
            1 => 4f,
            2 => 14f,
            _ => 0f,
        };

        public static bool IsDisco => IsActive(SpoofGoof.Disco);

        /// <summary>Angle maximal du titubage des cases, en degrés.</summary>
        public static float DrunkAngle => Get(SpoofGoof.Drunk) switch {
            1 => 9f,
            2 => 24f,
            _ => 0f,
        };

        public static bool IsGoldfish => IsActive(SpoofGoof.Goldfish);

        public static bool IsDeplorable => IsActive(SpoofGoof.Interface);

        static string Key(SpoofGoof option) => $"SpoofGoof_{option}";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load() {
            for (int i = 0; i < Values.Length; i++) {
                Values[i] = PlayerPrefs.GetInt(Key((SpoofGoof)i), Defaults[i]);
            }
        }
    }
}
