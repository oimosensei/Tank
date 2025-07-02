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

        private void OnSpectateTargetChanged(Guid targetId)
        {
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

            cameraSwitcher.SetCameraMode(true);
            Debug.Log($"Started spectating tank {targetId}");
        }

        private void OnStopSpectating()
        {
            if (_spectateTargetId.Value == Guid.Empty) return;

            if (TankManager.Instance == null)
            {
                Debug.LogError("TankManager.Instance is null");
                return;
            }

            var targetTank = TankManager.Instance.GetTank(_spectateTargetId.Value);
            if (targetTank != null)
            {
                var cameraSwitcher = targetTank.GetComponent<CameraSwitcher>();
                if (cameraSwitcher != null)
                {
                    cameraSwitcher.SetCameraMode(false);
                    Debug.Log($"Stopped spectating tank {_spectateTargetId.Value}");
                }
            }
        }

        void OnDestroy()
        {
            _isSpectating?.Dispose();
            _spectateTargetId?.Dispose();
        }
    }
}