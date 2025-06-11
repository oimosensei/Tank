using UnityEngine;
using UniRx;

namespace Nakatani
{
    // InputControllerの状態に基づいて、タンクの移動とエンジン音を制御するクラス
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
        private TankInputController m_InputController;
        private float m_OriginalPitch;
        private ParticleSystem[] m_particleSystems;

        [SerializeField]
        private Transform m_TurretTransform;
        private Quaternion lastParentRotation; // 親オブジェクト（車体）の前フレームでの回転を保存

        public void Initialize(TankInputController inputController)
        {
            m_InputController = inputController;
            m_Rigidbody = GetComponent<Rigidbody>();
            m_OriginalPitch = m_MovementAudio.pitch;
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();

            // 移動入力があったかどうかを監視するストリームを作成
            m_InputController.MovementInputValue
                .CombineLatest(m_InputController.TurnInputValue, (move, turn) => Mathf.Abs(move) > 0.1f || Mathf.Abs(turn) > 0.1f)
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
            if (m_InputController == null) return;

            // InputControllerの最新の入力値を使って移動と回転
            Move(m_InputController.MovementInputValue.Value);
            Turn(m_InputController.TurnInputValue.Value);
            //todo オフラインの時どうするか
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

        private void LateUpdate()
        {
            if (m_TurretTransform == null) return;
            // 1. 親が今フレームでどれだけ回転したかを計算する
            // (現在の親の回転) * (前の親の回転の逆) = 差分の回転
            Quaternion parentRotationDelta = transform.rotation * Quaternion.Inverse(lastParentRotation);

            // 2. 砲塔の現在の回転に、親の回転差分の「逆」を掛けることで、親の回転を打ち消す
            // これにより、砲塔はワールド空間で同じ向きを保とうとする
            m_TurretTransform.rotation = Quaternion.Inverse(parentRotationDelta) * m_TurretTransform.rotation;

            // 3. 次のフレームのために、現在の親の回転を保存する
            lastParentRotation = transform.rotation;
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