using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tetris {
    /// <summary>
    /// Aperçu en direct du Casier : un mini-plateau aux couleurs du plateau, des blocs et du fond équipés,
    /// sur lequel un petit bot joue tout seul. En simulation (<see cref="SetSimulating"/>), la partie devient sonore :
    /// sons équipés à chaque moment (apparition, déplacement, rotation, pose, ligne, Tetris, game over),
    /// boucle de descente pendant les descentes rapides, et musique de la playlist.
    /// Le bot laisse tomber la plupart des pièces à vitesse normale et n'accélère que de temps en temps.
    /// Après une vingtaine de pièces, le bot panique et pose n'importe comment, pour entendre aussi le game over.
    /// </summary>
    public class LockerPreview {
        const int Width = 10;
        const int Height = 20;
        const float CellSize = 28f;
        const int LimitRows = 3;
        const int PiecesBeforePanic = 22;

        enum Phase { Spawn, Rotate, Move, Drop, Dead }

        /// <summary>Formes des pièces dans l'ordre I, J, L, O, S, T, Z (comme <see cref="LockerCatalog.PieceOrder"/>).</summary>
        static readonly Vector2Int[][] Shapes = {
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
            new[] { new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) },
            new[] { new Vector2Int(1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) },
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },
            new[] { new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0) },
        };
        const int PieceI = 0;
        const int PieceO = 3;

        readonly MenuAudio _audio;
        readonly VisualElement _stage;
        readonly VisualElement _background;
        readonly VisualElement _board;
        readonly VisualElement _limitLine;
        readonly VisualElement[] _cells = new VisualElement[Width * Height];
        readonly Color[] _shown = new Color[Width * Height];
        readonly Label _status;
        readonly List<VisualElement> _blobs = new();
        readonly int[,] _grid = new int[Width, Height];
        readonly AudioClip[] _sounds = new AudioClip[System.Enum.GetValues(typeof(SoundSlot)).Length];
        IVisualElementScheduledItem _tick;

        LockerItem _skin;
        LockerItem _boardSkin;
        LockerItem _backgroundItem;
        Texture2D _cellTexture;
        Color _slotColor;

        Phase _phase;
        float _nextStep;
        int _piece;
        int _rotation;
        int _x;
        int _y;
        int _targetRotation;
        int _targetX;
        int _placed;
        int _score;
        int _lines;
        bool _isPanicking;
        bool _isFastDrop;
        bool _isSimulating;

        public bool IsSimulating => _isSimulating;

        public LockerPreview(VisualElement stage, Label status, MenuAudio audio) {
            _stage = stage;
            _status = status;
            _audio = audio;

            _background = new VisualElement { pickingMode = PickingMode.Ignore };
            _background.AddToClassList("preview__background");
            _stage.Add(_background);

            _board = new VisualElement { pickingMode = PickingMode.Ignore };
            _board.AddToClassList("preview__board");
            _board.style.width = Width * CellSize + 12f;
            _board.style.height = Height * CellSize + 12f;
            _stage.Add(_board);

            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    var cell = new VisualElement { pickingMode = PickingMode.Ignore };
                    cell.AddToClassList("preview__cell");
                    cell.style.left = 6f + x * CellSize;
                    cell.style.top = 6f + (Height - 1 - y) * CellSize;
                    cell.style.width = CellSize - 2f;
                    cell.style.height = CellSize - 2f;
                    _board.Add(cell);
                    _cells[y * Width + x] = cell;
                }
            }

            _limitLine = new VisualElement { pickingMode = PickingMode.Ignore };
            _limitLine.AddToClassList("preview__limit");
            _limitLine.style.top = 6f + LimitRows * CellSize - 1f;
            _board.Add(_limitLine);

            Reset();
        }

        public void Start() {
            Refresh();
            _tick ??= _stage.schedule.Execute(Tick).Every(16);
            _tick.Resume();
        }

        public void Stop() {
            SetSimulating(false);
            _tick?.Pause();
        }

        /// <summary>Simulation sonore (sons équipés et musique) ou aperçu muet.</summary>
        public void SetSimulating(bool isSimulating) {
            _isSimulating = isSimulating;
            if (isSimulating) {
                Refresh();
                IReadOnlyList<LockerItem> playlist = PlayerInventory.Playlist;
                LockerItem track = playlist[PlayerInventory.Shuffle ? Random.Range(0, playlist.Count) : 0];
                _audio.SetSimulationMusic(LockerContent.GetMusicClip(track));
                _score = 0;
                _lines = 0;
            }
            else {
                _audio.StopSimulation();
            }
            UpdateStatus();
        }

        /// <summary>Reprend le skin, le plateau, le fond et les sons équipés (à appeler après chaque changement).</summary>
        public void Refresh() {
            _skin = PlayerInventory.GetEquipped(LockerCategory.Blocks);
            _boardSkin = PlayerInventory.GetEquipped(LockerCategory.Board);
            _backgroundItem = PlayerInventory.GetEquipped(LockerCategory.Background);
            _cellTexture = LockerContent.GetCellTexture(_skin.CellStyle);

            Color fill = _boardSkin.HasBoardColors ? _boardSkin.BoardFill : new Color(0.04f, 0.05f, 0.12f, 0.55f);
            Color outline = _boardSkin.HasBoardColors ? _boardSkin.BoardOutline : new Color(1f, 1f, 1f, 0.35f);
            _slotColor = _boardSkin.HasBoardColors ? _boardSkin.BoardSlot : new Color(1f, 1f, 1f, 0.04f);
            _board.style.backgroundColor = fill;
            _board.style.borderTopColor = _board.style.borderBottomColor = _board.style.borderLeftColor = _board.style.borderRightColor = outline;
            _limitLine.style.backgroundColor = _boardSkin.HasBoardColors ? _boardSkin.BoardLimit : new Color(1f, 1f, 1f, 0.45f);

            foreach (SoundSlot slot in System.Enum.GetValues(typeof(SoundSlot))) {
                _sounds[(int)slot] = LockerContent.GetSoundClip(PlayerInventory.GetEquippedSound(slot));
            }

            BuildBackground();
            for (int i = 0; i < _shown.Length; i++) _shown[i] = new Color(-1f, 0f, 0f, 0f);
            Render();
        }

        // ---------- Fond ----------

        void BuildBackground() {
            foreach (VisualElement blob in _blobs) blob.RemoveFromHierarchy();
            _blobs.Clear();
            _background.style.backgroundImage = StyleKeyword.None;
            _background.style.backgroundColor = new Color(0.06f, 0.05f, 0.16f);

            switch (_backgroundItem.BackgroundStyle) {
                case BackgroundStyle.Void:
                    _background.style.backgroundColor = Color.black;
                    return;
                case BackgroundStyle.Cover: {
                    Texture2D cover = LockerContent.GetCover(PlayerInventory.Playlist[0]);
                    if (cover != null) _background.style.backgroundImage = Background.FromTexture2D(cover);
                    _background.style.unityBackgroundImageTintColor = new Color(0.45f, 0.45f, 0.45f);
                    return;
                }
            }

            Color[] colors = _backgroundItem.Colors ?? new[] {
                new Color(0.49f, 0.36f, 1f, 0.55f), new Color(0.13f, 0.83f, 0.93f, 0.4f), new Color(1f, 0.31f, 0.85f, 0.4f),
                new Color(0.23f, 0.51f, 0.96f, 0.5f), new Color(0.55f, 0.2f, 0.95f, 0.45f),
            };
            foreach (Color color in colors) {
                var blob = new VisualElement { pickingMode = PickingMode.Ignore, userData = color };
                blob.AddToClassList("preview__blob");
                blob.style.backgroundColor = color;
                _background.Add(blob);
                _blobs.Add(blob);
            }
        }

        void AnimateBackground(float now) {
            Rect area = _background.contentRect;
            if (area.width <= 0f) return;

            bool isDisco = _backgroundItem.BackgroundStyle == BackgroundStyle.Disco;
            for (int i = 0; i < _blobs.Count; i++) {
                float seed = i * 17.3f + 3.1f;
                float t = now * (isDisco ? 0.2f : 0.06f);
                _blobs[i].style.left = Mathf.PerlinNoise(seed, t) * area.width - 140f;
                _blobs[i].style.top = Mathf.PerlinNoise(t, seed) * area.height - 140f;
                if (isDisco) {
                    var color = (Color)_blobs[i].userData;
                    Color disco = Color.HSVToRGB(Mathf.Repeat(now * 0.15f + i * 0.2f, 1f), 0.8f, 1f);
                    disco.a = color.a;
                    _blobs[i].style.backgroundColor = disco;
                }
            }
        }

        // ---------- Partie simulée ----------

        void Tick() {
            float now = Time.unscaledTime;
            AnimateBackground(now);
            if (now < _nextStep) return;

            switch (_phase) {
                case Phase.Spawn: SpawnPiece(now); break;
                case Phase.Rotate: StepRotate(now); break;
                case Phase.Move: StepMove(now); break;
                case Phase.Drop: StepDrop(now); break;
                case Phase.Dead: Reset(); break;
            }
            Render();
        }

        void SpawnPiece(float now) {
            _piece = Random.Range(0, Shapes.Length);
            _rotation = 0;
            _x = Width / 2 - 1;
            _y = Height - 2;

            if (!Fits(_piece, _rotation, _x, _y, _grid)) {
                GameOver(now);
                return;
            }

            ChooseTarget();
            Play(SoundSlot.Spawn);
            _phase = Phase.Rotate;
            _nextStep = now + 0.2f;
        }

        void StepRotate(float now) {
            if (_rotation == _targetRotation || !Fits(_piece, (_rotation + 1) % 4, _x, _y, _grid)) {
                _phase = Phase.Move;
                return;
            }
            _rotation = (_rotation + 1) % 4;
            Play(SoundSlot.Rotate);
            _nextStep = now + 0.12f;
        }

        void StepMove(float now) {
            int direction = System.Math.Sign(_targetX - _x);
            if (direction == 0 || !Fits(_piece, _rotation, _x + direction, _y, _grid)) {
                _phase = Phase.Drop;
                _isFastDrop = Random.value < 0.35f;
                if (_isFastDrop) SetLoop(true);
                _nextStep = now + 0.08f;
                return;
            }
            _x += direction;
            Play(SoundSlot.Move);
            _nextStep = now + 0.09f;
        }

        void StepDrop(float now) {
            if (Fits(_piece, _rotation, _x, _y - 1, _grid)) {
                _y--;
                _nextStep = now + (_isFastDrop ? 0.045f : 0.16f);
                return;
            }

            SetLoop(false);
            Place(_piece, _rotation, _x, _y, _grid);
            Play(SoundSlot.Lock);

            int cleared = ClearLines(_grid);
            if (cleared > 0) {
                Play(cleared >= 4 && _sounds[(int)SoundSlot.Tetris] != null ? SoundSlot.Tetris : SoundSlot.Line);
                _lines += cleared;
                _score += new[] { 0, 100, 300, 500, 800 }[Mathf.Min(cleared, 4)];
            }

            _placed++;
            _isPanicking = _placed >= PiecesBeforePanic;
            if (HighestRow(_grid) >= Height - LimitRows) {
                GameOver(now);
                return;
            }

            _phase = Phase.Spawn;
            _nextStep = now + 0.3f;
            UpdateStatus();
        }

        void GameOver(float now) {
            SetLoop(false);
            Play(SoundSlot.GameOver);
            _phase = Phase.Dead;
            _nextStep = now + 2.5f;
            if (_status != null && _isSimulating) _status.text = "GAME OVER  ·  LE BOT A RAGE QUIT";
        }

        /// <summary>Plateau vidé, avec trois lignes de déchets (le puits de droite reste libre pour les Tetris).</summary>
        void Reset() {
            System.Array.Clear(_grid, 0, _grid.Length);
            for (int y = 0; y < 3; y++) {
                int hole = Random.Range(0, Width - 1);
                for (int x = 0; x < Width - 1; x++) {
                    if (x != hole) _grid[x, y] = Random.Range(1, Shapes.Length + 1);
                }
            }
            _placed = 0;
            _isPanicking = false;
            _phase = Phase.Spawn;
            _nextStep = Time.unscaledTime + 0.5f;
            UpdateStatus();
        }

        void UpdateStatus() {
            if (_status == null) return;
            _status.text = _isSimulating ? $"SIMULATION  ·  SCORE {_score}  ·  {_lines} LIGNES" : "APERÇU EN DIRECT";
        }

        void Play(SoundSlot slot) {
            if (_isSimulating) _audio.PlayEffect(_sounds[(int)slot]);
        }

        void SetLoop(bool isOn) {
            if (_isSimulating) _audio.SetLoop(isOn ? _sounds[(int)SoundSlot.SoftDrop] : null);
        }

        // ---------- Petit bot ----------

        /// <summary>
        /// Choisit la rotation et la colonne : la meilleure place selon la hauteur, les trous et les lignes,
        /// en gardant la colonne de droite libre pour les Tetris. En panique : au hasard.
        /// </summary>
        void ChooseTarget() {
            float best = float.NegativeInfinity;
            _targetRotation = 0;
            _targetX = _x;
            var copy = new int[Width, Height];

            for (int rotation = 0; rotation < (_piece == PieceO ? 1 : 4); rotation++) {
                for (int x = -2; x < Width + 2; x++) {
                    if (!Fits(_piece, rotation, x, Height - 2, _grid)) continue;

                    int y = Height - 2;
                    while (Fits(_piece, rotation, x, y - 1, _grid)) y--;

                    float score;
                    if (_isPanicking) {
                        score = Random.value;
                    }
                    else {
                        System.Array.Copy(_grid, copy, _grid.Length);
                        Place(_piece, rotation, x, y, copy);
                        score = Evaluate(copy, ClearLines(copy), UsesWell(_piece, rotation, x));
                    }

                    if (score > best) {
                        best = score;
                        _targetRotation = rotation;
                        _targetX = x;
                    }
                }
            }
        }

        bool UsesWell(int piece, int rotation, int x) {
            bool isVerticalI = piece == PieceI && rotation % 2 == 1;
            if (isVerticalI) return false;
            foreach (Vector2Int cell in Cells(piece, rotation)) {
                if (x + cell.x == Width - 1) return true;
            }
            return false;
        }

        static float Evaluate(int[,] grid, int lines, bool usesWell) {
            int aggregate = 0;
            int holes = 0;
            int bumpiness = 0;
            int previous = -1;
            for (int x = 0; x < Width; x++) {
                int height = 0;
                for (int y = Height - 1; y >= 0; y--) {
                    if (grid[x, y] != 0) {
                        if (height == 0) height = y + 1;
                    }
                    else if (height > 0) {
                        holes++;
                    }
                }
                aggregate += height;
                if (previous >= 0) bumpiness += Mathf.Abs(height - previous);
                previous = height;
            }
            float lineScore = lines >= 4 ? 12f : lines * 0.76f;
            return -0.51f * aggregate + lineScore - 0.36f * holes - 0.18f * bumpiness - (usesWell ? 3f : 0f);
        }

        // ---------- Grille ----------

        static IEnumerable<Vector2Int> Cells(int piece, int rotation) {
            foreach (Vector2Int cell in Shapes[piece]) {
                Vector2Int rotated = cell;
                for (int r = 0; r < rotation; r++) rotated = new Vector2Int(rotated.y, -rotated.x);
                yield return rotated;
            }
        }

        static bool Fits(int piece, int rotation, int x, int y, int[,] grid) {
            foreach (Vector2Int cell in Cells(piece, rotation)) {
                int cx = x + cell.x;
                int cy = y + cell.y;
                if (cx < 0 || cx >= Width || cy < 0) return false;
                if (cy < Height && grid[cx, cy] != 0) return false;
            }
            return true;
        }

        static void Place(int piece, int rotation, int x, int y, int[,] grid) {
            foreach (Vector2Int cell in Cells(piece, rotation)) {
                int cy = y + cell.y;
                if (cy < Height) grid[x + cell.x, cy] = piece + 1;
            }
        }

        static int ClearLines(int[,] grid) {
            int cleared = 0;
            for (int y = 0; y < Height; y++) {
                bool isFull = true;
                for (int x = 0; x < Width && isFull; x++) isFull = grid[x, y] != 0;
                if (!isFull) continue;

                for (int row = y; row < Height - 1; row++) {
                    for (int x = 0; x < Width; x++) grid[x, row] = grid[x, row + 1];
                }
                for (int x = 0; x < Width; x++) grid[x, Height - 1] = 0;
                cleared++;
                y--;
            }
            return cleared;
        }

        static int HighestRow(int[,] grid) {
            for (int y = Height - 1; y >= 0; y--) {
                for (int x = 0; x < Width; x++) {
                    if (grid[x, y] != 0) return y;
                }
            }
            return -1;
        }

        // ---------- Rendu ----------

        void Render() {
            var display = (int[,])_grid.Clone();
            var ghost = new bool[Width, Height];
            if (_phase is Phase.Rotate or Phase.Move or Phase.Drop) {
                int ghostY = _y;
                while (Fits(_piece, _rotation, _x, ghostY - 1, _grid)) ghostY--;
                foreach (Vector2Int cell in Cells(_piece, _rotation)) {
                    int gx = _x + cell.x;
                    int gy = ghostY + cell.y;
                    if (gy >= 0 && gy < Height) ghost[gx, gy] = true;
                }
                Place(_piece, _rotation, _x, _y, display);
            }

            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    int value = display[x, y];
                    Color color;
                    bool isPiece = value != 0 || ghost[x, y];
                    if (value != 0) {
                        color = PieceColor(value - 1);
                    }
                    else if (ghost[x, y]) {
                        color = PieceColor(_piece);
                        color.a *= Bloc.GhostAlpha;
                    }
                    else {
                        color = _slotColor;
                    }
                    SetCell(y * Width + x, color, isPiece);
                }
            }
        }

        Color PieceColor(int piece) {
            return _skin.Colors != null && piece < _skin.Colors.Length ? _skin.Colors[piece] : Color.white;
        }

        /// <summary>Ne touche au style que si la case a changé.</summary>
        void SetCell(int index, Color color, bool isPiece) {
            Color key = isPiece ? color : new Color(color.r, color.g, color.b, -1f - color.a);
            if (_shown[index] == key) return;
            _shown[index] = key;

            VisualElement cell = _cells[index];
            if (isPiece && _cellTexture != null) {
                cell.style.backgroundImage = Background.FromTexture2D(_cellTexture);
                cell.style.unityBackgroundImageTintColor = color;
                cell.style.backgroundColor = Color.clear;
            }
            else {
                cell.style.backgroundImage = StyleKeyword.None;
                cell.style.backgroundColor = color;
            }
        }
    }
}
