using UnityEngine;

namespace Nakatani
{
    // プレイヤーの入力を検知し、Modelに伝えるクラス
    public class TankInputController : MonoBehaviour
    {
        private TankModel m_Model;
        private string m_MovementAxisName;
        private string m_TurnAxisName;
        private string m_FireButton;

        public void Initialize(TankModel model)
        {
            //todo ここ、player1専用なのでもう意味ないが、、
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
                m_Model.MovementInputValue.Value = 0f;
                m_Model.TurnInputValue.Value = 0f;
                return;
            }

            // 移動と回転の入力をModelに設定
            m_Model.MovementInputValue.Value = Input.GetAxis(m_MovementAxisName);
            m_Model.TurnInputValue.Value = Input.GetAxis(m_TurnAxisName);

            // 射撃の入力をModelに渡して処理させる
            m_Model.HandleShootingInput(
                Input.GetButtonDown(m_FireButton),
                Input.GetButton(m_FireButton),
                Input.GetButtonUp(m_FireButton)
            );
        }
    }
}