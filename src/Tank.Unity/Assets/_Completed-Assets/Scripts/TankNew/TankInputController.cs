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

        // 射撃入力の状態（押下状況のみ）
        public BoolReactiveProperty IsFireButtonDown { get; } = new BoolReactiveProperty(false);
        public BoolReactiveProperty IsFireButtonHeld { get; } = new BoolReactiveProperty(false);
        public BoolReactiveProperty IsFireButtonUp { get; } = new BoolReactiveProperty(false);

        public void Initialize(TankModel model)
        {
            m_Model = model;
            m_MovementAxisName = "Vertical" + "1";
            m_TurnAxisName = "Horizontal" + "1";
            m_FireButton = "Fire" + "1";
        }

        private void Update()
        {
            if (m_Model == null || m_Model.IsDead.Value) return;

            // 操作が無効なら入力を0にリセット
            if (!m_Model.IsControlEnabled.Value)
            {
                MovementInputValue.Value = 0f;
                TurnInputValue.Value = 0f;
                IsFireButtonDown.Value = false;
                IsFireButtonHeld.Value = false;
                IsFireButtonUp.Value = false;
                return;
            }

            // 移動と回転の入力を自身のプロパティに設定
            MovementInputValue.Value = Input.GetAxis(m_MovementAxisName);
            TurnInputValue.Value = Input.GetAxis(m_TurnAxisName);

            // 射撃入力の状態を更新（自分のタンクの場合のみ）
            if (m_Model.isSelf)
            {
                IsFireButtonDown.Value = Input.GetButtonDown(m_FireButton);
                // ボタン押下状態を、値が変わっていなくても更新
                IsFireButtonHeld.SetValueAndForceNotify(Input.GetButton(m_FireButton));
                IsFireButtonUp.Value = Input.GetButtonUp(m_FireButton);
            }
            else
            {
                IsFireButtonDown.Value = false;
                IsFireButtonHeld.Value = false;
                IsFireButtonUp.Value = false;
            }
        }

        public void Reset()
        {
            MovementInputValue.Value = 0f;
            TurnInputValue.Value = 0f;
            IsFireButtonDown.Value = false;
            IsFireButtonHeld.Value = false;
            IsFireButtonUp.Value = false;
        }
    }
}