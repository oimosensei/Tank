using UnityEngine;

namespace Nakatani
{
    public class TurretRotator : MonoBehaviour
    {
        [Header("Turret Settings")]
        public Transform m_TurretTransform;
        public float m_RotationSpeed = 50f;
        
        private void Update()
        {
            if (m_TurretTransform == null) return;
            
            // マウスの左右移動を取得
            float mouseX = Input.GetAxis("Mouse X");
            
            // Y軸周りに回転
            m_TurretTransform.Rotate(0, mouseX * m_RotationSpeed * Time.deltaTime, 0);
        }
    }
}