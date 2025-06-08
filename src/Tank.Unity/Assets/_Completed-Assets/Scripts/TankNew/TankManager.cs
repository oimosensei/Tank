using UnityEngine;
using System;
using System.Collections.Generic;

namespace Nakatani
{
    public class TankManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField]
        private GameObject tankPrefab; // 通常のタンクPrefab

        [Header("Tank Settings")]
        [SerializeField]
        private Color[] playerColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow };

        [SerializeField]
        private int startingHealth = 100;

        [SerializeField]
        private float minLaunchForce = 15f;

        [SerializeField]
        private float maxLaunchForce = 30f;

        [SerializeField]
        private float maxChargeTime = 0.75f;

        // 場に出ているタンクを管理するディクショナリー
        [SerializeField]
        private Dictionary<Guid, GameObject> tanks = new Dictionary<Guid, GameObject>();

        // シングルトンインスタンス
        public static TankManager Instance { get; private set; }

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
        /// タンクをスポーンする
        /// </summary>
        /// <param name="playerId">プレイヤーのGuid</param>
        /// <param name="position">スポーン位置</param>
        /// <param name="rotation">スポーン回転</param>
        /// <param name="isSelf">自分自身かどうか</param>
        /// <param name="playerNumber">プレイヤー番号</param>
        /// <returns>生成されたタンクのGameObject</returns>
        public GameObject SpawnTank(Guid playerId, Vector3 position, Quaternion rotation, bool isSelf, int playerNumber = 1)
        {
            if (tanks.ContainsKey(playerId))
            {
                Debug.LogWarning($"Tank with ID {playerId} already exists. Removing old one.");
                DestroyTank(playerId);
            }

            if (tankPrefab == null)
            {
                Debug.LogError("Tank Prefab is not assigned in TankManager!");
                return null;
            }

            GameObject newTank = Instantiate(tankPrefab, position, rotation);
            newTank.name = $"Tank_{playerId.ToString().Substring(0, 8)}{(isSelf ? "_SELF" : "")}";

            // TankInitializerコンポーネントを取得して初期化
            TankInitializer tankInitializer = newTank.GetComponent<TankInitializer>();
            if (tankInitializer != null)
            {
                // // プレイヤー色を設定（プレイヤー番号に基づいて）
                // Color playerColor = playerColors[playerNumber % playerColors.Length];

                // // TankInitializerのパラメータを設定
                // tankInitializer.m_PlayerNumber = playerNumber;
                // tankInitializer.m_PlayerColor = playerColor;
                // tankInitializer.m_StartingHealth = startingHealth;
                // tankInitializer.m_MinLaunchForce = minLaunchForce;
                // tankInitializer.m_MaxLaunchForce = maxLaunchForce;
                // tankInitializer.m_MaxChargeTime = maxChargeTime;
                // tankInitializer.isSelf = isSelf;

                // 初期化を実行
                tankInitializer.Setup(isSelf);
            }
            else
            {
                Debug.LogError($"TankInitializer component not found on tank prefab for player {playerId}");
            }

            // ディクショナリーに追加
            tanks.Add(playerId, newTank);

            Debug.Log($"Tank spawned for player {playerId} at position {position} with rotation {rotation}, IsSelf: {isSelf}");

            return newTank;
        }

        /// <summary>
        /// タンクをスポーンする（後方互換性のためのオーバーロード）
        /// </summary>
        /// <param name="playerId">プレイヤーのGuid</param>
        /// <param name="position">スポーン位置</param>
        /// <param name="isSelf">自分自身かどうか</param>
        /// <param name="playerNumber">プレイヤー番号</param>
        /// <returns>生成されたタンクのGameObject</returns>
        public GameObject SpawnTank(Guid playerId, Vector3 position, bool isSelf, int playerNumber = 1)
        {
            return SpawnTank(playerId, position, Quaternion.identity, isSelf, playerNumber);
        }

        /// <summary>
        /// 指定されたプレイヤーのタンクを削除する
        /// </summary>
        /// <param name="playerId">削除するプレイヤーのGuid</param>
        /// <returns>削除に成功したかどうか</returns>
        public bool DestroyTank(Guid playerId)
        {
            if (tanks.TryGetValue(playerId, out GameObject tank))
            {
                Destroy(tank);
                tanks.Remove(playerId);
                Debug.Log($"Tank destroyed for player {playerId}");
                return true;
            }
            else
            {
                Debug.LogWarning($"Tank not found for player {playerId}");
                return false;
            }
        }

        /// <summary>
        /// GameHubのOnMoveから呼び出される関数
        /// </summary>
        /// <param name="playerId">移動したプレイヤーのGuid</param>
        /// <param name="position">新しい位置</param>
        /// <param name="rotation">新しい回転</param>
        public void OnTankMove(Guid playerId, Vector3 position, Quaternion rotation)
        {
            if (tanks.TryGetValue(playerId, out GameObject tank))
            {
                // TODO: スムーズな移動のために補間処理を実装することを推奨
                // 例: tank.GetComponent<TankMovementController>()?.SetTargetPosition(position, rotation);
                tank.transform.position = position;
                tank.transform.rotation = rotation;
                // Debug.Log($"Tank {playerId} moved to {position} with rotation {rotation}");
            }
            else
            {
                Debug.LogWarning($"Tank {playerId} not found for move operation");
            }
        }

        /// <summary>
        /// 指定されたプレイヤーのタンクを取得する
        /// </summary>
        /// <param name="playerId">プレイヤーのGuid</param>
        /// <returns>タンクのGameObject（存在しない場合はnull）</returns>
        public GameObject GetTank(Guid playerId)
        {
            tanks.TryGetValue(playerId, out GameObject tank);
            return tank;
        }

        /// <summary>
        /// 全てのタンクを削除する
        /// </summary>
        public void DestroyAllTanks()
        {
            foreach (var tank in tanks.Values)
            {
                if (tank != null)
                {
                    Destroy(tank);
                }
            }
            tanks.Clear();
            Debug.Log("All tanks destroyed");
        }

        /// <summary>
        /// アクティブなタンクの数を取得
        /// </summary>
        public int GetActiveTankCount()
        {
            return tanks.Count;
        }

        /// <summary>
        /// 全てのアクティブなタンクのGuidを取得
        /// </summary>
        public Guid[] GetAllTankIds()
        {
            Guid[] ids = new Guid[tanks.Count];
            tanks.Keys.CopyTo(ids, 0);
            return ids;
        }
    }
}