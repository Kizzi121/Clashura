using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Clashura.Characters.Data;

namespace Clashura.CharacterSelection.UI
{
    /// <summary>
    /// Reusable confirmation popup system for CLASHURA character selection.
    /// Handles all 4 validation states (0 heroes, 1 hero, 2 heroes, and 3 heroes selected).
    /// Provides 5-second animated countdown and automatic return to Main Menu upon success.
    /// </summary>
    public class ConfirmationPopupUI : MonoBehaviour
    {
        [Header("Popup Containers")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private GameObject darkOverlay;

        [Header("Header Elements")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI headingText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Action Elements")]
        [SerializeField] private Button okButton;
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Success Heroes Display")]
        [SerializeField] private GameObject heroesContainer;
        [SerializeField] private Image[] heroPortraitImages;
        [SerializeField] private TextMeshProUGUI[] heroNameTexts;

        [Header("Themed Icons")]
        [SerializeField] private Sprite warningIcon;
        [SerializeField] private Sprite successIcon;

        private Coroutine _countdownCoroutine;
        private bool _isSuccessSequenceRunning = false;

        public bool IsSuccessSequenceRunning => _isSuccessSequenceRunning;

        private void Awake()
        {
            if (okButton != null)
            {
                okButton.onClick.AddListener(Close);
            }
        }

        private void OnDisable()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
            _isSuccessSequenceRunning = false;
        }

        /// <summary>
        /// STATE 0 — Zero characters selected when Confirm is clicked.
        /// </summary>
        public void ShowState0_ZeroSelected()
        {
            if (_isSuccessSequenceRunning) return;

            StopActiveCountdown();
            SetupWarningLayout();

            if (headingText != null)
            {
                headingText.text = "TEAM NOT SELECTED!";
                headingText.color = new Color(1.0f, 0.85f, 0.30f, 1f); // Radiant Gold
            }
            if (messageText != null)
            {
                messageText.text = "Please select your team first.";
                messageText.color = Color.white;
            }
            if (subtitleText != null)
            {
                subtitleText.text = "Choose 3 characters before continuing.";
                subtitleText.color = new Color(0.75f, 0.85f, 0.98f, 0.95f);
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// STATE 1 — One character selected when Confirm is clicked.
        /// </summary>
        public void ShowState1_OneSelected()
        {
            if (_isSuccessSequenceRunning) return;

            StopActiveCountdown();
            SetupWarningLayout();

            if (headingText != null)
            {
                headingText.text = "INCOMPLETE TEAM!";
                headingText.color = new Color(0.35f, 0.85f, 1.0f, 1f); // Azure Energy
            }
            if (messageText != null)
            {
                messageText.text = "You have selected only 1 character.";
                messageText.color = Color.white;
            }
            if (subtitleText != null)
            {
                subtitleText.text = "Please select 2 more characters to continue.";
                subtitleText.color = new Color(0.75f, 0.85f, 0.98f, 0.95f);
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// STATE 2 — Two characters selected when Confirm is clicked.
        /// </summary>
        public void ShowState2_TwoSelected()
        {
            if (_isSuccessSequenceRunning) return;

            StopActiveCountdown();
            SetupWarningLayout();

            if (headingText != null)
            {
                headingText.text = "ALMOST READY!";
                headingText.color = new Color(1.0f, 0.65f, 0.25f, 1f); // Warning Amber
            }
            if (messageText != null)
            {
                messageText.text = "You have selected 2 out of 3 characters.";
                messageText.color = Color.white;
            }
            if (subtitleText != null)
            {
                subtitleText.text = "Please select 1 more character to complete your team.";
                subtitleText.color = new Color(0.75f, 0.85f, 0.98f, 0.95f);
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// STATE 3 — Exactly three unique characters selected.
        /// Shows portraits, success indicator, runs 5-second timer, and returns to Main Menu.
        /// </summary>
        public void ShowState3_ThreeSelected(IReadOnlyList<CharacterDataSO> selectedHeroes, Action onCompleteReturnToMainMenu)
        {
            if (_isSuccessSequenceRunning) return;
            _isSuccessSequenceRunning = true;

            StopActiveCountdown();

            if (iconImage != null && successIcon != null)
            {
                iconImage.sprite = successIcon;
                iconImage.color = Color.white;
            }

            if (headingText != null)
            {
                headingText.text = "TEAM SUCCESSFULLY SELECTED!";
                headingText.color = new Color(0.30f, 1.0f, 0.55f, 1f); // Emerald Victory
            }
            if (messageText != null)
            {
                messageText.text = "Congratulations! Your team is ready.";
                messageText.color = Color.white;
            }
            if (subtitleText != null)
            {
                subtitleText.text = "All 3 characters have been successfully selected.";
                subtitleText.color = new Color(0.85f, 0.95f, 1.0f, 0.95f);
            }

            // Bind 3 hero portraits and names
            if (heroesContainer != null)
            {
                heroesContainer.SetActive(true);
            }

            int count = selectedHeroes != null ? selectedHeroes.Count : 0;
            for (int i = 0; i < 3; i++)
            {
                if (heroPortraitImages != null && i < heroPortraitImages.Length && heroPortraitImages[i] != null)
                {
                    if (i < count && selectedHeroes[i] != null && selectedHeroes[i].SelectionArtwork != null)
                    {
                        heroPortraitImages[i].sprite = selectedHeroes[i].SelectionArtwork;
                        heroPortraitImages[i].enabled = true;
                    }
                    else
                    {
                        heroPortraitImages[i].enabled = false;
                    }
                }

                if (heroNameTexts != null && i < heroNameTexts.Length && heroNameTexts[i] != null)
                {
                    heroNameTexts[i].text = (i < count && selectedHeroes[i] != null) ? selectedHeroes[i].CharacterName : "";
                }
            }

            // Hide OK button in success state; show live countdown
            if (okButton != null)
            {
                okButton.gameObject.SetActive(false);
            }

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
            }

            gameObject.SetActive(true);

            // Start 5-second countdown to return to Main Menu
            _countdownCoroutine = StartCoroutine(SuccessCountdownRoutine(5, onCompleteReturnToMainMenu));
        }

        private IEnumerator SuccessCountdownRoutine(int totalSeconds, Action onComplete)
        {
            for (int remaining = totalSeconds; remaining > 0; remaining--)
            {
                if (countdownText != null)
                {
                    countdownText.text = $"Returning to Main Menu in {remaining}s...";
                }
                yield return new WaitForSecondsRealtime(1.0f);
            }

            if (countdownText != null)
            {
                countdownText.text = "Returning to Main Menu...";
            }

            yield return new WaitForSecondsRealtime(0.25f);

            _isSuccessSequenceRunning = false;
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        private void SetupWarningLayout()
        {
            if (iconImage != null && warningIcon != null)
            {
                iconImage.sprite = warningIcon;
                iconImage.color = Color.white;
            }

            if (heroesContainer != null)
            {
                heroesContainer.SetActive(false);
            }

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
            }

            if (okButton != null)
            {
                okButton.gameObject.SetActive(true);
            }
        }

        private void StopActiveCountdown()
        {
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
        }

        public void Close()
        {
            if (_isSuccessSequenceRunning) return; // Prevent dismissing during success countdown

            StopActiveCountdown();
            gameObject.SetActive(false);
        }
    }
}
