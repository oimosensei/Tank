using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace Nakatani
{
    // Modelを購読し、見た目を更新するクラス
    public class TankView : MonoBehaviour
    {
        [Header("UI References")]
        public Slider m_HealthSlider;
        public Image m_FillImage;
        public Color m_FullHealthColor = Color.green;
        public Color m_ZeroHealthColor = Color.red;
        public Slider m_AimSlider;
        public GameObject m_CanvasGameObject;

        [Header("Effects")]
        public GameObject m_ExplosionPrefab;

        private ParticleSystem m_ExplosionParticles;
        private AudioSource m_ExplosionAudio;
        private TankModel m_Model;
        private TankShootingController m_ShootingController;

        public void Initialize(TankModel model)
        {
            m_Model = model;
            m_ShootingController = GetComponent<TankShootingController>();

            // 爆発エフェクトを準備
            var explosionInstance = Instantiate(m_ExplosionPrefab);
            m_ExplosionParticles = explosionInstance.GetComponent<ParticleSystem>();
            m_ExplosionAudio = explosionInstance.GetComponent<AudioSource>();
            explosionInstance.SetActive(false);

            // --- Modelのプロパティとイベントを購読 ---

            // 体力UIの更新
            m_Model.CurrentHealth
                .Subscribe(health => SetHealthUI(health, m_Model.m_StartingHealth))
                .AddTo(this);

            // 照準UIの更新（ShootingControllerから取得）
            if (m_ShootingController != null)
            {
                m_ShootingController.CurrentLaunchForce
                    .Subscribe(force => m_AimSlider.value = force)
                    .AddTo(this);
                m_ShootingController.CurrentLaunchForce
                    .Subscribe(force => Debug.Log("currentforce: " + force.ToString()))
                    .AddTo(this);
            }

            // タンクの色の設定
            m_Model.PlayerColor
                .Subscribe(SetTankColor)
                .AddTo(this);

            // 操作可能状態に応じてUIキャンバスの表示を切り替え
            m_Model.IsControlEnabled
                .Subscribe(m_CanvasGameObject.SetActive)
                .AddTo(this);

            // 死亡イベントを購読
            m_Model.OnDeath
                .Subscribe(_ => OnDeath())
                .AddTo(this);

            // 死亡したらGameObjectを非アクティブ化
            m_Model.IsDead
                .Where(isDead => isDead)
                .Subscribe(_ => gameObject.SetActive(false))
                .AddTo(this);

            // 初期UI設定
            m_HealthSlider.maxValue = m_Model.CurrentHealth.Value;

            // ShootingControllerから照準UIの設定を取得
            if (m_ShootingController != null)
            {
                m_AimSlider.minValue = m_ShootingController.MinLaunchForce;
                m_AimSlider.maxValue = m_ShootingController.MaxLaunchForce;
            }
        }

        private void SetHealthUI(float current, float starting)
        {
            m_HealthSlider.value = current;
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, current / starting);
        }

        private void SetTankColor(Color color)
        {
            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                r.material.color = color;
            }
        }

        private void OnDeath()
        {
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);
            m_ExplosionParticles.Play();
            m_ExplosionAudio.Play();
        }

        private void OnDestroy()
        {
            // このGameObjectが破棄されるときに、Modelのリソースも解放する
            m_Model?.Dispose();
        }
    }
}