using UnityEngine;
using UniRx;

namespace Nakatani
{
    // InputControllerからの入力を受けて、発射管理と実際の砲弾発射を行うクラス
    public class TankShootingController : MonoBehaviour
    {
        public Rigidbody m_Shell;
        public Transform m_FireTransform;
        public AudioSource m_ShootingAudio;
        public AudioClip m_ChargingClip;
        public AudioClip m_FireClip;

        // 発射管理用のパラメータ
        private float m_MinLaunchForce;
        private float m_MaxLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired = true; // 初期は発射不可状態

        // 発射状態をリアクティブプロパティとして公開
        public ReactiveProperty<float> CurrentLaunchForce { get; private set; } = new ReactiveProperty<float>(15f); // デフォルト値で初期化
        public BoolReactiveProperty IsCharging { get; } = new BoolReactiveProperty(false);

        // 発射イベント
        public ISubject<Unit> OnFire { get; } = new Subject<Unit>();

        // 設定値の読み取り専用プロパティ
        public float MinLaunchForce => m_MinLaunchForce;
        public float MaxLaunchForce => m_MaxLaunchForce;

        public void Initialize(TankInputController inputController, float minLaunchForce, float maxLaunchForce, float maxChargeTime)
        {
            // 発射設定の初期化
            m_MinLaunchForce = minLaunchForce;
            m_MaxLaunchForce = maxLaunchForce;
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / maxChargeTime;
            CurrentLaunchForce.Value = m_MinLaunchForce; // 既存のインスタンスの値を更新

            // InputControllerからの入力を監視して発射管理
            inputController.IsFireButtonDown
                .Where(isDown => isDown)
                .Subscribe(_ => StartCharging())
                .AddTo(this);

            inputController.IsFireButtonHeld
                .Subscribe(isHeld =>
                {
                    if (isHeld && !m_Fired)
                    {
                        ContinueCharging();
                    }
                })
                .AddTo(this);

            inputController.IsFireButtonUp
                .Where(isUp => isUp)
                .Subscribe(_ => TryFire())
                .AddTo(this);

            // チャージ状態を購読して、チャージ音を再生
            IsCharging
                .DistinctUntilChanged()
                .Where(isCharging => isCharging)
                .Subscribe(_ =>
                {
                    m_ShootingAudio.clip = m_ChargingClip;
                    m_ShootingAudio.Play();
                })
                .AddTo(this);

            // 発射イベントを購読して、砲弾を発射
            OnFire
                .Subscribe(_ => Fire(CurrentLaunchForce.Value))
                .AddTo(this);
        }

        private void StartCharging()
        {
            m_Fired = false;
            IsCharging.Value = true;
            CurrentLaunchForce.Value = m_MinLaunchForce;
        }

        private void ContinueCharging()
        {
            if (CurrentLaunchForce.Value >= m_MaxLaunchForce)
            {
                // 最大チャージに達したら自動発射
                TryFire();
            }
            else
            {
                CurrentLaunchForce.Value += m_ChargeSpeed * Time.deltaTime;
                Debug.Log("chargingg");
            }
        }

        private void TryFire()
        {
            if (!m_Fired)
            {
                m_Fired = true;
                IsCharging.Value = false;
                //TODO Onfire経由するのぜったいいらん
                OnFire.OnNext(Unit.Default);
                CurrentLaunchForce.Value = m_MinLaunchForce;
            }
        }

        public void Reset()
        {
            if (CurrentLaunchForce != null)
            {
                CurrentLaunchForce.Value = m_MinLaunchForce;
            }
            IsCharging.Value = false;
            m_Fired = true;
        }

        private void Fire(float launchForce)
        {
            // ネットワーク対応の発射処理
            Vector3 firePosition = m_FireTransform.position;
            Vector3 velocity = launchForce * m_FireTransform.forward;
            Quaternion rotation = m_FireTransform.rotation;

            Debug.Log($"[TankShootingController] Fire called: force={launchForce}, pos={firePosition}, vel={velocity}");

            // サーバーに発射を通知（ローカルではシェルを生成せず、サーバーからの通知で生成）
            if (GameHubClient.Instance != null)
            {
                GameHubClient.Instance.ShootShell(firePosition, velocity, rotation, launchForce);
                Debug.Log($"[TankShootingController] ShootShell sent to server: force={launchForce}, pos={firePosition}, vel={velocity}");
            }
            else
            {
                Debug.LogError("[TankShootingController] GameHubClient.Instance is null - cannot send shell data to server");

                // フォールバック: ローカルでシェルを生成（デバッグ用）
                var shellInstance = Instantiate(m_Shell, firePosition, rotation) as Rigidbody;
                shellInstance.velocity = velocity;
                Debug.Log("[TankShootingController] Fallback: Created local shell");
            }

            // 発射音を再生
            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
        }
    }
}