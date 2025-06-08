using UnityEngine;
using UniRx;

namespace Nakatani
{
    // Modelからの指示で、実際に砲弾を発射するクラス
    // todo Inputからどのように指示が下るかの仕組み、リファクタリング候補
    public class TankShootingController : MonoBehaviour
    {
        public Rigidbody m_Shell;
        public Transform m_FireTransform;
        public AudioSource m_ShootingAudio;
        public AudioClip m_ChargingClip;
        public AudioClip m_FireClip;

        public void Initialize(TankModel model)
        {
            // 発射イベントを購読して、砲弾を発射
            model.OnFire
                .Subscribe(_ => Fire(model.CurrentLaunchForce.Value))
                .AddTo(this);

            // チャージ状態を購読して、チャージ音を再生
            model.IsCharging
                .DistinctUntilChanged()
                .Where(isCharging => isCharging)
                .Subscribe(_ =>
                {
                    m_ShootingAudio.clip = m_ChargingClip;
                    m_ShootingAudio.Play();
                })
                .AddTo(this);
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