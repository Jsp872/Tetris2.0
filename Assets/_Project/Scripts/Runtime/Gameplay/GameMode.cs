using System.Globalization;
using UnityEngine;

namespace Tetris {
    public enum GameMode {
        /// <summary>Survivre et marquer le plus de points. Fin au game over ou au niveau 15 (150 lignes). Classement au score.</summary>
        Marathon,
        /// <summary>Effacer 40 lignes le plus vite possible. Classement au temps.</summary>
        Sprint,
        /// <summary>Marquer un maximum de points en 2 minutes. Classement au score.</summary>
        Ultra,
    }

    /// <summary>Mode choisi dans le menu principal, lu par le GameManager au chargement de la scène de jeu.</summary>
    public static class GameSession {
        public static GameMode Mode = GameMode.Marathon;
    }

    /// <summary>
    /// Meilleurs résultats par mode, enregistrés sur la machine (PlayerPrefs).
    /// Marathon et Ultra : meilleur score (plus haut). Sprint : meilleur temps en secondes (plus bas).
    /// </summary>
    public static class ModeRecords {
        static string Key(GameMode mode) => $"Record_{mode}";

        public static bool LowerIsBetter(GameMode mode) => mode == GameMode.Sprint;

        public static bool TryGet(GameMode mode, out float value) {
            value = PlayerPrefs.GetFloat(Key(mode), -1f);
            return value >= 0f;
        }

        /// <summary>Enregistre <paramref name="value"/> s'il bat le record. Renvoie vrai si c'est un nouveau record.</summary>
        public static bool Submit(GameMode mode, float value) {
            bool isRecord = !TryGet(mode, out float best) || (LowerIsBetter(mode) ? value < best : value > best);
            if (isRecord) {
                PlayerPrefs.SetFloat(Key(mode), value);
                PlayerPrefs.Save();
            }
            return isRecord;
        }

        /// <summary>Efface les records de tous les modes.</summary>
        public static void ClearAll() {
            foreach (GameMode mode in (GameMode[])System.Enum.GetValues(typeof(GameMode))) {
                PlayerPrefs.DeleteKey(Key(mode));
            }
            PlayerPrefs.Save();
        }

        /// <summary>Texte du résultat : temps "1:02.37" pour le Sprint, score "12 400" sinon.</summary>
        public static string Format(GameMode mode, float value) {
            return mode == GameMode.Sprint ? FormatTime(value) : FormatScore((int)value);
        }

        public static string FormatTime(float seconds) {
            seconds = Mathf.Max(seconds, 0f);
            int minutes = (int)(seconds / 60f);
            float rest = seconds - minutes * 60f;
            return $"{minutes}:{rest.ToString("00.00", CultureInfo.InvariantCulture)}";
        }

        public static string FormatScore(int score) {
            return score.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");
        }
    }
}
