using UnityEngine;

namespace Nakatani
{
    /// <summary>
    /// ネットワーク経由での砲塔制御を管理するクラス
    /// </summary>
    public class NetworkTurretController : MonoBehaviour
    {
        [Header("Turret Network Settings")]
        public Transform m_TurretTransform;

        /// <summary>
        /// ネットワーク経由で砲塔の回転を設定する
        /// </summary>
        /// <param name="rotation">設定する回転</param>
        public void SetTurretRotation(Quaternion rotation)
        {
            if (m_TurretTransform != null)
            {
                m_TurretTransform.rotation = rotation;
            }
        }

        /// <summary>
        /// 現在の砲塔の回転を取得する
        /// </summary>
        /// <returns>現在の砲塔回転</returns>
        public Quaternion GetTurretRotation()
        {
            return m_TurretTransform != null ? m_TurretTransform.rotation : Quaternion.identity;
        }
    }
}