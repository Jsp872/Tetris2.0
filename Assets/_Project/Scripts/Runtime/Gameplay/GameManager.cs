using System.Collections;
using Tetris.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tetris {
    /// <summary>
    /// Déroulé d'une partie pour le mode choisi dans le menu (<see cref="GameSession.Mode"/>) :
    /// compte à rebours, gravité et inputs, score, fin de partie, record, revanche en un clic.
    /// <list type="bullet">
    /// <item>Marathon : un niveau toutes les 10 lignes (chute plus rapide, points x niveau), fin au game over ou à 150 lignes.</item>
    /// <item>Sprint : fin à 40 lignes, classement au temps.</item>
    /// <item>Ultra : fin après 2 minutes, classement au score.</item>
    /// </list>
    /// </summary>
    public class GameManager : MonoBehaviour {
        enum State { Countdown, Playing, Paused, Ended }

        const string MenuSceneName = "MainMenu";

        [SerializeField] Board _board;
        [SerializeField] Spawner _spawner;
        [SerializeField] HUD _hud;
        [SerializeField] GameConfigSO _config;

        Bloc _current;
        State _state;
        float _fallTimer;
        float _elapsed;
        bool _isSoftDropping;

        public GameMode Mode { get; private set; }
        public int Score { get; private set; }
        public int Lines { get; private set; }
        public int Level => Mode == GameMode.Marathon ? Mathf.Min(1 + Lines / _config.linesPerLevel, _config.marathonMaxLevel) : 1;
        public bool IsGameOver => _state == State.Ended;

        float FallInterval => Mathf.Max(_config.fallInterval * Mathf.Pow(_config.levelSpeedFactor, Level - 1), _config.minFallInterval);

        void OnEnable() {
            EventBus<MoveInputEvent>.Subscribe(OnMove);
            EventBus<DownInputEvent>.Subscribe(OnDown);
            EventBus<RotateInputEvent>.Subscribe(OnRotate);
            EventBus<PauseInputEvent>.Subscribe(OnPause);
            _hud.PrimaryClicked += OnPrimaryClicked;
            _hud.SecondaryClicked += GoToMenu;
        }

        void OnDisable() {
            EventBus<MoveInputEvent>.Unsubscribe(OnMove);
            EventBus<DownInputEvent>.Unsubscribe(OnDown);
            EventBus<RotateInputEvent>.Unsubscribe(OnRotate);
            EventBus<PauseInputEvent>.Unsubscribe(OnPause);
            _hud.PrimaryClicked -= OnPrimaryClicked;
            _hud.SecondaryClicked -= GoToMenu;
            Time.timeScale = 1f;
        }

        void Start() {
            Mode = GameSession.Mode;
            _hud.SetMode(Mode.ToString().ToUpperInvariant(), Mode == GameMode.Marathon ? "NIVEAU" : "TEMPS");
            RefreshHud();
            StartCoroutine(CountdownThenStart());
        }

        IEnumerator CountdownThenStart() {
            _state = State.Countdown;
            for (int i = _config.countdown; i > 0; i--) {
                _hud.ShowCountdown(i.ToString());
                yield return new WaitForSeconds(1f);
            }
            _hud.ShowCountdown("GO !");
            StartGame();
        }

        /// <summary>Lance la partie : première pièce et chronomètre.</summary>
        public void StartGame() {
            _state = State.Playing;
            _elapsed = 0f;
            SpawnNext();
        }

        void Update() {
            if (_state != State.Playing) return;

            _elapsed += Time.deltaTime;
            RefreshTimer();

            if (Mode == GameMode.Ultra && _elapsed >= _config.ultraDuration) {
                EndGame(false, "TEMPS ÉCOULÉ");
                return;
            }

            _fallTimer += Time.deltaTime;
            if (_fallTimer >= (_isSoftDropping ? _config.softDropInterval : FallInterval)) {
                _fallTimer = 0f;
                StepDown();
            }
        }

        /// <summary>Fait descendre la pièce d'une case, ou la pose si elle ne peut plus descendre.</summary>
        public void StepDown() {
            if (_current == null) return;

            if (_current.CanMove(Vector2Int.down)) {
                _current.Move(Vector2Int.down);
                return;
            }

            _current.Lock();
            _current = null;

            int cleared = _board.ClearFullRows();
            if (cleared > 0) {
                Score += _config.lineScores[Mathf.Min(cleared, _config.lineScores.Length - 1)] * Level;
                Lines += cleared;
                RefreshHud();
                EventBus<ScoreChangedEvent>.Publish(new ScoreChangedEvent(Score, Lines, cleared));
            }

            if (Mode == GameMode.Sprint && Lines >= _config.sprintLines) {
                EndGame(false, "SPRINT TERMINÉ");
                return;
            }
            if (Mode == GameMode.Marathon && Lines >= _config.marathonMaxLevel * _config.linesPerLevel) {
                EndGame(false, "MARATHON TERMINÉ");
                return;
            }

            SpawnNext();
        }

        void SpawnNext() {
            _current = _spawner.SpawnNext();
            _fallTimer = 0f;

            if (!_current.CanMove(Vector2Int.zero)) {
                Destroy(_current.gameObject);
                _current = null;
                EndGame(true, "GAME OVER");
            }
        }

        /// <summary>
        /// Termine la partie et affiche le résultat. <paramref name="isLoss"/> : la pile a atteint le haut
        /// (publie <see cref="GameOverEvent"/>). Un Sprint perdu n'a pas de temps valable, donc pas de record.
        /// </summary>
        public void EndGame(bool isLoss, string title) {
            if (_state == State.Ended) return;
            _state = State.Ended;
            Time.timeScale = 1f;

            bool hasResult = !(Mode == GameMode.Sprint && isLoss);
            float result = Mode == GameMode.Sprint ? _elapsed : Score;
            bool isRecord = hasResult && ModeRecords.Submit(Mode, result);

            string value = hasResult ? ModeRecords.Format(Mode, result) : $"{Lines} / {_config.sprintLines} lignes";
            string recordLine = ModeRecords.TryGet(Mode, out float best) ? $"Record : {ModeRecords.Format(Mode, best)}" : "";
            _hud.ShowResult(title, value, recordLine, isRecord);

            if (isLoss) {
                EventBus<GameOverEvent>.Publish(new GameOverEvent());
            }
        }

        void RefreshHud() {
            _hud.Show(Score, Lines);
            if (Mode == GameMode.Marathon) {
                _hud.ShowThirdStat(Level.ToString());
            }
            else {
                RefreshTimer();
            }
        }

        void RefreshTimer() {
            if (Mode == GameMode.Sprint) {
                _hud.ShowThirdStat(ModeRecords.FormatTime(_elapsed));
            }
            else if (Mode == GameMode.Ultra) {
                _hud.ShowThirdStat(ModeRecords.FormatTime(_config.ultraDuration - _elapsed));
            }
        }

        // ---------- Inputs ----------

        void OnMove(MoveInputEvent e) {
            if (_state != State.Playing || _current == null || e.Direction == 0) return;

            var direction = new Vector2Int(e.Direction, 0);
            if (_current.CanMove(direction)) {
                _current.Move(direction);
            }
        }

        void OnDown(DownInputEvent e) {
            _isSoftDropping = e.IsPressed;
        }

        void OnRotate(RotateInputEvent e) {
            if (_state == State.Playing && _current != null && _current.CanRotate()) {
                _current.Rotate();
            }
        }

        void OnPause(PauseInputEvent e) {
            if (_state == State.Playing) SetPaused(true);
            else if (_state == State.Paused) SetPaused(false);
        }

        void SetPaused(bool isPaused) {
            _state = isPaused ? State.Paused : State.Playing;
            Time.timeScale = isPaused ? 0f : 1f;
            _hud.SetPaused(isPaused);
        }

        // ---------- Boutons de l'écran de pause / de fin ----------

        /// <summary>Pause : reprendre. Fin de partie : revanche dans le même mode.</summary>
        void OnPrimaryClicked() {
            if (_state == State.Paused) {
                SetPaused(false);
            }
            else if (_state == State.Ended) {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        void GoToMenu() {
            Time.timeScale = 1f;
            SceneManager.LoadScene(MenuSceneName);
        }
    }
}
