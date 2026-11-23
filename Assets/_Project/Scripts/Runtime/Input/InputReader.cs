using Tetris.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tetris {
    /// <summary>
    /// Lit les actions du <see cref="PlayerInput"/> et les traduit en événements sur l'<see cref="EventBus{T}"/>.
    /// Le reste du jeu ne dépend jamais de l'Input System : il s'abonne aux événements de InputEvents.cs.
    /// </summary>
    /// <remarks>
    /// S'abonne directement aux InputAction : fonctionne quel que soit le "Behavior" du PlayerInput.
    /// </remarks>
    [RequireComponent(typeof(PlayerInput))]
    public class InputReader : MonoBehaviour {
        const string MoveActionName = "Player/Move";
        const string DownActionName = "Player/Down";
        const string RotateActionName = "Player/Rotate";
        const string PauseActionName = "Player/Pause";
        const string MiscActionName = "Player/Misc";

        [Tooltip("Valeur absolue minimale de l'axe Move pour être considérée comme une direction (utile pour les sticks).")]
        [SerializeField, Range(0.1f, 1f)] float _moveThreshold = 0.5f;

        InputAction _moveAction;
        InputAction _downAction;
        InputAction _rotateAction;
        InputAction _pauseAction;
        InputAction _miscAction;
        int _moveDirection;
        bool _isDownPressed;

        void Awake() {
            InputActionAsset actions = GetComponent<PlayerInput>().actions;
            _moveAction = actions.FindAction(MoveActionName, throwIfNotFound: true);
            _downAction = actions.FindAction(DownActionName, throwIfNotFound: true);
            _rotateAction = actions.FindAction(RotateActionName, throwIfNotFound: true);
            _pauseAction = actions.FindAction(PauseActionName, throwIfNotFound: true);
            _miscAction = actions.FindAction(MiscActionName, throwIfNotFound: true);
        }

        void OnEnable() {
            _moveAction.performed += HandleMove;
            _moveAction.canceled += HandleMove;
            _downAction.performed += HandleDown;
            _downAction.canceled += HandleDown;
            _rotateAction.performed += HandleRotate;
            _pauseAction.performed += HandlePause;
            _miscAction.performed += HandleMisc;
            _miscAction.canceled += HandleMisc;
        }

        void OnDisable() {
            _moveAction.performed -= HandleMove;
            _moveAction.canceled -= HandleMove;
            _downAction.performed -= HandleDown;
            _downAction.canceled -= HandleDown;
            _rotateAction.performed -= HandleRotate;
            _pauseAction.performed -= HandlePause;
            _miscAction.performed -= HandleMisc;
            _miscAction.canceled -= HandleMisc;

            // Évite qu'un abonné reste bloqué sur un input "maintenu" quand le reader est désactivé.
            SetMoveDirection(0);
            SetDownPressed(false);
        }

        void HandleMove(InputAction.CallbackContext context) {
            float value = context.ReadValue<float>();
            int direction = value >= _moveThreshold ? 1 : value <= -_moveThreshold ? -1 : 0;
            SetMoveDirection(direction);
        }

        void HandleDown(InputAction.CallbackContext context) {
            SetDownPressed(context.performed);
        }

        void HandleRotate(InputAction.CallbackContext context) {
            EventBus<RotateInputEvent>.Publish(new RotateInputEvent());
        }

        void HandlePause(InputAction.CallbackContext context) {
            EventBus<PauseInputEvent>.Publish(new PauseInputEvent());
        }

        void HandleMisc(InputAction.CallbackContext context) {
            EventBus<MiscInputEvent>.Publish(new MiscInputEvent(context.performed));
        }

        void SetMoveDirection(int direction) {
            if (direction == _moveDirection) return;

            _moveDirection = direction;
            EventBus<MoveInputEvent>.Publish(new MoveInputEvent(direction));
        }

        void SetDownPressed(bool isPressed) {
            if (isPressed == _isDownPressed) return;

            _isDownPressed = isPressed;
            EventBus<DownInputEvent>.Publish(new DownInputEvent(isPressed));
        }
    }
}
