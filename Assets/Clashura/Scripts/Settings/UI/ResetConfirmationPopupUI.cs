using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Clashura.Settings.UI
{
    /// <summary>
    /// Themed confirmation modal dialog for resetting settings to defaults.
    /// </summary>
    public class ResetConfirmationPopupUI : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private GameObject darkOverlay;
        [SerializeField] private GameObject popupPanel;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI headingText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action _onConfirmed;

        private void Awake()
        {
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Close);
            }
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }
        }

        public void Show(Action onConfirmed)
        {
            _onConfirmed = onConfirmed;

            if (headingText != null)
            {
                headingText.text = "RESET ALL SETTINGS?";
                headingText.color = new Color(1.0f, 0.85f, 0.30f, 1f); // Radiant Gold
            }
            if (messageText != null)
            {
                messageText.text = "Are you sure you want to restore default settings?";
                messageText.color = Color.white;
            }
            if (subtitleText != null)
            {
                subtitleText.text = "All custom audio, graphics, controls and gameplay adjustments will be reverted.";
                subtitleText.color = new Color(0.75f, 0.85f, 0.98f, 0.95f);
            }

            gameObject.SetActive(true);
        }

        private void OnConfirmClicked()
        {
            _onConfirmed?.Invoke();
            Close();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
