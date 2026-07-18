using System.Threading;
using Cysharp.Threading.Tasks;
using Sinkii09.UIFramework;
using UnityEngine;
using VContainer;

namespace AircraftStriker
{
    public class AircraftGameplayState : IGameState
    {
        private readonly IUINavigator          _navigator;
        private readonly GameplayController    _gameplay;
        private readonly PlayerController      _playerShipPrefab;
        private readonly AircraftInputHandler  _inputHandler;
        private readonly ITransitionOverlay    _overlay;

        private PlayerController _playerInstance;

        [Inject]
        public AircraftGameplayState(IUINavigator navigator, GameplayController gameplay,
            PlayerController playerShipPrefab, AircraftInputHandler inputHandler, ITransitionOverlay overlay)
        {
            _navigator        = navigator;
            _gameplay         = gameplay;
            _playerShipPrefab = playerShipPrefab;
            _inputHandler     = inputHandler;
            _overlay          = overlay;
        }

        public string SceneName    => null;
        public bool PausesGameTime => false;

        public async UniTask OnEnterAsync(CancellationToken ct = default)
        {
            if (_playerShipPrefab == null)
            {
                Debug.LogError("[AircraftGameplayState] PlayerShip prefab not assigned in AircraftLifetimeScope.");
                return;
            }
            if (_playerInstance != null)
            {
                Debug.LogWarning("[AircraftGameplayState] OnEnterAsync called while player instance is live — skipping spawn.");
                return;
            }

            await _navigator.CloseAllAsync(ct);
            await _navigator.ShowAsync<AircraftHUDView>(ct);

            _playerInstance = Object.Instantiate(_playerShipPrefab);
            float camHalf = Camera.main != null ? Camera.main.orthographicSize : 5f;
            _playerInstance.transform.position = new Vector3(0f, -camHalf * 0.7f, 0f);

            _gameplay.SetPlayerTransform(_playerInstance.transform);
            _playerInstance.Initialize(_inputHandler, _gameplay);

            // Overlay must be fully hidden before gameplay starts — otherwise wave spawning could
            // begin while still covered, and the user would see an already-in-progress game the
            // instant the curtain lifts. GameLifecycleManager's own hide-in-finally becomes a
            // harmless no-op afterward (ITransitionOverlay.HideAsync is idempotent).
            // The overlay is decorative and must never abort state entry — same principle as
            // GameLifecycleManager's own Show/HideOverlaySafeAsync — so failures are logged and
            // swallowed rather than propagated (an uncaught throw here would leave UIStateMachine's
            // CurrentState null, permanently breaking RestartCurrentStateAsync).
            try
            {
                await _overlay.HideAsync(ct);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AircraftGameplayState] overlay hide failed: {e}");
            }
            _gameplay.StartGame();
        }

        public UniTask OnExitAsync(CancellationToken ct = default)
        {
            _gameplay.StopGame();
            if (_playerInstance != null)
            {
                Object.Destroy(_playerInstance.gameObject);
                _playerInstance = null;
            }
            _gameplay.SetPlayerTransform(null);
            return _navigator.CloseAllAsync(ct);
        }
    }
}
