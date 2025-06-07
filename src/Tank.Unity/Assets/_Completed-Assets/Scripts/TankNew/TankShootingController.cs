using UnityEngine;
using UniRx;

namespace Nakatani
{
    // Modelからの指示で、実際に砲弾を発射するクラス
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
            var shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
            shellInstance.velocity = launchForce * m_FireTransform.forward;

            m_ShootingAudio.clip = m_FireClip;
            m_ShootingAudio.Play();
        }
    }
}