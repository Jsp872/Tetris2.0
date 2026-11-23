using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Le Casier : un onglet par catégorie, et les objets possédés de cette catégorie.
    /// Cliquer un objet l'équipe (et joue un aperçu pour les musiques et les sons).
    /// Au départ, seuls les objets de base sont là ; une carte "+ boutique" mène aux autres.
    /// </summary>
    public class LockerScreen : MenuScreen {
        readonly Action _openShop;
        readonly VisualElement _tabs;
        readonly ScrollView _grid;
        readonly Dictionary<LockerCategory, Button> _tabButtons = new();
        LockerCategory _category;

        public LockerScreen(VisualElement root, MenuAudio audio, Action openShop) : base(root, audio, "CASIER") {
            _openShop = openShop;
            Subtitle.text = "Choisis ce que tu verras et entendras en jeu. N en partie pour changer de musique.";
            AddHeaderButton("BOUTIQUE", openShop, "screen__button--shop");
            AddHeaderButton("RETOUR", RequestClose);

            var body = new VisualElement();
            body.AddToClassList("locker__body");
            _tabs = new VisualElement();
            _tabs.AddToClassList("locker__tabs");
            body.Add(_tabs);

            _grid = new ScrollView(ScrollViewMode.Vertical) { verticalScrollerVisibility = ScrollerVisibility.Hidden };
            _grid.AddToClassList("locker__grid");
            _grid.contentContainer.AddToClassList("item-grid");
            body.Add(_grid);
            Root.Add(body);

            foreach (LockerCategory category in Enum.GetValues(typeof(LockerCategory))) {
                var tab = new Button(() => ShowCategory(category));
                tab.AddToClassList("glass");
                tab.AddToClassList("locker__tab");
                _tabs.Add(tab);
                _tabButtons[category] = tab;
            }
        }

        public override Focusable FirstFocus => _tabButtons[_category];

        public override void Open() {
            RefreshTabs();
            ShowCategory(_category);
        }

        void RefreshTabs() {
            foreach (KeyValuePair<LockerCategory, Button> pair in _tabButtons) {
                int owned = 0;
                int total = 0;
                foreach (LockerItem item in LockerCatalog.InCategory(pair.Key)) {
                    total++;
                    if (PlayerInventory.Owns(item)) owned++;
                }
                pair.Value.text = $"{ItemCards.CategoryName(pair.Key)}   {owned}/{total}";
                pair.Value.EnableInClassList("locker__tab--active", pair.Key == _category);
            }
        }

        void ShowCategory(LockerCategory category) {
            _category = category;
            RefreshTabs();
            _grid.Clear();
            _grid.scrollOffset = UnityEngine.Vector2.zero;

            int missing = 0;
            foreach (LockerItem item in LockerCatalog.InCategory(category)) {
                if (!PlayerInventory.Owns(item)) {
                    missing++;
                    continue;
                }

                Button card = ItemCards.CreateCard(item);
                card.clicked += () => OnItemClicked(item);
                _grid.Add(card);
            }

            if (missing > 0) {
                var shopCard = new Button(_openShop);
                shopCard.AddToClassList("item-card");
                shopCard.AddToClassList("item-card--shop");
                shopCard.Add(ItemCards.CreateLabel("+", "item-card__plus"));
                shopCard.Add(ItemCards.CreateLabel($"{missing} DE PLUS DANS LA BOUTIQUE", "item-card__name"));
                _grid.Add(shopCard);
            }

            RefreshFooters();
        }

        void RefreshFooters() {
            int index = 0;
            foreach (LockerItem item in LockerCatalog.InCategory(_category)) {
                if (!PlayerInventory.Owns(item)) continue;

                VisualElement card = _grid.contentContainer[index++];
                if (PlayerInventory.IsEquipped(item)) ItemCards.SetFooter(card, "ÉQUIPÉ", "item-card__footer--equipped");
                else ItemCards.SetFooter(card, "ÉQUIPER", "item-card__footer--owned");
            }
        }

        void OnItemClicked(LockerItem item) {
            PlayerInventory.Equip(item);
            UiSounds.Play(UiSound.Equip);
            RefreshFooters();
            Preview(Audio, item);
        }

        /// <summary>Aperçu sonore d'une musique ou d'un pack de sons.</summary>
        public static void Preview(MenuAudio audio, LockerItem item) {
            if (item.Category == LockerCategory.Music) {
                audio.PreviewMusic(LockerContent.GetMusicClip(item));
            }
            else if (item.Category == LockerCategory.Sounds) {
                SoundPack pack = LockerContent.GetSoundPack(item);
                audio.PreviewSound(pack != null ? pack.GetPreview() : null);
            }
        }
    }
}
