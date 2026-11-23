using UnityEngine;

namespace Tetris {
    /// <summary>
    /// Options du joueur, enregistrées sur la machine (PlayerPrefs) et appliquées au lancement du jeu.
    /// Volumes de 0 à 1 : <see cref="MasterVolume"/> passe par l'AudioListener, les autres sont lus
    /// par chaque source de son (musique, bruitages, mèmes) au moment de jouer.
    /// Le mode plein écran et la résolution sont enregistrés par Unity lui-même.
    /// Les valeurs sont écrites dans PlayerPrefs à chaque changement ; <see cref="Save"/> les écrit sur le disque.
    /// </summary>
    public static class GameSettings {
        const string MasterKey = "Settings_MasterVolume";
        const string MusicKey = "Settings_MusicVolume";
        const string SfxKey = "Settings_SfxVolume";
        const string MemeKey = "Settings_MemeVolume";
        const string VSyncKey = "Settings_VSync";

        static float _masterVolume = 1f;
        static float _musicVolume = 1f;
        static float _sfxVolume = 1f;
        static float _memeVolume = 1f;
        static bool _vSync = true;

        public static float MasterVolume {
            get => _masterVolume;
            set {
                _masterVolume = SetVolume(MasterKey, value);
                AudioListener.volume = _masterVolume;
            }
        }

        public static float MusicVolume {
            get => _musicVolume;
            set => _musicVolume = SetVolume(MusicKey, value);
        }

        /// <summary>Bruitages de la partie (apparition et pose des pièces).</summary>
        public static float SfxVolume {
            get => _sfxVolume;
            set => _sfxVolume = SetVolume(SfxKey, value);
        }

        /// <summary>Sons des easter eggs (Plankton, Maxwell, Scuba Cat).</summary>
        public static float MemeVolume {
            get => _memeVolume;
            set => _memeVolume = SetVolume(MemeKey, value);
        }

        public static bool VSync {
            get => _vSync;
            set {
                _vSync = value;
                PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0);
                QualitySettings.vSyncCount = value ? 1 : 0;
            }
        }

        public static void Save() {
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadAndApply() {
            _masterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
            _musicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
            _sfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
            _memeVolume = PlayerPrefs.GetFloat(MemeKey, 1f);
            _vSync = PlayerPrefs.GetInt(VSyncKey, 1) == 1;

            AudioListener.volume = _masterVolume;
            QualitySettings.vSyncCount = _vSync ? 1 : 0;
        }

        static float SetVolume(string key, float value) {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(key, value);
            return value;
        }
    }
}
