using UnityEngine;

namespace Nakatani
{
    // ネットワーク経由で受信した情報に基づいて、タンクの移動とエンジン音を制御するクラス
    [RequireComponent(typeof(Rigidbody))]
    public class TankNetworkMovementController : MonoBehaviour // クラス名を変更しました
    {
        // AudioSourceとAudioClip関連の変数は元のまま
        public AudioSource m_MovementAudio;
        public AudioClip m_EngineIdling;
        public AudioClip m_EngineDriving;
        public float m_PitchRange = 0.2f;

        private Rigidbody m_Rigidbody;
        private float m_OriginalPitch;
        private ParticleSystem[] m_particleSystems;

        private Vector3 m_PreviousPosition;
        private bool m_IsAudioPlayingDriveSound = false; // 現在駆動音を再生しているか

        private TankModel m_Model; // TankModelを追加

        public void Initialize(TankModel model)
        {
            m_Model = model;
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.isKinematic = true; // ネットワーク同期なのでKinematicに設定

            if (m_MovementAudio != null)
            {
                m_OriginalPitch = m_MovementAudio.pitch;
            }
            else
            {
                Debug.LogWarning("MovementAudio is not assigned on " + gameObject.name, this);
            }
            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            m_PreviousPosition = transform.position; // 初期位置を記録
        }

        private void OnEnable()
        {
            // RigidbodyはAwakeでisKinematic = trueにしているので、必須ではないが念のため
            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = true;
            }

            // パーティクルシステムの再生
            if (m_particleSystems != null)
            {
                foreach (var ps in m_particleSystems)
                {
                    if (ps != null) ps.Play();
                }
            }
            // 有効化された時点では停止していると仮定し、アイドリング音を再生開始
            // (UpdateMovementがすぐに呼ばれて上書きされる可能性もある)
            PlayEngineAudio(false);
        }

        private void OnDisable()
        {
            // オーディオ停止
            if (m_MovementAudio != null && m_MovementAudio.isPlaying)
            {
                m_MovementAudio.Stop();
            }

            // パーティクルシステムの停止
            if (m_particleSystems != null)
            {
                foreach (var ps in m_particleSystems)
                {
                    if (ps != null) ps.Stop();
                }
            }
        }

        /// <summary>
        /// ネットワークから受信した情報でタンクの状態を更新します。
        /// このメソッドをサーバーからのメッセージ受信時に呼び出してください。
        /// </summary>
        /// <param name="newPosition">新しい目標位置</param>
        /// <param name="newRotation">新しい目標回転</param>
        public void UpdateTankState(Vector3 newPosition, Quaternion newRotation)
        {
            // 前回の位置と新しい位置を比較して移動しているか判定
            // ごくわずかな移動は無視するための閾値
            bool isMoving = (newPosition - m_PreviousPosition).sqrMagnitude > 0.0001f;

            // 位置と回転を直接設定 (補間なしのシンプルなバージョン)
            transform.position = newPosition;
            transform.rotation = newRotation;

            // エンジン音の制御
            PlayEngineAudio(isMoving);

            m_PreviousPosition = newPosition; // 現在の位置を次の比較のために保存
        }

        private void PlayEngineAudio(bool isMoving)
        {
            if (m_MovementAudio == null) return;

            AudioClip targetClip = isMoving ? m_EngineDriving : m_EngineIdling;

            // 再生するクリップが変更されたか、現在の移動状態と音の状態が一致しない場合のみ更新
            if (m_MovementAudio.clip != targetClip || m_IsAudioPlayingDriveSound != isMoving)
            {
                m_MovementAudio.clip = targetClip;
                m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                m_MovementAudio.Play(); // Play()はクリップが変われば最初から再生
                m_IsAudioPlayingDriveSound = isMoving;
            }
            // (オプション) AudioSourceのLoopがfalseのアイドリング音が停止していたら再開
            else if (!isMoving && targetClip == m_EngineIdling && !m_MovementAudio.isPlaying)
            {
                // AudioSourceのLoopプロパティをtrueにすることを推奨
                // m_MovementAudio.Play();
            }
        }
    }
}