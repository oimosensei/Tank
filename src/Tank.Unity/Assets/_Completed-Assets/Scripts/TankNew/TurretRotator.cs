using UnityEngine;

namespace Nakatani
{
    public class TurretRotator : MonoBehaviour
    {
        [Header("Turret Settings")]
        public Transform m_TurretTransform;
        public float m_RotationSpeed = 50f;

        [Header("Vertical Angle Settings")]
        public Transform m_BarrelTransform; // 砲身の上下角度用
        public float m_VerticalRotationSpeed = 30f;
        public float m_MaxVerticalAngle = 20f; // 上下の最大角度制限

        [Header("Mouse Settings")]
        public bool m_LockCursorOnStart = true;

        // 初期角度を保存
        private float m_InitialVerticalAngle;
        private bool m_IsCursorLocked = false;

        private void Start()
        {
            // 初期の垂直角度を保存
            if (m_BarrelTransform != null)
            {
                m_InitialVerticalAngle = m_BarrelTransform.localEulerAngles.x;
                // 角度を-180〜180の範囲に正規化
                if (m_InitialVerticalAngle > 180f)
                    m_InitialVerticalAngle -= 360f;
            }

            // マウスカーソルをロック
            if (m_LockCursorOnStart)
            {
                LockCursor();
            }
        }

        private void Update()
        {
            // ESCキーでマウスカーソルのロック/アンロックを切り替え
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnlockCursor();
            }

            // カーソルがロックされている時のみ回転処理
            // if (!m_IsCursorLocked) return;

            if (m_TurretTransform == null) return;

            //todo InputControllerに処理を預ける
            // マウスの左右移動を取得（水平回転）
            float mouseX = Input.GetAxis("Mouse X");
            // Y軸周りに回転
            m_TurretTransform.Rotate(0, mouseX * m_RotationSpeed * Time.deltaTime, 0);

            // マウスの上下移動を取得（垂直回転）
            if (m_BarrelTransform != null)
            {
                float mouseY = Input.GetAxis("Mouse Y");

                // 現在の角度を取得
                float currentAngle = m_BarrelTransform.localEulerAngles.x;
                if (currentAngle > 180f)
                    currentAngle -= 360f;

                // 新しい角度を計算
                float newAngle = currentAngle - mouseY * m_VerticalRotationSpeed * Time.deltaTime;

                // 初期角度からの制限範囲内にクランプ
                float minAngle = m_InitialVerticalAngle - m_MaxVerticalAngle;
                float maxAngle = m_InitialVerticalAngle + m_MaxVerticalAngle;
                newAngle = Mathf.Clamp(newAngle, minAngle, maxAngle);

                // 角度を適用
                var currentRotation = m_BarrelTransform.localEulerAngles;
                m_BarrelTransform.localRotation = Quaternion.Euler(newAngle, currentRotation.y, 0);
            }
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            m_IsCursorLocked = true;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            m_IsCursorLocked = false;
        }

        private void ToggleCursorLock()
        {
            if (m_IsCursorLocked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }

        // アプリケーションフォーカス時の処理
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && !m_IsCursorLocked)
            {
                LockCursor();
            }
        }

    }
}