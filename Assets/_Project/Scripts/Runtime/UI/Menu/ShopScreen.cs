using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// La boutique, façon boutique Fortnite : objets en vedette (légendaires et mythiques), puis une section par catégorie.
    /// On paie en compliments : un mot par compliment, pris dans <see cref="ComplimentDictionary"/>, et chacun ne sert qu'une fois.
    /// La fenêtre d'achat remplit un panier de compliments jusqu'au prix de l'objet.
    /// Le livre des compliments liste ceux déjà dépensés, avec une recherche.
    /// Joue la musique du Wii Shop Channel tant qu'elle est ouverte.
    /// </summary>
    public class ShopScreen : MenuScreen {
        const string HiddenClass = "modal--hidden";

        static readonly string[] AcceptedMessages = {
            "« {0} » ! Merci, ça fait plaisir.", "« {0} »... je rougis.", "« {0} », validé par le comité des compliments.",
            "Oh, « {0} ». Trop aimable.", "« {0} » ! Tu sais parler aux boutiques, toi.",
        };
        static readonly string[] UnknownMessages = {
            "« {0} » ? C'est pas un compliment, ça.", "Le dictionnaire des compliments ne connaît pas « {0} ».",
            "« {0} »... essaie quelque chose de plus gentil.", "Refusé. « {0} » n'a ému personne.",
        };
        static readonly string[] SpentMessages = {
            "« {0} », tu l'as déjà dépensé. Sois créatif.", "Déjà utilisé ! Ouvre le livre si tu doutes.",
            "« {0} » ? Réchauffé. Trouve autre chose.",
        };

        readonly ScrollView _content;
        readonly Label _wallet;
        readonly Dictionary<LockerItem, Button> _cards = new();
        readonly List<(string word, string key)> _basket = new();
        IVisualElementScheduledItem _clock;

        VisualElement _dialog;
        VisualElement _dialogCardSlot;
        Label _dialogRarity;
        Label _dialogName;
        Label _dialogDescription;
        Label _dialogPrice;
        ScrollView _soundList;
        VisualElement _chips;
        TextField _input;
        Label _feedback;
        Button _listenButton;
        Button _buyButton;
        Button _equipButton;
        Button _closeDialogButton;
        LockerItem _selected;

        VisualElement _book;
        Label _bookCount;
        TextField _search;
        ScrollView _bookList;

        public ShopScreen(VisualElement root, MenuAudio audio, Action openLocker) : base(root, audio, "BOUTIQUE") {
            Root.AddToClassList("shop");

            _wallet = ItemCards.CreateLabel("", "shop__wallet");
            Root.Q(className: "screen__titles").Add(_wallet);

            AddHeaderButton("LIVRE DES COMPLIMENTS", OpenBook, "screen__button--book");
            AddHeaderButton("CASIER", openLocker);
            AddHeaderButton("RETOUR", RequestClose);

            _content = new ScrollView(ScrollViewMode.Vertical) { verticalScrollerVisibility = ScrollerVisibility.Hidden };
            _content.AddToClassList("shop__content");
            Root.Add(_content);

            CreateDialog();
            CreateBook();
        }

        public override Focusable FirstFocus {
            get {
                foreach (Button card in _cards.Values) return card;
                return null;
            }
        }

        public override void Open() {
            Audio.SetShopOpen(true);
            BuildContent();
            RefreshWallet();
            UpdateClock();
            _clock ??= Root.schedule.Execute(UpdateClock).Every(1000);
            _clock.Resume();
        }

        public override void Close() {
            base.Close();
            Audio.SetShopOpen(false);
            _clock?.Pause();
            CloseDialog();
            CloseBook();
        }

        public override bool HandleCancel() {
            if (!_dialog.ClassListContains(HiddenClass)) {
                CloseDialog();
                return true;
            }
            if (!_book.ClassListContains(HiddenClass)) {
                CloseBook();
                return true;
            }
            return false;
        }

        // ---------- Vitrine ----------

        void BuildContent() {
            _content.Clear();
            _cards.Clear();

            var featured = new List<LockerItem>();
            foreach (LockerItem item in LockerCatalog.Items) {
                if (item.IsSellable && item.Rarity >= Rarity.Legendary) featured.Add(item);
            }
            featured.Sort((a, b) => b.Rarity.CompareTo(a.Rarity));
            AddSection("EN VEDETTE", featured, true);

            (LockerCategory category, string title)[] sections = {
                (LockerCategory.Sounds, "PACKS DE SONS"), (LockerCategory.Music, "MUSIQUES"), (LockerCategory.Blocks, "BLOCS"),
                (LockerCategory.Board, "PLATEAUX"), (LockerCategory.Background, "FONDS"), (LockerCategory.Cursor, "CURSEURS"),
            };
            foreach ((LockerCategory category, string title) in sections) {
                var items = new List<LockerItem>();
                foreach (LockerItem item in LockerCatalog.InCategory(category)) {
                    if (item.IsSellable && item.Rarity < Rarity.Legendary) items.Add(item);
                }
                items.Sort((a, b) => b.Rarity.CompareTo(a.Rarity));
                AddSection(title, items, false);
            }
        }

        void AddSection(string title, List<LockerItem> items, bool isBig) {
            if (items.Count == 0) return;

            _content.Add(ItemCards.CreateLabel(title, isBig ? "shop__section shop__section--featured" : "shop__section"));
            var grid = new VisualElement();
            grid.AddToClassList("item-grid");
            foreach (LockerItem item in items) {
                Button card = ItemCards.CreateCard(item, isBig);
                card.clicked += () => OpenDialog(item);
                grid.Add(card);
                _cards[item] = card;
                RefreshCard(item);
            }
            _content.Add(grid);
        }

        void RefreshCard(LockerItem item) {
            if (!_cards.TryGetValue(item, out Button card)) return;

            if (PlayerInventory.Owns(item)) ItemCards.SetFooter(card, "POSSÉDÉ", "item-card__footer--owned");
            else ItemCards.SetFooter(card, ItemCards.PriceText(item.Price), "item-card__footer--price");
        }

        void RefreshWallet() {
            int spent = PlayerInventory.Spent.Count;
            _wallet.text = $"COMPLIMENTS DÉCOUVERTS : {spent} / {ComplimentDictionary.Count}";
        }

        /// <summary>Faux compte à rebours jusqu'à minuit, comme la vraie. La boutique, elle, ne change jamais.</summary>
        void UpdateClock() {
            TimeSpan left = DateTime.Today.AddDays(1) - DateTime.Now;
            Subtitle.text = $"NOUVELLE BOUTIQUE DANS {left.Hours:00}:{left.Minutes:00}:{left.Seconds:00}  (ELLE NE CHANGE JAMAIS)";
        }

        // ---------- Fenêtre d'achat ----------

        void CreateDialog() {
            _dialog = CreateModal("shop-dialog");
            var panel = new VisualElement();
            panel.AddToClassList("glass");
            panel.AddToClassList("shop-dialog__panel");
            _dialog.Add(panel);

            _dialogCardSlot = new VisualElement();
            _dialogCardSlot.AddToClassList("shop-dialog__card");
            panel.Add(_dialogCardSlot);

            var details = new VisualElement();
            details.AddToClassList("shop-dialog__details");
            panel.Add(details);

            _dialogRarity = ItemCards.CreateLabel("", "shop-dialog__rarity");
            _dialogName = ItemCards.CreateLabel("", "shop-dialog__name");
            _dialogDescription = ItemCards.CreateLabel("", "shop-dialog__description");
            _dialogPrice = ItemCards.CreateLabel("", "shop-dialog__price");
            _soundList = new ScrollView(ScrollViewMode.Vertical) { verticalScrollerVisibility = ScrollerVisibility.Auto };
            _soundList.AddToClassList("shop-dialog__sounds");
            _chips = new VisualElement();
            _chips.AddToClassList("shop-dialog__chips");

            _input = new TextField { maxLength = 30 };
            _input.AddToClassList("shop-dialog__input");
            _input.textEdition.placeholder = "Écris un compliment, puis Entrée";
            _input.textEdition.hidePlaceholderOnFocus = false;
            _input.RegisterCallback<KeyDownEvent>(OnInputKey, TrickleDown.TrickleDown);

            _feedback = ItemCards.CreateLabel("", "shop-dialog__feedback");

            var buttons = new VisualElement();
            buttons.AddToClassList("shop-dialog__buttons");
            _listenButton = DialogButton(buttons, "ÉCOUTER", () => LockerScreen.Preview(Audio, _selected));
            _buyButton = DialogButton(buttons, "ACHETER", Buy, "shop-dialog__button--buy");
            _equipButton = DialogButton(buttons, "ÉQUIPER", EquipSelected, "shop-dialog__button--buy");
            _closeDialogButton = DialogButton(buttons, "ANNULER", CloseDialog);

            details.Add(_dialogRarity);
            details.Add(_dialogName);
            details.Add(_dialogDescription);
            details.Add(_soundList);
            details.Add(_dialogPrice);
            details.Add(_chips);
            details.Add(_input);
            details.Add(_feedback);
            details.Add(buttons);
        }

        void OpenDialog(LockerItem item) {
            _selected = item;
            _basket.Clear();

            _dialogCardSlot.Clear();
            Button card = ItemCards.CreateCard(item, true);
            card.pickingMode = PickingMode.Ignore;
            card.focusable = false;
            _dialogCardSlot.Add(card);

            Color rarity = ItemCards.RarityColor(item.Rarity);
            _dialogRarity.text = $"{ItemCards.CategoryName(item.Category)}  ·  {ItemCards.RarityName(item.Rarity)}";
            _dialogRarity.style.color = rarity;
            _dialogName.text = item.Name;
            _dialogDescription.text = string.IsNullOrEmpty(item.Artist) ? item.Description : $"{item.Artist}. {item.Description}";
            _listenButton.style.display = item.Category is LockerCategory.Music ? DisplayStyle.Flex : DisplayStyle.None;
            FillSoundList(item);

            bool isOwned = PlayerInventory.Owns(item);
            ShowOwnedState(isOwned, isOwned ? "Déjà dans ton Casier." : "");
            if (!isOwned) RefreshBasket();

            Show(_dialog, true);
            _dialog.schedule.Execute(() => (isOwned ? (Focusable)_equipButton : _input).Focus()).StartingIn(30);
        }

        /// <summary>Pack : la liste de ses sons, chacun avec son bouton d'écoute.</summary>
        void FillSoundList(LockerItem item) {
            _soundList.Clear();
            _soundList.style.display = item.IsPack ? DisplayStyle.Flex : DisplayStyle.None;
            if (!item.IsPack) return;

            foreach (LockerItem sound in item.Contents) {
                var row = new VisualElement();
                row.AddToClassList("shop-dialog__sound");
                var play = new Button(() => Audio.PreviewSound(LockerContent.GetSoundClip(sound)));
                play.AddToClassList("shop-dialog__play");
                play.Add(CreatePlayIcon());
                row.Add(play);
                row.Add(ItemCards.CreateLabel(sound.Name, "shop-dialog__sound-name"));
                row.Add(ItemCards.CreateLabel(LockerContent.SlotName(sound.Slot), "shop-dialog__sound-slot"));
                _soundList.Add(row);
            }
        }

        void ShowOwnedState(bool isOwned, string message) {
            _dialogPrice.text = isOwned ? "DÉJÀ À TOI" : $"PRIX : {ItemCards.PriceText(_selected.Price)}";
            _chips.style.display = isOwned ? DisplayStyle.None : DisplayStyle.Flex;
            _input.style.display = isOwned ? DisplayStyle.None : DisplayStyle.Flex;
            _buyButton.style.display = isOwned ? DisplayStyle.None : DisplayStyle.Flex;
            _equipButton.style.display = isOwned ? DisplayStyle.Flex : DisplayStyle.None;
            _equipButton.text = _selected.IsPack ? "TOUT ÉQUIPER" : "ÉQUIPER";
            _closeDialogButton.text = isOwned ? "FERMER" : "ANNULER";
            SetFeedback(message, isOwned ? "shop-dialog__feedback--good" : null);
        }

        void CloseDialog() {
            if (_dialog == null || _dialog.ClassListContains(HiddenClass)) return;

            Audio.StopPreview();
            Show(_dialog, false);
            // Pas de focus rendu à la carte : elle resterait figée dans son état survolé.
            Root.focusController?.focusedElement?.Blur();
        }

        void OnInputKey(KeyDownEvent e) {
            if (e.keyCode != KeyCode.Return && e.keyCode != KeyCode.KeypadEnter) return;

            e.StopPropagation();
            if (string.IsNullOrWhiteSpace(_input.value) && _basket.Count >= _selected.Price) {
                Buy();
                return;
            }
            SubmitWord(_input.value);
            _input.value = "";
            _input.schedule.Execute(() => _input.Focus()).StartingIn(10);
        }

        void SubmitWord(string word) {
            if (_basket.Count >= _selected.Price) {
                SetFeedback("Le panier est plein ! Appuie sur ACHETER.", null);
                return;
            }

            var basketKeys = new List<string>();
            foreach ((string _, string key) in _basket) basketKeys.Add(key);

            string trimmed = word.Trim();
            switch (ComplimentDictionary.Check(trimmed, basketKeys, out string complimentKey)) {
                case ComplimentCheck.Accepted:
                    _basket.Add((trimmed, complimentKey));
                    UiSounds.Play(UiSound.Accept);
                    SetFeedback(Pick(AcceptedMessages, trimmed), "shop-dialog__feedback--good");
                    RefreshBasket();
                    break;
                case ComplimentCheck.SeveralWords:
                    Refuse("Un seul mot à la fois, Shakespeare.");
                    break;
                case ComplimentCheck.Unknown:
                    Refuse(Pick(UnknownMessages, trimmed));
                    break;
                case ComplimentCheck.AlreadySpent:
                    Refuse(Pick(SpentMessages, trimmed));
                    break;
                case ComplimentCheck.InBasket:
                    Refuse($"« {trimmed} » est déjà dans le panier. On ne paie pas deux fois avec le même.");
                    break;
            }
        }

        void Refuse(string message) {
            UiSounds.Play(UiSound.Wrong, 0.7f);
            SetFeedback(message, "shop-dialog__feedback--bad");
            Shake(_input);
        }

        /// <summary>Une case par compliment du prix : remplie par les mots acceptés (cliquer une case la vide).</summary>
        void RefreshBasket() {
            _chips.Clear();
            for (int i = 0; i < _selected.Price; i++) {
                bool isFilled = i < _basket.Count;
                Label chip = ItemCards.CreateLabel(isFilled ? _basket[i].word.ToUpperInvariant() : "?", "shop-dialog__chip");
                chip.EnableInClassList("shop-dialog__chip--filled", isFilled);
                if (isFilled) {
                    int index = i;
                    chip.pickingMode = PickingMode.Position;
                    chip.RegisterCallback<ClickEvent>(_ => {
                        _basket.RemoveAt(index);
                        RefreshBasket();
                    });
                }
                _chips.Add(chip);
            }

            bool isFull = _basket.Count >= _selected.Price;
            _buyButton.SetEnabled(isFull);
            _buyButton.text = isFull ? "ACHETER" : $"ACHETER  ({_basket.Count}/{_selected.Price})";
        }

        void Buy() {
            if (_selected == null || PlayerInventory.Owns(_selected) || _basket.Count < _selected.Price) return;

            PlayerInventory.Purchase(_selected, _basket);
            UiSounds.Play(UiSound.Buy);
            RefreshCard(_selected);
            RefreshWallet();
            ShowOwnedState(true, "ACHETÉ ! Merci pour tous ces compliments. Il t'attend dans le Casier.");
            _equipButton.Focus();
        }

        void EquipSelected() {
            PlayerInventory.Equip(_selected);
            UiSounds.Play(UiSound.Equip);
            SetFeedback(_selected.IsPack
                ? "Un son de chaque moment est équipé. Change-les un par un dans le Casier."
                : "Équipé ! Lance une partie pour en profiter.", "shop-dialog__feedback--good");
        }

        void SetFeedback(string message, string modifier) {
            _feedback.text = message;
            _feedback.RemoveFromClassList("shop-dialog__feedback--good");
            _feedback.RemoveFromClassList("shop-dialog__feedback--bad");
            if (modifier != null) _feedback.AddToClassList(modifier);
        }

        // ---------- Livre des compliments ----------

        void CreateBook() {
            _book = CreateModal("book");
            var page = new VisualElement();
            page.AddToClassList("book__page");
            _book.Add(page);

            page.Add(ItemCards.CreateLabel("LE LIVRE DES COMPLIMENTS", "book__title"));
            _bookCount = ItemCards.CreateLabel("", "book__count");
            page.Add(_bookCount);

            _search = new TextField { maxLength = 30 };
            _search.AddToClassList("book__search");
            _search.textEdition.placeholder = "Rechercher un compliment...";
            _search.textEdition.hidePlaceholderOnFocus = false;
            _search.RegisterValueChangedCallback(_ => RefreshBook());
            page.Add(_search);

            _bookList = new ScrollView(ScrollViewMode.Vertical) { verticalScrollerVisibility = ScrollerVisibility.Auto };
            _bookList.AddToClassList("book__list");
            page.Add(_bookList);

            var close = new Button(CloseBook) { text = "REFERMER LE LIVRE" };
            close.AddToClassList("book__close");
            page.Add(close);
        }

        void OpenBook() {
            _search.value = "";
            RefreshBook();
            Show(_book, true);
            _book.schedule.Execute(() => _search.Focus()).StartingIn(30);
        }

        void CloseBook() {
            if (_book == null || _book.ClassListContains(HiddenClass)) return;
            Show(_book, false);
        }

        void RefreshBook() {
            IReadOnlyList<SpentCompliment> spent = PlayerInventory.Spent;
            _bookCount.text = $"{spent.Count} / {ComplimentDictionary.Count} COMPLIMENTS DÉCOUVERTS";
            _bookList.Clear();

            if (spent.Count == 0) {
                _bookList.Add(ItemCards.CreateLabel("Le livre est vide. Va faire des compliments à la boutique.", "book__empty"));
                return;
            }

            string search = ComplimentDictionary.Simplify(_search.value ?? "");
            int shown = 0;
            for (int i = spent.Count - 1; i >= 0; i--) {
                SpentCompliment compliment = spent[i];
                if (search.Length > 0 && !ComplimentDictionary.Simplify(compliment.word).Contains(search) && !compliment.key.Contains(search)) continue;

                LockerItem item = LockerCatalog.Get(compliment.itemId);
                var row = new VisualElement();
                row.AddToClassList("book__row");
                row.Add(ItemCards.CreateLabel(compliment.word.ToUpperInvariant(), "book__word"));
                row.Add(ItemCards.CreateLabel(item != null ? $"→ {item.Name}" : "", "book__item"));
                row.Add(ItemCards.CreateLabel(compliment.date, "book__date"));
                _bookList.Add(row);
                shown++;
            }

            if (shown == 0) {
                _bookList.Add(ItemCards.CreateLabel($"Aucun « {_search.value} » dans le livre. Il est encore à dépenser !", "book__empty"));
            }
        }

        // ---------- Outils ----------

        VisualElement CreateModal(string className) {
            var modal = new VisualElement();
            modal.AddToClassList("modal");
            modal.AddToClassList(HiddenClass);
            modal.AddToClassList(className);
            modal.style.display = DisplayStyle.None;
            Root.Add(modal);
            return modal;
        }

        static void Show(VisualElement modal, bool isVisible) {
            if (isVisible) {
                modal.style.display = DisplayStyle.Flex;
                modal.schedule.Execute(() => modal.RemoveFromClassList(HiddenClass)).StartingIn(16);
            }
            else {
                modal.AddToClassList(HiddenClass);
                modal.style.display = DisplayStyle.None;
            }
        }

        /// <summary>Triangle "lecture" dessiné en code (la police n'a pas le caractère ▶).</summary>
        static VisualElement CreatePlayIcon() {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("shop-dialog__play-icon");
            icon.generateVisualContent += context => {
                Rect rect = icon.contentRect;
                Painter2D painter = context.painter2D;
                painter.fillColor = Color.white;
                painter.BeginPath();
                painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
                painter.LineTo(new Vector2(rect.xMax, rect.center.y));
                painter.LineTo(new Vector2(rect.xMin, rect.yMax));
                painter.ClosePath();
                painter.Fill();
            };
            return icon;
        }

        static Button DialogButton(VisualElement parent, string text, Action onClick, string modifier = null) {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("glass");
            button.AddToClassList("shop-dialog__button");
            if (modifier != null) button.AddToClassList(modifier);
            parent.Add(button);
            return button;
        }

        static string Pick(string[] messages, string word) {
            return string.Format(messages[UnityEngine.Random.Range(0, messages.Length)], word);
        }

        static void Shake(VisualElement element) {
            float[] offsets = { -12f, 12f, -8f, 8f, 0f };
            for (int i = 0; i < offsets.Length; i++) {
                float offset = offsets[i];
                element.schedule.Execute(() => element.style.translate = new Translate(offset, 0)).StartingIn(i * 45);
            }
        }
    }
}
