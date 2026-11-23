using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Le Casier, façon casier Fortnite : à gauche l'aperçu en direct de la partie (<see cref="LockerPreview"/>)
    /// avec le bouton de simulation, à droite les emplacements équipés (apparence, playlist, un son par moment).
    /// Cliquer un emplacement ouvre le choix parmi les objets possédés ; l'aperçu suit chaque changement.
    /// Musique : on compose une playlist (clic pour ajouter ou retirer, dans l'ordre), avec lecture aléatoire en option.
    /// Au départ, seuls les objets de base sont là ; une carte "+ boutique" mène aux autres.
    /// </summary>
    public class LockerScreen : MenuScreen {
        readonly Action _openShop;
        readonly LockerPreview _preview;
        readonly Button _simulateButton;
        readonly VisualElement _right;
        readonly ScrollView _loadout;
        readonly VisualElement _picker;
        readonly Label _pickerTitle;
        readonly Label _pickerHint;
        readonly Button _shuffleButton;
        readonly ScrollView _pickerGrid;
        readonly List<(Button card, LockerItem item)> _cards = new();
        Button _firstTile;
        LockerCategory _pickerCategory;
        SoundSlot _pickerSlot;

        public LockerScreen(VisualElement root, MenuAudio audio, Action openShop) : base(root, audio, "CASIER") {
            _openShop = openShop;
            Subtitle.text = "Équipe ce que tu verras et entendras en jeu. Lance une simulation pour tout essayer.";
            AddHeaderButton("BOUTIQUE", openShop, "screen__button--shop");
            AddHeaderButton("RETOUR", RequestClose);

            var body = new VisualElement();
            body.AddToClassList("locker__body");
            Root.Add(body);

            // Aperçu
            var left = new VisualElement();
            left.AddToClassList("glass");
            left.AddToClassList("locker__preview");
            body.Add(left);
            var stage = new VisualElement();
            stage.AddToClassList("preview__stage");
            left.Add(stage);
            Label status = ItemCards.CreateLabel("", "preview__status");
            left.Add(status);
            _simulateButton = new Button(ToggleSimulation);
            _simulateButton.AddToClassList("glass");
            _simulateButton.AddToClassList("preview__simulate");
            left.Add(_simulateButton);
            _preview = new LockerPreview(stage, status, audio);

            // Emplacements
            _right = new VisualElement();
            _right.AddToClassList("locker__right");
            body.Add(_right);

            _loadout = new ScrollView(ScrollViewMode.Vertical) { verticalScrollerVisibility = ScrollerVisibility.Hidden };
            _loadout.AddToClassList("locker__loadout");
            _right.Add(_loadout);

            // Choix d'un objet
            _picker = new VisualElement();
            _picker.AddToClassList("locker__picker");
            var pickerHeader = new VisualElement();
            pickerHeader.AddToClassList("picker__header");
            var back = new Button(ClosePicker) { text = "RETOUR AUX EMPLACEMENTS" };
            back.AddToClassList("glass");
            back.AddToClassList("screen__button");
            pickerHeader.Add(back);
            _pickerTitle = ItemCards.CreateLabel("", "picker__title");
            pickerHeader.Add(_pickerTitle);
            _shuffleButton = new Button(() => {
                PlayerInventory.Shuffle = !PlayerInventory.Shuffle;
                RefreshPicker();
            });
            _shuffleButton.AddToClassList("glass");
            _shuffleButton.AddToClassList("screen__button");
            pickerHeader.Add(_shuffleButton);
            _picker.Add(pickerHeader);
            _pickerHint = ItemCards.CreateLabel("", "picker__hint");
            _picker.Add(_pickerHint);
            _pickerGrid = new ScrollView(ScrollViewMode.Vertical) { verticalScrollerVisibility = ScrollerVisibility.Hidden };
            _pickerGrid.AddToClassList("locker__grid");
            _pickerGrid.contentContainer.AddToClassList("item-grid");
            _picker.Add(_pickerGrid);
            _picker.style.display = DisplayStyle.None;
            _right.Add(_picker);
        }

        public override Focusable FirstFocus => _firstTile;

        public override void Open() {
            ClosePicker();
            _preview.Start();
            UpdateSimulateButton();
        }

        public override void Close() {
            base.Close();
            _preview.Stop();
            UpdateSimulateButton();
        }

        public override bool HandleCancel() {
            if (_picker.style.display == DisplayStyle.None) return false;
            ClosePicker();
            return true;
        }

        void ToggleSimulation() {
            _preview.SetSimulating(!_preview.IsSimulating);
            UpdateSimulateButton();
        }

        void UpdateSimulateButton() {
            _simulateButton.text = _preview.IsSimulating ? "ARRÊTER LA SIMULATION" : "SIMULER UNE PARTIE";
            _simulateButton.EnableInClassList("preview__simulate--on", _preview.IsSimulating);
        }

        // ---------- Emplacements ----------

        void BuildLoadout() {
            _loadout.Clear();
            _firstTile = null;

            AddLoadoutSection("APPARENCE");
            VisualElement looks = AddRow();
            AddTile(looks, "FOND", PlayerInventory.GetEquipped(LockerCategory.Background), () => OpenPicker(LockerCategory.Background));
            AddTile(looks, "BLOCS", PlayerInventory.GetEquipped(LockerCategory.Blocks), () => OpenPicker(LockerCategory.Blocks));
            AddTile(looks, "PLATEAU", PlayerInventory.GetEquipped(LockerCategory.Board), () => OpenPicker(LockerCategory.Board));
            AddTile(looks, "CURSEUR", PlayerInventory.GetEquipped(LockerCategory.Cursor), () => OpenPicker(LockerCategory.Cursor));

            IReadOnlyList<LockerItem> playlist = PlayerInventory.Playlist;
            string count = playlist.Count > 1 ? $"{playlist.Count} MUSIQUES" : playlist[0].Name;
            Button music = AddTile(looks, "PLAYLIST", playlist[0], () => OpenPicker(LockerCategory.Music));
            music.Q<Label>("tile-name").text = PlayerInventory.Shuffle ? $"{count}  ·  ALÉATOIRE" : count;

            AddLoadoutSection("SONS");
            VisualElement sounds = AddRow();
            foreach (SoundSlot slot in Enum.GetValues(typeof(SoundSlot))) {
                SoundSlot captured = slot;
                AddTile(sounds, LockerContent.SlotName(slot), PlayerInventory.GetEquippedSound(slot), () => OpenSoundPicker(captured));
            }
        }

        void AddLoadoutSection(string title) {
            _loadout.Add(ItemCards.CreateLabel(title, "shop__section"));
        }

        VisualElement AddRow() {
            var row = new VisualElement();
            row.AddToClassList("loadout__row");
            _loadout.Add(row);
            return row;
        }

        /// <summary>Case façon casier Fortnite : vignette de l'objet équipé, couleur de rareté, nom de l'emplacement.</summary>
        Button AddTile(VisualElement row, string slotName, LockerItem item, Action onClick) {
            Color rarity = item.IsFree ? new Color(0.55f, 0.6f, 0.75f) : ItemCards.RarityColor(item.Rarity);
            var tile = new Button(onClick);
            tile.AddToClassList("loadout-tile");
            tile.style.backgroundColor = Color.Lerp(rarity, Color.black, 0.5f);
            tile.style.borderTopColor = tile.style.borderBottomColor = tile.style.borderLeftColor = tile.style.borderRightColor = rarity;

            VisualElement thumbnail = ItemCards.CreateThumbnail(item);
            thumbnail.AddToClassList("loadout-tile__thumb");
            tile.Add(thumbnail);
            tile.Add(ItemCards.CreateLabel(slotName, "loadout-tile__slot"));
            Label name = ItemCards.CreateLabel(item.Name, "loadout-tile__name");
            name.name = "tile-name";
            tile.Add(name);

            row.Add(tile);
            _firstTile ??= tile;
            return tile;
        }

        // ---------- Choix d'un objet ----------

        void OpenPicker(LockerCategory category) {
            _pickerCategory = category;
            ShowPicker();
        }

        void OpenSoundPicker(SoundSlot slot) {
            _pickerCategory = LockerCategory.Sounds;
            _pickerSlot = slot;
            ShowPicker();
        }

        void ShowPicker() {
            _loadout.style.display = DisplayStyle.None;
            _picker.style.display = DisplayStyle.Flex;
            RefreshPicker();
            _pickerGrid.scrollOffset = Vector2.zero;
            if (_cards.Count > 0) _cards[0].card.Focus();
        }

        void ClosePicker() {
            _picker.style.display = DisplayStyle.None;
            _loadout.style.display = DisplayStyle.Flex;
            BuildLoadout();
            _preview.Refresh();
            _firstTile?.Focus();
        }

        void RefreshPicker() {
            bool isMusic = _pickerCategory == LockerCategory.Music;
            bool isSound = _pickerCategory == LockerCategory.Sounds;
            _pickerTitle.text = isSound ? $"SON · {LockerContent.SlotName(_pickerSlot)}" : isMusic ? "PLAYLIST" : ItemCards.CategoryName(_pickerCategory);
            _shuffleButton.style.display = isMusic ? DisplayStyle.Flex : DisplayStyle.None;
            _shuffleButton.text = PlayerInventory.Shuffle ? "LECTURE ALÉATOIRE : OUI" : "LECTURE ALÉATOIRE : NON";
            _pickerHint.text = isMusic
                ? "Clique une musique pour l'ajouter à la playlist ou l'en retirer. L'ordre d'ajout est l'ordre de lecture. En jeu : N et B, ou le carrousel."
                : isSound ? "Clique un son pour l'équiper et l'écouter." : "Clique un objet pour l'équiper. L'aperçu se met à jour.";

            _pickerGrid.Clear();
            _cards.Clear();
            int missing = 0;
            foreach (LockerItem item in LockerCatalog.InCategory(_pickerCategory)) {
                if (item.IsPack) {
                    if (isSound && !PlayerInventory.Owns(item) && ContainsSlot(item, _pickerSlot)) missing++;
                    continue;
                }
                if (isSound && item.Slot != _pickerSlot) continue;
                if (!PlayerInventory.Owns(item)) {
                    if (!isSound) missing++;
                    continue;
                }

                Button card = ItemCards.CreateCard(item);
                card.clicked += () => OnItemClicked(item);
                _pickerGrid.Add(card);
                _cards.Add((card, item));
            }

            if (missing > 0) {
                var shopCard = new Button(_openShop);
                shopCard.AddToClassList("item-card");
                shopCard.AddToClassList("item-card--shop");
                shopCard.Add(ItemCards.CreateLabel("+", "item-card__plus"));
                string what = isSound ? $"{missing} PACKS AVEC CE SON" : $"{missing} DE PLUS";
                shopCard.Add(ItemCards.CreateLabel($"{what} DANS LA BOUTIQUE", "item-card__name"));
                _pickerGrid.Add(shopCard);
            }

            RefreshFooters();
        }

        static bool ContainsSlot(LockerItem pack, SoundSlot slot) {
            foreach (LockerItem sound in pack.Contents) {
                if (sound.Slot == slot) return true;
            }
            return false;
        }

        void RefreshFooters() {
            foreach ((Button card, LockerItem item) in _cards) {
                if (item.Category == LockerCategory.Music) {
                    int index = PlayerInventory.PlaylistIndex(item);
                    if (index >= 0) ItemCards.SetFooter(card, $"N° {index + 1} DE LA PLAYLIST", "item-card__footer--equipped");
                    else ItemCards.SetFooter(card, "AJOUTER", "item-card__footer--owned");
                }
                else if (PlayerInventory.IsEquipped(item)) {
                    ItemCards.SetFooter(card, "ÉQUIPÉ", "item-card__footer--equipped");
                }
                else {
                    ItemCards.SetFooter(card, "ÉQUIPER", "item-card__footer--owned");
                }
            }
        }

        void OnItemClicked(LockerItem item) {
            if (item.Category == LockerCategory.Music) {
                PlayerInventory.TogglePlaylist(item);
            }
            else {
                PlayerInventory.Equip(item);
            }
            UiSounds.Play(UiSound.Equip);
            RefreshFooters();
            _preview.Refresh();
            if (!_preview.IsSimulating) Preview(Audio, item);
        }

        /// <summary>Aperçu sonore d'une musique ou d'un son.</summary>
        public static void Preview(MenuAudio audio, LockerItem item) {
            if (item.Category == LockerCategory.Music) {
                audio.PreviewMusic(LockerContent.GetMusicClip(item));
            }
            else if (item.Category == LockerCategory.Sounds) {
                audio.PreviewSound(LockerContent.GetSoundClip(item));
            }
        }
    }
}
