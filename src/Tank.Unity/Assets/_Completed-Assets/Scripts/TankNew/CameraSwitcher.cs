using UnityEngine;

namespace Nakatani
{
    public class CameraSwitcher : MonoBehaviour
    {
        [Header("Camera Settings")]
        public GameObject m_TPSCamera; // Third Person View Camera
        public GameObject m_FPSCamera; // First Person View Camera

        [Header("Camera Switch Settings")]
        public KeyCode m_SwitchKey = KeyCode.Mouse1; // 右クリック

        // カメラモード
        public enum CameraMode
        {
            None, // No camera active
            TPS,  // Third Person
            FPS   // First Person
        }

        private CameraMode m_CurrentCameraMode = CameraMode.TPS;
        private bool isLocal = true;

        private void Start()
        {
            // 初期状態はTPSカメラをアクティブに
            SetCameraMode(CameraMode.TPS);
        }

        public void Initialize(bool isLocalPlayer)
        {
            isLocal = isLocalPlayer;
            if (!isLocal)
            {
                SetCameraMode(CameraMode.None);
            }
            else
            {
                SetCameraMode(CameraMode.TPS);
            }
        }

        private void Update()
        {
            // 右クリックでカメラ切り替え
            if (Input.GetKeyDown(m_SwitchKey))
            {
                SwitchCamera();
            }
        }

        private void SwitchCamera()
        {
            // 現在のモードを切り替え
            if (m_CurrentCameraMode == CameraMode.TPS)
            {
                SetCameraMode(CameraMode.FPS);
            }
            else
            {
                SetCameraMode(CameraMode.TPS);
            }
        }

        private void SetCameraMode(CameraMode mode)
        {
            m_CurrentCameraMode = mode;

            switch (mode)
            {
                case CameraMode.None:
                    // 両方のカメラを非アクティブに
                    if (m_TPSCamera != null) m_TPSCamera.SetActive(false);
                    if (m_FPSCamera != null) m_FPSCamera.SetActive(false);
                    break;

                case CameraMode.TPS:
                    // TPSカメラをアクティブに、FPSカメラを非アクティブに
                    if (m_TPSCamera != null) m_TPSCamera.SetActive(true);
                    if (m_FPSCamera != null) m_FPSCamera.SetActive(false);
                    break;

                case CameraMode.FPS:
                    // FPSカメラをアクティブに、TPSカメラを非アクティブに
                    if (m_TPSCamera != null) m_TPSCamera.SetActive(false);
                    if (m_FPSCamera != null) m_FPSCamera.SetActive(true);
                    break;
            }
        }

        // 外部からカメラモードを取得
        public CameraMode GetCurrentCameraMode()
        {
            return m_CurrentCameraMode;
        }

        // 外部からカメラモードを設定
        public void SetCameraMode(int modeIndex)
        {
            if (modeIndex >= 0 && modeIndex < System.Enum.GetValues(typeof(CameraMode)).Length)
            {
                SetCameraMode((CameraMode)modeIndex);
            }
        }

        // アクティブなカメラを取得
        public Camera GetActiveCamera()
        {
            switch (m_CurrentCameraMode)
            {
                case CameraMode.None:
                    return null;
                case CameraMode.TPS:
                    return m_TPSCamera != null ? m_TPSCamera.GetComponent<Camera>() : null;
                case CameraMode.FPS:
                    return m_FPSCamera != null ? m_FPSCamera.GetComponent<Camera>() : null;
                default:
                    return null;
            }
        }
    }
}