using UnityEngine;
using UniRx;
using System;

namespace Nakatani
{
    // プレイヤーの入力を検知し、リアクティブに公開するクラス
    public class TankInputController : MonoBehaviour
    {
        private TankModel m_Model;
        private string m_MovementAxisName;
        private string m_TurnAxisName;
        private string m_FireButton;

        // 入力値をリアクティブプロパティとして公開
        public ReactiveProperty<float> MovementInputValue { get; } = new ReactiveProperty<float>();
        public ReactiveProperty<float> TurnInputValue { get; } = new ReactiveProperty<float>();

        // 射撃関連の状態
        public ReactiveProperty<float> CurrentLaunchForce { get; private set; }
        public BoolReactiveProperty IsCharging { get; } = new BoolReactiveProperty(false);

        // 射撃イベント
        public ISubject<Unit> OnFire { get; } = new Subject<Unit>();

        // 射撃設定値
        private float m_MinLaunchForce;
        private float m_MaxLaunchForce;
        private float m_ChargeSpeed;
        private bool m_Fired = true; // 初期は発射不可状態

        // 設定値の読み取り専用プロパティ
        public float MinLaunchForce => m_MinLaunchForce;
        public float MaxLaunchForce => m_MaxLaunchForce;

        public void Initialize(TankModel model, float minLaunchForce, float maxLaunchForce, float maxChargeTime)
        {
            m_Model = model;
            m_MovementAxisName = "Vertical" + "1";
            m_TurnAxisName = "Horizontal" + "1";
            m_FireButton = "Fire" + "1";

            // 射撃設定の初期化
            m_MinLaunchForce = minLaunchForce;
            m_MaxLaunchForce = maxLaunchForce;
            m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / maxChargeTime;
            CurrentLaunchForce = new ReactiveProperty<float>(m_MinLaunchForce);
        }

        private void Update()
        {
            if (m_Model == null || m_Model.IsDead.Value) return;

            // 操作が無効なら入力を0にリセット
            if (!m_Model.IsControlEnabled.Value)
            {
                MovementInputValue.Value = 0f;
                TurnInputValue.Value = 0f;
                return;
            }

            // 移動と回転の入力を自身のプロパティに設定
            MovementInputValue.Value = Input.GetAxis(m_MovementAxisName);
            TurnInputValue.Value = Input.GetAxis(m_TurnAxisName);

            // 射撃の入力処理
            if (m_Model.isSelf)
            {
                HandleShootingInput(
                    Input.GetButtonDown(m_FireButton),
                    Input.GetButton(m_FireButton),
                    Input.GetButtonUp(m_FireButton)
                );
            }
        }

        private void HandleShootingInput(bool getButtonDown, bool getButton, bool getButtonUp)
        {
            if (!m_Model.IsControlEnabled.Value) return;

            // 最大までチャージされたら自動発射
            if (CurrentLaunchForce.Value >= m_MaxLaunchForce && !m_Fired)
            {
                Fire();
            }
            // 射撃ボタン押下
            else if (getButtonDown)
            {
                m_Fired = false;
                IsCharging.Value = true;
                CurrentLaunchForce.Value = m_MinLaunchForce;
            }
            // 射撃ボタン長押し中
            else if (getButton && !m_Fired)
            {
                CurrentLaunchForce.Value += m_ChargeSpeed * Time.deltaTime;
            }
            // 射撃ボタン解放
            else if (getButtonUp && !m_Fired)
            {
                Fire();
            }
        }

        private void Fire()
        {
            m_Fired = true;
            IsCharging.Value = false;
            OnFire.OnNext(Unit.Default);
            CurrentLaunchForce.Value = m_MinLaunchForce;
        }

        public void Reset()
        {
            MovementInputValue.Value = 0f;
            TurnInputValue.Value = 0f;
            CurrentLaunchForce.Value = m_MinLaunchForce;
            IsCharging.Value = false;
            m_Fired = true;
        }
    }
}