using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;

namespace Nakatani
{
    public class SpectateManager : MonoBehaviour
    {
        public static SpectateManager Instance { get; private set; }

        private readonly ReactiveProperty<bool> _isSpectating = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsSpectating => _isSpectating;

        private readonly ReactiveProperty<Guid> _spectateTargetId = new ReactiveProperty<Guid>(Guid.Empty);
        public IReadOnlyReactiveProperty<Guid> SpectateTargetId => _spectateTargetId;

        private readonly ReactiveProperty<string> _currentPlayerName = new ReactiveProperty<string>("");
        public IReadOnlyReactiveProperty<string> CurrentPlayerName => _currentPlayerName;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            _spectateTargetId
                .Where(guid => guid != Guid.Empty)
                .Subscribe(OnSpectateTargetChanged)
                .AddTo(this);

            _isSpectating
                .Where(isSpectating => !isSpectating)
                .Subscribe(_ => OnStopSpectating())
                .AddTo(this);

            _spectateTargetId
                .Subscribe(UpdateCurrentPlayerName)
                .AddTo(this);
        }

        public void StartSpectating(Guid targetId)
        {
            _isSpectating.Value = true;
            _spectateTargetId.Value = targetId;
        }

        public void StopSpectating()
        {
            _isSpectating.Value = false;
            _spectateTargetId.Value = Guid.Empty;
        }

        public void StartSpectatingRandomPlayer()
        {
            if (TankManager.Instance == null)
            {
                Debug.LogError("TankManager.Instance is null");
                return;
            }

            var tankKeys = TankManager.Instance.tanks.Keys.ToArray();
            if (tankKeys.Length == 0)
            {
                Debug.LogWarning("No tanks found for spectating");
                return;
            }

            // ランダムなインデックスを選択
            var randomIndex = UnityEngine.Random.Range(0, tankKeys.Length);
            var randomPlayerGuid = tankKeys[randomIndex];

            StartSpectating(randomPlayerGuid);
            Debug.Log($"Started spectating random player: {randomPlayerGuid}");
        }

        public void SpectateNextPlayer()
        {
            //todo スペ区てーとしているプレイヤーがいなくなったときの処理を考える
            //todo カメラを一つにして、それを動かすほうがいいのではないか？という問題と、自由観戦のカメラを作りたい問題をどうにかする
            if (TankManager.Instance == null)
            {
                Debug.LogError("TankManager.Instance is null");
                return;
            }

            var tankKeys = TankManager.Instance.tanks.Keys.ToArray();
            if (tankKeys.Length == 0)
            {
                Debug.LogWarning("No tanks found for spectating");
                return;
            }

            if (!_isSpectating.Value)
            {
                // 観戦中でない場合は最初のプレイヤーを観戦
                StartSpectating(tankKeys[0]);
                return;
            }

            // 現在の観戦対象のインデックスを取得
            var currentIndex = Array.IndexOf(tankKeys, _spectateTargetId.Value);
            if (currentIndex == -1)
            {
                // 現在の対象が見つからない場合は最初のプレイヤーを観戦
                StartSpectating(tankKeys[0]);
                return;
            }

            // 次のインデックスを計算（循環）
            var nextIndex = (currentIndex + 1) % tankKeys.Length;
            var nextPlayerGuid = tankKeys[nextIndex];

            StartSpectating(nextPlayerGuid);
            Debug.Log($"Switched to next player: {nextPlayerGuid}");
        }

        public void SpectatePreviousPlayer()
        {
            if (TankManager.Instance == null)
            {
                Debug.LogError("TankManager.Instance is null");
                return;
            }

            var tankKeys = TankManager.Instance.tanks.Keys.ToArray();
            if (tankKeys.Length == 0)
            {
                Debug.LogWarning("No tanks found for spectating");
                return;
            }

            if (!_isSpectating.Value)
            {
                // 観戦中でない場合は最後のプレイヤーを観戦
                StartSpectating(tankKeys[tankKeys.Length - 1]);
                return;
            }

            // 現在の観戦対象のインデックスを取得
            var currentIndex = Array.IndexOf(tankKeys, _spectateTargetId.Value);
            if (currentIndex == -1)
            {
                // 現在の対象が見つからない場合は最後のプレイヤーを観戦
                StartSpectating(tankKeys[tankKeys.Length - 1]);
                return;
            }

            // 前のインデックスを計算（循環）
            var previousIndex = (currentIndex - 1 + tankKeys.Length) % tankKeys.Length;
            var previousPlayerGuid = tankKeys[previousIndex];

            StartSpectating(previousPlayerGuid);
            Debug.Log($"Switched to previous player: {previousPlayerGuid}");
        }

        private GameObject _currentSpectateTarget;

        private void OnSpectateTargetChanged(Guid targetId)
        {
            // 前の観戦対象のカメラを無効化
            if (_currentSpectateTarget != null)
            {
                var previousCameraSwitcher = _currentSpectateTarget.GetComponent<CameraSwitcher>();
                if (previousCameraSwitcher != null)
                {
                    previousCameraSwitcher.SetCameraMode(false);
                    Debug.Log($"Disabled camera for previous spectate target");
                }
            }

            if (TankManager.Instance == null)
            {
                Debug.LogError("TankManager.Instance is null");
                return;
            }

            var targetTank = TankManager.Instance.GetTank(targetId);
            if (targetTank == null)
            {
                Debug.LogWarning($"Target tank {targetId} not found");
                return;
            }

            var cameraSwitcher = targetTank.GetComponent<CameraSwitcher>();
            if (cameraSwitcher == null)
            {
                Debug.LogWarning($"CameraSwitcher not found on tank {targetId}");
                return;
            }

            // 新しい観戦対象のカメラを有効化
            cameraSwitcher.SetCameraMode(true);
            _currentSpectateTarget = targetTank;
            Debug.Log($"Started spectating tank {targetId}");
        }

        private void OnStopSpectating()
        {
            // 現在の観戦対象のカメラを無効化
            if (_currentSpectateTarget != null)
            {
                var cameraSwitcher = _currentSpectateTarget.GetComponent<CameraSwitcher>();
                if (cameraSwitcher != null)
                {
                    cameraSwitcher.SetCameraMode(false);
                    Debug.Log($"Stopped spectating tank {_spectateTargetId.Value}");
                }
                _currentSpectateTarget = null;
            }
        }

        private void UpdateCurrentPlayerName(Guid targetId)
        {
            if (targetId == Guid.Empty)
            {
                _currentPlayerName.Value = "";
                return;
            }

            if (TankManager.Instance == null)
            {
                _currentPlayerName.Value = "";
                return;
            }

            var targetTank = TankManager.Instance.GetTank(targetId);
            if (targetTank == null)
            {
                _currentPlayerName.Value = "";
                return;
            }

            var tankInitializer = targetTank.GetComponent<TankInitializer>();
            if (tankInitializer == null)
            {
                _currentPlayerName.Value = "";
                return;
            }

            var tankModel = tankInitializer.Model;
            if (tankModel == null)
            {
                _currentPlayerName.Value = "";
                return;
            }

            _currentPlayerName.Value = tankModel.ColoredPlayerText.Value;
        }

        void OnDestroy()
        {
            _isSpectating?.Dispose();
            _spectateTargetId?.Dispose();
            _currentPlayerName?.Dispose();
        }
    }
}