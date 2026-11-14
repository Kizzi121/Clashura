using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Clashura.Characters.Data;

namespace Clashura.CharacterSelection.UI
{
    /// <summary>
    /// Displays one of the 3 selected character team slots.
    /// Supports empty placeholder state, filled state, and remove/deselect action.
    /// </summary>
    public class SelectedSlotUI : MonoBehaviour
    {
        [Header("Slot Identity")]
        [SerializeField] private int slotIndex = 0;

        [Header("UI Visuals")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private GameObject filledStateRoot;
        [SerializeField] private GameObject emptyStateRoot;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button slotButton;

        public event Action<int> OnRemoveClicked;
        public event Action<int> OnSlotClicked;

        private CharacterDataSO _boundCharacter;
        public CharacterDataSO BoundCharacter => _boundCharacter;
        public bool IsFilled => _boundCharacter != null;

        private void Awake()
        {
            if (removeButton != null)
            {
                removeButton.onClick.AddListener(() => OnRemoveClicked?.Invoke(slotIndex));
            }

            if (slotButton != null)
            {
                slotButton.onClick.AddListener(() => OnSlotClicked?.Invoke(slotIndex));
            }
        }

        public void SetSlotIndex(int index)
        {
            slotIndex = index;
        }

        public void Bind(CharacterDataSO character)
        {
            _boundCharacter = character;

            if (character == null)
            {
                if (filledStateRoot != null) filledStateRoot.SetActive(false);
                if (emptyStateRoot != null) emptyStateRoot.SetActive(true);
                if (portraitImage != null) portraitImage.enabled = false;
                if (nameText != null) nameText.text = "Empty Slot";
                if (roleText != null) roleText.text = string.Empty;
                return;
            }

            if (filledStateRoot != null) filledStateRoot.SetActive(true);
            if (emptyStateRoot != null) emptyStateRoot.SetActive(false);

            if (nameText != null) nameText.text = character.CharacterName;
            if (roleText != null) roleText.text = character.Role.ToString();

            if (portraitImage != null)
            {
                if (character.Portrait != null)
                {
                    portraitImage.sprite = character.Portrait;
                    portraitImage.enabled = true;
                }
                else
                {
                    portraitImage.enabled = false;
                }
            }
        }
    }
}
