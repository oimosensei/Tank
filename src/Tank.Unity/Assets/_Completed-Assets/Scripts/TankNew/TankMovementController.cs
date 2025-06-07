using UnityEngine;
using UniRx;

namespace Nakatani
{
    // Modelの状態に基づいて、タンクの移動とエンジン音を制御するクラス
    [RequireComponent(typeof(Rigidbody))]
    public class TankMovementController : MonoBehaviour
    {
        public float m_Speed = 12f;
        public float m_TurnSpeed = 180f;
        public AudioSource m_MovementAudio;
        public AudioClip m_EngineIdling;
        public AudioClip m_EngineDriving;
        public float m_PitchRange = 0.2f;

        private Rigidbody m_Rigidbody;
        private TankModel m_Model;
        private float m_OriginalPitch;
        private ParticleSystem[] m_particleSystems;

        public void Initialize(TankModel model)
        {
            m_Model = model;
            m_Rigidbody = GetComponent<Rigidbody>();
            m_OriginalPitch = m_MovementAudio.pitch;
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();

            // 移動入力があったかどうかを監視するストリームを作成
            m_Model.MovementInputValue
                .CombineLatest(m_Model.TurnInputValue, (move, turn) => Mathf.Abs(move) > 0.1f || Mathf.Abs(turn) > 0.1f)
                .DistinctUntilChanged() // 値が変化したときだけ通知
                .Subscribe(isMoving => EngineAudio(isMoving))
                .AddTo(this);
        }

        private void OnEnable()
        {
            if (m_Rigidbody != null) m_Rigidbody.isKinematic = false;
            if (m_particleSystems == null) return;
            foreach (var ps in m_particleSystems) ps.Play();
        }

        private void OnDisable()
        {
            if (m_Rigidbody != null) m_Rigidbody.isKinematic = true;
            if (m_particleSystems == null) return;
            foreach (var ps in m_particleSystems) ps.Stop();
        }

        private void FixedUpdate()
        {
            if (m_Model == null || !m_Model.IsControlEnabled.Value) return;

            // Modelの最新の入力値を使って移動と回転
            Move(m_Model.MovementInputValue.Value);
            Turn(m_Model.TurnInputValue.Value);
            GameHubClient.Instance.MoveTank(transform.position, transform.rotation);
        }

        private void Move(float inputValue)
        {
            var movement = transform.forward * inputValue * m_Speed * Time.deltaTime;
            m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
        }

        private void Turn(float inputValue)
        {
            var turn = inputValue * m_TurnSpeed * Time.deltaTime;
            var turnRotation = Quaternion.Euler(0f, turn, 0f);
            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }

        private void EngineAudio(bool isMoving)
        {
            if (isMoving)
            {
                m_MovementAudio.clip = m_EngineDriving;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
            else
            {
                m_MovementAudio.clip = m_EngineIdling;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play();
            }
        }
    }
}