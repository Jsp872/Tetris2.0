using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Tetris {
    /// <summary>Un compliment dépensé dans la boutique, pour le livre des compliments.</summary>
    [Serializable]
    public class SpentCompliment {
        /// <summary>Le mot tel que le joueur l'a écrit.</summary>
        public string word;
        /// <summary>Clé du compliment (<see cref="ComplimentDictionary.TryGetKey"/>).</summary>
        public string key;
        public string itemId;
        public string date;
    }

    /// <summary>
    /// Ce que possède le joueur : objets achetés, objet équipé par catégorie, compliments dépensés.
    /// Enregistré dans un fichier JSON (casier.json dans <see cref="Application.persistentDataPath"/>).
    /// </summary>
    public static class PlayerInventory {
        const string FileName = "casier.json";

        [Serializable]
        class SaveData {
            public List<string> owned = new();
            public string[] equipped;
            public string[] sounds;
            public List<SpentCompliment> spent = new();
        }

        static SaveData _data;
        static HashSet<string> _spentKeys;

        /// <summary>Un objet a été acheté ou équipé.</summary>
        public static event Action Changed;

        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        static SaveData Data {
            get {
                if (_data == null) Load();
                return _data;
            }
        }

        public static IReadOnlyList<SpentCompliment> Spent => Data.spent;

        /// <summary>Vrai pour les objets de base, les objets achetés et les sons des packs achetés.</summary>
        public static bool Owns(LockerItem item) {
            return item != null && (item.IsFree || Data.owned.Contains(item.Id) || (item.PackId != null && Data.owned.Contains(item.PackId)));
        }

        /// <summary>Objet équipé dans la catégorie (l'objet de base si rien n'est équipé).</summary>
        public static LockerItem GetEquipped(LockerCategory category) {
            LockerItem item = LockerCatalog.Get(Data.equipped[(int)category]);
            return item != null && item.Category == category && Owns(item) ? item : LockerCatalog.Default(category);
        }

        /// <summary>Son équipé pour ce moment de la partie (le son de base si rien n'est équipé).</summary>
        public static LockerItem GetEquippedSound(SoundSlot slot) {
            LockerItem item = LockerCatalog.Get(Data.sounds[(int)slot]);
            return item != null && item.Category == LockerCategory.Sounds && item.Slot == slot && !item.IsPack && Owns(item)
                ? item
                : LockerCatalog.DefaultSound(slot);
        }

        public static bool IsEquipped(LockerItem item) {
            if (item.Category == LockerCategory.Sounds) return !item.IsPack && GetEquippedSound(item.Slot) == item;
            return GetEquipped(item.Category) == item;
        }

        /// <summary>Équipe l'objet. Un son s'équipe sur son moment ; un pack équipe son premier son de chaque moment.</summary>
        public static void Equip(LockerItem item) {
            if (!Owns(item)) return;

            if (item.IsPack) {
                var done = new HashSet<SoundSlot>();
                foreach (LockerItem sound in item.Contents) {
                    if (done.Add(sound.Slot)) Data.sounds[(int)sound.Slot] = sound.Id;
                }
            }
            else if (item.Category == LockerCategory.Sounds) {
                Data.sounds[(int)item.Slot] = item.Id;
            }
            else {
                Data.equipped[(int)item.Category] = item.Id;
            }
            Save();
            Changed?.Invoke();
        }

        public static bool IsSpent(string key) {
            if (_spentKeys == null) {
                _spentKeys = new HashSet<string>();
                foreach (SpentCompliment compliment in Data.spent) _spentKeys.Add(compliment.key);
            }
            return _spentKeys.Contains(key);
        }

        /// <summary>Ajoute l'objet au Casier et marque les compliments (mot écrit, clé) comme dépensés.</summary>
        public static void Purchase(LockerItem item, IReadOnlyList<(string word, string key)> compliments) {
            if (Owns(item)) return;

            string date = DateTime.Now.ToString("dd/MM/yyyy");
            foreach ((string word, string key) in compliments) {
                Data.spent.Add(new SpentCompliment { word = word, key = key, itemId = item.Id, date = date });
                IsSpent(key);
                _spentKeys.Add(key);
            }
            Data.owned.Add(item.Id);
            Save();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() {
            _data = null;
            _spentKeys = null;
            Changed = null;
        }

        static void Load() {
            try {
                if (File.Exists(FilePath)) {
                    _data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                }
            }
            catch (Exception exception) {
                Debug.LogWarning($"Casier illisible, on repart de zéro : {exception.Message}");
            }

            _data ??= new SaveData();
            _data.owned ??= new List<string>();
            _data.spent ??= new List<SpentCompliment>();

            _data.equipped = Resize(_data.equipped, Enum.GetValues(typeof(LockerCategory)).Length);
            _data.sounds = Resize(_data.sounds, Enum.GetValues(typeof(SoundSlot)).Length);
        }

        static string[] Resize(string[] values, int length) {
            if (values != null && values.Length == length) return values;

            var result = new string[length];
            if (values != null) Array.Copy(values, result, Mathf.Min(values.Length, length));
            return result;
        }

        static void Save() {
            try {
                File.WriteAllText(FilePath, JsonUtility.ToJson(_data, true));
            }
            catch (Exception exception) {
                Debug.LogWarning($"Impossible d'enregistrer le Casier : {exception.Message}");
            }
        }
    }
}
