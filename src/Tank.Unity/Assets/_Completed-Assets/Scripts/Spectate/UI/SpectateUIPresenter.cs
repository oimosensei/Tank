using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

namespace Nakatani
{
    public class SpectateUIPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject uiRoot;
        [SerializeField] private TextMeshProUGUI currentTankInfoText;
        [SerializeField] private Button nextPlayerButton;
        [SerializeField] private Button previousPlayerButton;

        void Start()
        {
            if (nextPlayerButton != null)
            {
                nextPlayerButton.OnClickAsObservable()
                    .Subscribe(_ => OnNextPlayerClicked())
                    .AddTo(this);
            }

            if (previousPlayerButton != null)
            {
                previousPlayerButton.OnClickAsObservable()
                    .Subscribe(_ => OnPreviousPlayerClicked())
                    .AddTo(this);
            }

            // 観戦中のプレイヤー名を購読して表示
            if (SpectateManager.Instance != null && currentTankInfoText != null)
            {
                SpectateManager.Instance.CurrentPlayerName
                    .Subscribe(playerName => UpdatePlayerNameDisplay(playerName))
                    .AddTo(this);
            }

            // 観戦状態に応じてUIの表示/非表示を制御
            if (SpectateManager.Instance != null && uiRoot != null)
            {
                SpectateManager.Instance.IsSpectating
                    .Subscribe(isSpectating => UpdateUIVisibility(isSpectating))
                    .AddTo(this);
            }
        }

        private void UpdatePlayerNameDisplay(string playerName)
        {
            if (currentTankInfoText != null)
            {
                if (string.IsNullOrEmpty(playerName))
                {
                    currentTankInfoText.text = "No Player Selected";
                }
                else
                {
                    currentTankInfoText.text = $"Spectating: {playerName}";
                }
            }
        }

        private void UpdateUIVisibility(bool isSpectating)
        {
            if (uiRoot != null)
            {
                uiRoot.SetActive(isSpectating);
                Debug.Log($"Spectate UI visibility set to: {isSpectating}");
            }
        }

        private void OnNextPlayerClicked()
        {
            if (SpectateManager.Instance != null)
            {
                SpectateManager.Instance.SpectateNextPlayer();
                Debug.Log("Next player button clicked");
            }
            else
            {
                Debug.LogError("SpectateManager.Instance is null");
            }
        }

        private void OnPreviousPlayerClicked()
        {
            if (SpectateManager.Instance != null)
            {
                SpectateManager.Instance.SpectatePreviousPlayer();
                Debug.Log("Previous player button clicked");
            }
            else
            {
                Debug.LogError("SpectateManager.Instance is null");
            }
        }
    }
}