using System;
using UnityEngine;
using UniRx;
using System.Collections.Generic;

namespace Nakatani
{
    [Serializable]
    public class TankInitializer : MonoBehaviour
    {
        public Color m_PlayerColor;
        public Transform m_SpawnPoint;
        [HideInInspector] public int m_PlayerNumber = 1;
        [HideInInspector] public string m_ColoredPlayerText;
        [HideInInspector] public GameObject m_Instance;
        [HideInInspector] public int m_Wins;

        public bool isSelf = true;

        public TankModel Model { get; private set; }

        [Header("Game Constants")]
        public GameConstants gameConstants;

        void Awake()
        {
        }

        [ContextMenu("Setup Self")]
        private void SetupSelf()
        {
            Setup(true);
        }

        [ContextMenu("Setup Other")]
        private void SetupOther()
        {
            Setup(false);
        }

        public void Setup(bool isSelf)
        {
            if (gameConstants == null)
            {
                Debug.LogError("GameConstants is not assigned in TankInitializer!");
                return;
            }

            //networkから生成されたものかどうか
            this.isSelf = isSelf;
            // Modelを生成
            Model = new TankModel(m_PlayerNumber, m_PlayerColor, gameConstants.StartingHealth);
            Model.isSelf = isSelf;
            Model.Wins.Value = m_Wins;
            m_ColoredPlayerText = Model.ColoredPlayerText.Value; // 初期値を取得
            m_Instance = this.gameObject;

            // 各コンポーネントにModelを注入して初期化
            // ここらへん、vcontainerとか使いたいが、、

            // === 共通コンポーネント（自分・他人共通） ===
            //ネットワークの時はいらない
            var inputController = m_Instance.GetComponent<TankInputController>();
            inputController.Initialize(Model);

            //AIもこれを通じて射撃を行う
            //networkの時はいらない
            var shootingController = m_Instance.GetComponent<TankShootingController>();
            shootingController.Initialize(inputController, gameConstants);

            //viewはいる
            m_Instance.GetComponent<TankView>().Initialize(Model);
            //ネットワークのときも、観戦のときに必要
            m_Instance.GetComponent<CameraSwitcher>().Initialize(isSelf);

            // === 移動制御コンポーネント ===
            if (isSelf)
            {
                // 自分のタンク：ローカル入力で制御
                var movementController = m_Instance.GetComponent<TankMovementController>();
                movementController.Initialize(inputController, gameConstants);

                // ネットワーク制御は無効化
                m_Instance.GetComponent<TankNetworkMovementController>().enabled = false;
            }
            else
            {
                // 他人のタンク：ネットワークデータで制御
                m_Instance.GetComponent<TankNetworkMovementController>().Initialize(Model);

                // ローカル制御は無効化
                m_Instance.GetComponent<TankMovementController>().enabled = false;
            }

            // === タレット制御コンポーネント ===
            if (isSelf)
            {
                // 自分のタンク：ローカル入力でタレット制御
                var turretRotator = m_Instance.GetComponent<TurretRotator>();
                turretRotator.Initialize(gameConstants);

                // ネットワークタレット制御は無効化
                m_Instance.GetComponent<NetworkTurretController>().enabled = false;
            }
            else
            {
                // 他人のタンク：ネットワークデータでタレット制御
                // ローカルタレット制御は無効化
                m_Instance.GetComponent<TurretRotator>().enabled = false;
            }

            // ModelのWinsプロパティを監視して、Managerのm_Winsを更新し続ける
            Model.Wins.Subscribe(wins => m_Wins = wins).AddTo(m_Instance);

            EnableControl();
        }

        public void DisableControl()
        {
            Model.IsControlEnabled.Value = false;
        }

        public void EnableControl()
        {
            Model.IsControlEnabled.Value = true;
        }

        public void Reset()
        {
            m_Instance.transform.position = m_SpawnPoint.position;
            m_Instance.transform.rotation = m_SpawnPoint.rotation;

            m_Instance.SetActive(false);
            m_Instance.SetActive(true);

            Model.Reset();
            m_Instance.GetComponent<TankInputController>().Reset();
            m_Instance.GetComponent<TankShootingController>().Reset();
        }
    }
}