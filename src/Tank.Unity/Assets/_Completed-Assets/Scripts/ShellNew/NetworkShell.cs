using UnityEngine;
using System;

namespace Nakatani
{
    public class NetworkShell : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField]
        private bool isOwnShell = false; // 自分が発射したシェルかどうか

        [Header("Physics")]
        [SerializeField]
        private float maxLifeTime = 5f; // シェルの最大生存時間

        [Header("Effects")]
        [SerializeField]
        private ParticleSystem explosionParticles;
        [SerializeField]
        private AudioSource explosionAudio;

        [Header("Explosion Settings")]
        [SerializeField]
        private float explosionRadius = 5f;
        [SerializeField]
        private float maxDamage = 100f;
        [SerializeField]
        private float explosionForce = 1000f;
        [SerializeField]
        private LayerMask tankMask = -1;

        private ShellInfo shellInfo;
        private Rigidbody shellRigidbody;
        private bool hasExploded = false;
        private float spawnTime;

        // ネットワーク同期用
        private Vector3 networkPosition;
        private Vector3 networkVelocity;
        private bool hasNetworkUpdate = false;

        private void Awake()
        {
            shellRigidbody = GetComponent<Rigidbody>();
            spawnTime = Time.time;
        }

        private void Start()
        {
            // 最大生存時間後に自動削除
            Destroy(gameObject, maxLifeTime);
        }

        private void FixedUpdate()
        {
            // 自分のシェルの場合、定期的にサーバーに位置を送信
            if (isOwnShell && !hasExploded && shellInfo != null)
            {
                SendPositionUpdate();
            }

            // 他人のシェルの場合、ネットワークからの位置情報に基づいて補間
            if (!isOwnShell && hasNetworkUpdate)
            {
                InterpolateToNetworkPosition();
            }
        }

        /// <summary>
        /// シェルを初期化
        /// </summary>
        /// <param name="info">シェル情報</param>
        public void Initialize(ShellInfo info)
        {
            shellInfo = info;

            // 自分が発射したシェルかどうかを確認
            if (GameHubClient.Instance != null)
            {
                isOwnShell = (info.ShooterId == GameHubClient.Instance.MyConnectionId);
                Debug.Log($"[NetworkShell] isOwnShell check: ShooterId={info.ShooterId}, MyId={GameHubClient.Instance.MyConnectionId}, isOwn={isOwnShell}");
                Debug.Log($"[NetworkShell] ShooterId == MyConnectionId: {info.ShooterId == GameHubClient.Instance.MyConnectionId}");
                Debug.Log($"[NetworkShell] ShooterId.ToString(): '{info.ShooterId.ToString()}'");
                Debug.Log($"[NetworkShell] MyConnectionId.ToString(): '{GameHubClient.Instance.MyConnectionId.ToString()}'");
            }
            else
            {
                Debug.LogWarning("[NetworkShell] GameHubClient.Instance is null during Initialize");
            }

            // 物理設定を適用
            if (shellRigidbody != null)
            {
                shellRigidbody.velocity = info.Velocity;
                Debug.Log($"[NetworkShell] Rigidbody velocity set to: {info.Velocity}");
            }
            else
            {
                Debug.LogError($"[NetworkShell] Rigidbody is null for shell {info.Id}");
            }

            transform.position = info.Position;
            transform.rotation = info.Rotation;

            Debug.Log($"[NetworkShell] Shell initialized: {info.Id}, isOwn: {isOwnShell}, position: {info.Position}");
        }

        /// <summary>
        /// ネットワークからの位置更新を受信
        /// </summary>
        /// <param name="position">新しい位置</param>
        /// <param name="velocity">新しい速度</param>
        public void UpdateFromNetwork(Vector3 position, Vector3 velocity)
        {
            Debug.Log($"[NetworkShell] UpdateFromNetwork called: isOwn={isOwnShell}, hasExploded={hasExploded}, pos={position}, vel={velocity}");

            if (isOwnShell || hasExploded)
            {
                Debug.Log($"[NetworkShell] UpdateFromNetwork ignored: isOwn={isOwnShell}, hasExploded={hasExploded}");
                return; // 自分のシェルまたは爆発済みは無視
            }

            networkPosition = position;
            networkVelocity = velocity;
            hasNetworkUpdate = true;
            Debug.Log($"[NetworkShell] Network position/velocity updated");
        }

        /// <summary>
        /// ネットワークからの爆発指示を受信
        /// </summary>
        /// <param name="explosionPosition">爆発位置</param>
        /// <param name="shooterId">発射者ID</param>
        public void ExplodeFromNetwork(Vector3 explosionPosition, Guid shooterId)
        {
            Debug.Log($"[NetworkShell] ExplodeFromNetwork called: hasExploded={hasExploded}, pos={explosionPosition}, shooterId={shooterId}");

            if (hasExploded)
            {
                Debug.Log($"[NetworkShell] ExplodeFromNetwork ignored - already exploded");
                return;
            }

            hasExploded = true;
            transform.position = explosionPosition;
            Debug.Log($"[NetworkShell] Explosion position set to: {explosionPosition}");

            // 爆発エフェクトを再生
            Debug.Log($"[NetworkShell] Playing explosion effects");
            PlayExplosionEffects();

            // ダメージ処理
            Debug.Log($"[NetworkShell] Processing explosion damage");
            ProcessExplosionDamage(explosionPosition, shooterId);

            // オブジェクトを削除
            Debug.Log($"[NetworkShell] Destroying shell object");
            Destroy(gameObject);
        }

        private void SendPositionUpdate()
        {
            // 一定間隔でサーバーに位置情報を送信
            if (Time.time - spawnTime > 0.1f) // 0.1秒間隔
            {
                if (GameHubClient.Instance != null && shellRigidbody != null)
                {
                    GameHubClient.Instance.UpdateShell(
                        shellInfo.Id,
                        transform.position,
                        shellRigidbody.velocity
                    );
                }
                spawnTime = Time.time;
            }
        }

        private void InterpolateToNetworkPosition()
        {
            if (!hasNetworkUpdate) return;

            // ネットワーク位置に向かって補間
            float lerpSpeed = 10f * Time.fixedDeltaTime;
            transform.position = Vector3.Lerp(transform.position, networkPosition, lerpSpeed);

            if (shellRigidbody != null)
            {
                shellRigidbody.velocity = Vector3.Lerp(shellRigidbody.velocity, networkVelocity, lerpSpeed);
            }
        }

        private void PlayExplosionEffects()
        {
            // パーティクルエフェクトを再生
            if (explosionParticles != null)
            {
                explosionParticles.transform.parent = null;
                explosionParticles.Play();

                // パーティクル終了後に削除
                ParticleSystem.MainModule mainModule = explosionParticles.main;
                Destroy(explosionParticles.gameObject, mainModule.duration);
            }

            // サウンドエフェクトを再生
            if (explosionAudio != null)
            {
                explosionAudio.Play();
            }
        }

        private void ProcessExplosionDamage(Vector3 explosionPosition, Guid shooterId)
        {
            // 爆発範囲内のタンクにダメージを与える
            Collider[] colliders = Physics.OverlapSphere(explosionPosition, explosionRadius, tankMask);

            foreach (Collider collider in colliders)
            {
                Rigidbody targetRigidbody = collider.GetComponent<Rigidbody>();
                if (targetRigidbody == null) continue;

                // 爆発力を適用
                targetRigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);

                // ダメージ計算
                float distance = Vector3.Distance(explosionPosition, targetRigidbody.position);
                float relativeDistance = (explosionRadius - distance) / explosionRadius;
                float damage = relativeDistance * maxDamage;
                damage = Mathf.Max(0f, damage);

                // TankHealthコンポーネントにダメージを適用
                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(damage);
                    Debug.Log($"Tank took {damage} damage from shell explosion by {shooterId}");
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[NetworkShell] OnTriggerEnter: isOwn={isOwnShell}, hasExploded={hasExploded}, collider={other.name}");

            // 自分のシェルの場合のみ、衝突時に爆発をサーバーに通知
            if (isOwnShell && !hasExploded)
            {
                Vector3 explosionPos = transform.position;
                Debug.Log($"[NetworkShell] Own shell collision detected, sending explosion to server at {explosionPos}");

                if (GameHubClient.Instance != null)
                {
                    GameHubClient.Instance.ExplodeShell(shellInfo.Id, explosionPos);
                    Debug.Log($"[NetworkShell] ExplodeShell sent to server for shell {shellInfo.Id}");
                }
                else
                {
                    Debug.LogError("[NetworkShell] GameHubClient.Instance is null during OnTriggerEnter");
                }
            }
            else
            {
                Debug.Log($"[NetworkShell] OnTriggerEnter ignored: isOwn={isOwnShell}, hasExploded={hasExploded}");
            }
        }

        /// <summary>
        /// シェル情報を取得
        /// </summary>
        public ShellInfo GetShellInfo()
        {
            return shellInfo;
        }

        /// <summary>
        /// 自分のシェルかどうかを取得
        /// </summary>
        public bool IsOwnShell()
        {
            return isOwnShell;
        }
    }
}