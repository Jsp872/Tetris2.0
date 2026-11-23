using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Données d'une pièce : sa couleur et sa forme.
    /// </summary>
    [CreateAssetMenu(fileName = "Bloc_", menuName = "Tetris/Bloc")]
    public class BlocSO : ScriptableObject {
        public Color color = Color.white;

        [Tooltip("Cases de la pièce, relatives à son pivot (0, 0). La pièce tourne autour de ce pivot. " +
                 "Garder y entre -1 et 1 pour que la pièce apparaisse en entier dans la grille.")]
        public Vector2Int[] shape;

        [Tooltip("Décocher pour les pièces qui ne doivent pas tourner (le carré).")]
        public bool canRotate = true;
    }
}
