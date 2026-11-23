using System;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Écran plein écran du menu principal (Casier, Boutique) : en-tête avec titre et boutons, puis un contenu.
    /// <see cref="MainMenu"/> l'ouvre, le ferme et lui transmet Échap (<see cref="HandleCancel"/>).
    /// </summary>
    public abstract class MenuScreen {
        protected readonly VisualElement Root;

        /// <summary>Conteneur de l'écran dans le menu.</summary>
        public VisualElement Element => Root;
        protected readonly VisualElement HeaderButtons;
        protected readonly Label Subtitle;
        protected readonly MenuAudio Audio;

        /// <summary>Le joueur demande à revenir au menu principal.</summary>
        public event Action CloseRequested;

        protected MenuScreen(VisualElement root, MenuAudio audio, string title) {
            Root = root;
            Audio = audio;
            Root.Clear();

            var header = new VisualElement();
            header.AddToClassList("screen__header");
            var titles = new VisualElement();
            titles.AddToClassList("screen__titles");
            titles.Add(ItemCards.CreateLabel(title, "screen__title"));
            Subtitle = ItemCards.CreateLabel("", "screen__subtitle");
            titles.Add(Subtitle);
            header.Add(titles);

            HeaderButtons = new VisualElement();
            HeaderButtons.AddToClassList("screen__buttons");
            header.Add(HeaderButtons);
            Root.Add(header);
        }

        public virtual void Open() { }

        public virtual void Close() {
            Audio.StopPreview();
        }

        /// <summary>Échap : ferme une fenêtre interne si besoin. Faux : l'écran entier doit se fermer.</summary>
        public virtual bool HandleCancel() => false;

        /// <summary>Premier élément à sélectionner à l'ouverture.</summary>
        public abstract Focusable FirstFocus { get; }

        protected void RequestClose() => CloseRequested?.Invoke();

        protected Button AddHeaderButton(string text, Action onClick, string modifier = null) {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("glass");
            button.AddToClassList("screen__button");
            if (modifier != null) button.AddToClassList(modifier);
            HeaderButtons.Add(button);
            return button;
        }
    }
}
