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

        [Header("Tank Properties")]
        public float m_StartingHealth = 100f;
        public float m_MinLaunchForce = 15f;
        public float m_MaxLaunchForce = 30f;
        public float m_MaxChargeTime = 0.75f;

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
            //networkから生成されたものかどうか
            this.isSelf = isSelf;
            // Modelを生成
            Model = new TankModel(m_PlayerNumber, m_PlayerColor, m_StartingHealth);
            Model.isSelf = isSelf;
            Model.Wins.Value = m_Wins;
            m_ColoredPlayerText = Model.ColoredPlayerText.Value; // 初期値を取得
            m_Instance = this.gameObject;

            // 各コンポーネントにModelを注入して初期化
            // ここらへん、vcontainerとか使いたいが、、

            var inputController = m_Instance.GetComponent<TankInputController>();
            inputController.Initialize(Model);

            var shootingController = m_Instance.GetComponent<TankShootingController>();
            shootingController.Initialize(inputController, m_MinLaunchForce, m_MaxLaunchForce, m_MaxChargeTime);

            m_Instance.GetComponent<TankView>().Initialize(Model);
            if (isSelf)
            {
                var movementController = m_Instance.GetComponent<TankMovementController>();
                movementController.Initialize(inputController);

                m_Instance.GetComponent<TankNetworkMovementController>().enabled = false;
            }
            else
            {
                m_Instance.GetComponent<TankNetworkMovementController>().Initialize(Model);
                m_Instance.GetComponent<TankMovementController>().enabled = false;
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