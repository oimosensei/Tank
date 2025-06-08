using UnityEngine;
using System;
using System.Collections.Generic;

namespace Nakatani
{
    public class ShellManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField]
        private GameObject shellPrefab; // ネットワーク対応シェルPrefab

        [Header("Shell Settings")]
        [SerializeField]
        private float syncUpdateInterval = 0.1f; // シェル位置同期間隔

        // アクティブなシェルを管理するディクショナリー
        private Dictionary<Guid, GameObject> activeShells = new Dictionary<Guid, GameObject>();
        private Dictionary<Guid, ShellInfo> shellInfos = new Dictionary<Guid, ShellInfo>();

        // シングルトンインスタンス
        public static ShellManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// ネットワークからのシェル発射通知を受信してシェルを生成
        /// </summary>
        /// <param name="shellInfo">シェル情報</param>
        public void SpawnShell(ShellInfo shellInfo)
        {
            Debug.Log($"[ShellManager] SpawnShell called: {shellInfo.Id} by {shellInfo.ShooterId} at {shellInfo.Position}");
            
            if (activeShells.ContainsKey(shellInfo.Id))
            {
                Debug.LogWarning($"[ShellManager] Shell with ID {shellInfo.Id} already exists. Ignoring spawn request.");
                return;
            }

            if (shellPrefab == null)
            {
                Debug.LogError("[ShellManager] Shell Prefab is not assigned in ShellManager!");
                return;
            }

            GameObject newShell = Instantiate(shellPrefab, shellInfo.Position, shellInfo.Rotation);
            newShell.name = $"Shell_{shellInfo.Id.ToString().Substring(0, 8)}";
            Debug.Log($"[ShellManager] Shell GameObject created: {newShell.name}");

            // Rigidbodyを取得して初期速度を設定
            Rigidbody shellRb = newShell.GetComponent<Rigidbody>();
            if (shellRb != null)
            {
                shellRb.velocity = shellInfo.Velocity;
                Debug.Log($"[ShellManager] Shell Rigidbody velocity set to: {shellInfo.Velocity}");
            }
            else
            {
                Debug.LogError($"[ShellManager] Rigidbody not found on shell {shellInfo.Id}");
            }

            // ネットワークシェルコンポーネントを取得して初期化
            NetworkShell networkShell = newShell.GetComponent<NetworkShell>();
            if (networkShell != null)
            {
                networkShell.Initialize(shellInfo);
                Debug.Log($"[ShellManager] NetworkShell initialized for {shellInfo.Id}");
            }
            else
            {
                Debug.LogError($"[ShellManager] NetworkShell component not found on shell prefab for shell {shellInfo.Id}");
            }

            // ディクショナリーに追加
            activeShells.Add(shellInfo.Id, newShell);
            shellInfos.Add(shellInfo.Id, shellInfo);

            Debug.Log($"[ShellManager] Shell spawned successfully: {shellInfo.Id} by {shellInfo.ShooterId} at {shellInfo.Position}");
        }

        /// <summary>
        /// シェルの位置と速度を更新
        /// </summary>
        /// <param name="shellId">シェルID</param>
        /// <param name="position">新しい位置</param>
        /// <param name="velocity">新しい速度</param>
        public void UpdateShell(Guid shellId, Vector3 position, Vector3 velocity)
        {
            Debug.Log($"[ShellManager] UpdateShell called: {shellId} at {position} with velocity {velocity}");
            
            if (activeShells.TryGetValue(shellId, out GameObject shell))
            {
                NetworkShell networkShell = shell.GetComponent<NetworkShell>();
                if (networkShell != null)
                {
                    networkShell.UpdateFromNetwork(position, velocity);
                    Debug.Log($"[ShellManager] NetworkShell.UpdateFromNetwork called for {shellId}");
                }
                else
                {
                    // フォールバック: 直接位置と速度を設定
                    shell.transform.position = position;
                    Rigidbody rb = shell.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = velocity;
                    }
                    Debug.Log($"[ShellManager] Fallback position/velocity update for {shellId}");
                }

                // シェル情報を更新
                if (shellInfos.TryGetValue(shellId, out ShellInfo shellInfo))
                {
                    shellInfo.Position = position;
                    shellInfo.Velocity = velocity;
                }

                Debug.Log($"[ShellManager] Shell updated successfully: {shellId} at {position} with velocity {velocity}");
            }
            else
            {
                Debug.LogWarning($"[ShellManager] Shell {shellId} not found for update operation");
            }
        }

        /// <summary>
        /// シェルを爆発させて削除
        /// </summary>
        /// <param name="shellId">シェルID</param>
        /// <param name="explosionPosition">爆発位置</param>
        /// <param name="shooterId">発射者ID</param>
        public void ExplodeShell(Guid shellId, Vector3 explosionPosition, Guid shooterId)
        {
            Debug.Log($"[ShellManager] ExplodeShell called: {shellId} at {explosionPosition}, shot by {shooterId}");
            
            if (activeShells.TryGetValue(shellId, out GameObject shell))
            {
                Debug.Log($"[ShellManager] Shell found for explosion: {shellId}");
                
                NetworkShell networkShell = shell.GetComponent<NetworkShell>();
                if (networkShell != null)
                {
                    Debug.Log($"[ShellManager] Calling NetworkShell.ExplodeFromNetwork for {shellId}");
                    networkShell.ExplodeFromNetwork(explosionPosition, shooterId);
                }
                else
                {
                    // フォールバック: 直接爆発処理
                    Debug.LogWarning($"[ShellManager] NetworkShell not found, using fallback for {shellId}");
                    Destroy(shell);
                }

                // ディクショナリーから削除
                activeShells.Remove(shellId);
                shellInfos.Remove(shellId);

                Debug.Log($"[ShellManager] Shell exploded successfully: {shellId} at {explosionPosition}, shot by {shooterId}");
            }
            else
            {
                Debug.LogWarning($"[ShellManager] Shell {shellId} not found for explosion - may have already exploded");
            }
        }

        /// <summary>
        /// 指定されたシェルを取得
        /// </summary>
        /// <param name="shellId">シェルID</param>
        /// <returns>シェルのGameObject（存在しない場合はnull）</returns>
        public GameObject GetShell(Guid shellId)
        {
            activeShells.TryGetValue(shellId, out GameObject shell);
            return shell;
        }

        /// <summary>
        /// 指定されたシェルの情報を取得
        /// </summary>
        /// <param name="shellId">シェルID</param>
        /// <returns>シェル情報（存在しない場合はnull）</returns>
        public ShellInfo GetShellInfo(Guid shellId)
        {
            shellInfos.TryGetValue(shellId, out ShellInfo shellInfo);
            return shellInfo;
        }

        /// <summary>
        /// 全てのアクティブなシェルを削除
        /// </summary>
        public void DestroyAllShells()
        {
            foreach (var shell in activeShells.Values)
            {
                if (shell != null)
                {
                    Destroy(shell);
                }
            }
            activeShells.Clear();
            shellInfos.Clear();
            Debug.Log("All shells destroyed");
        }

        /// <summary>
        /// アクティブなシェルの数を取得
        /// </summary>
        public int GetActiveShellCount()
        {
            return activeShells.Count;
        }

        /// <summary>
        /// 全てのアクティブなシェルのIDを取得
        /// </summary>
        public Guid[] GetAllShellIds()
        {
            Guid[] ids = new Guid[activeShells.Count];
            activeShells.Keys.CopyTo(ids, 0);
            return ids;
        }
    }
}