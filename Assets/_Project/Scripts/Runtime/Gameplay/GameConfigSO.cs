using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Réglages d'une partie : vitesse de chute, points par lignes et règles des modes.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Tetris/Game Config")]
    public class GameConfigSO : ScriptableObject {
        [Header("Chute")]
        [Tooltip("Temps entre deux descentes au niveau 1, en secondes.")]
        public float fallInterval = 0.8f;
        [Tooltip("Temps entre deux descentes pendant la descente rapide, en secondes.")]
        public float softDropInterval = 0.05f;
        [Tooltip("Multiplicateur du temps de chute à chaque niveau (Marathon). 0.85 = 15 % plus rapide par niveau.")]
        public float levelSpeedFactor = 0.85f;
        public float minFallInterval = 0.05f;

        [Header("Score")]
        [Tooltip("Points pour 0, 1, 2, 3 et 4 lignes effacées d'un coup (multipliés par le niveau en Marathon).")]
        public int[] lineScores = { 0, 100, 300, 500, 800 };

        [Header("Modes")]
        [Tooltip("Compte à rebours avant le début de la partie, en secondes.")]
        public int countdown = 3;
        public int linesPerLevel = 10;
        [Tooltip("Marathon : la partie se termine à ce niveau (15 = 150 lignes).")]
        public int marathonMaxLevel = 15;
        public int sprintLines = 40;
        [Tooltip("Ultra : durée de la partie, en secondes.")]
        public float ultraDuration = 120f;
    }
}
