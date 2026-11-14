using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Clashura.Characters.Data;
using Clashura.Characters.Preview;
using Clashura.CharacterSelection.Managers;
using Clashura.CharacterSelection.SaveSystem;

namespace Clashura.CharacterSelection.UI
{
    /// <summary>
    /// Master UI script for the 2D Character Selection Screen.
    /// Handles 2D artwork presentation with smooth transitions, 
    /// character navigation, abilities bar, and 3-character team selection slots.
    /// </summary>
    public class CharacterSelectionUI : MonoBehaviour
    {
        [Header("Manager References")]
        [SerializeField] private CharacterSelectionManager selectionManager;
        [SerializeField] private CharacterVisual2DController visual2DController;

        [Header("Top Header")]
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI characterTitleText;
        [SerializeField] private TextMeshProUGUI characterRoleText;
        [SerializeField] private TextMeshProUGUI characterCounterText;

        [Header("Navigation Buttons")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        [Header("Character Selection Actions")]
        [SerializeField] private Button toggleSelectButton;
        [SerializeField] private TextMeshProUGUI toggleSelectButtonText;
        [SerializeField] private Image toggleSelectButtonImage;
        [SerializeField] private Color selectColor = new Color(0.2f, 0.7f, 0.3f, 1f);
        [SerializeField] private Color deselectColor = new Color(0.8f, 0.25f, 0.25f, 1f);

        [Header("Combat Stats")]
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI attackText;
        [SerializeField] private TextMeshProUGUI defenseText;
        [SerializeField] private TextMeshProUGUI speedText;

        [Header("Abilities Section")]
        [SerializeField] private Transform abilitiesContainer;
        [SerializeField] private AbilitySlotUI abilitySlotPrefab;
        private readonly List<AbilitySlotUI> _spawnedAbilitySlots = new();

        [Header("Selected Team (3 Slots)")]
        [SerializeField] private List<SelectedSlotUI> selectedSlots = new();
        [SerializeField] private Button confirmTeamButton;
        [SerializeField] private TextMeshProUGUI teamStatusText;

        [Header("Feedback Banner / Toast")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private float feedbackDuration = 3f;
        private float _feedbackTimer = 0f;

        private void Awake()
        {
            HookButtons();
        }

        private void Start()
        {
            if (selectionManager != null && selectionManager.CurrentCharacter != null)
            {
                HandleCurrentCharacterChanged(selectionManager.CurrentCharacter);
            }
        }

        private void OnEnable()
        {
            if (selectionManager != null)
            {
                selectionManager.OnCurrentCharacterChanged += HandleCurrentCharacterChanged;
                selectionManager.OnSelectionsUpdated += HandleSelectionsUpdated;
                selectionManager.OnSelectionError += HandleSelectionError;
                selectionManager.OnSelectionConfirmed += HandleSelectionConfirmed;
            }
        }

        private void OnDisable()
        {
            if (selectionManager != null)
            {
                selectionManager.OnCurrentCharacterChanged -= HandleCurrentCharacterChanged;
                selectionManager.OnSelectionsUpdated -= HandleSelectionsUpdated;
                selectionManager.OnSelectionError -= HandleSelectionError;
                selectionManager.OnSelectionConfirmed -= HandleSelectionConfirmed;
            }
        }

        private void Update()
        {
            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f && feedbackBanner != null)
                {
                    feedbackBanner.SetActive(false);
                }
            }
        }

        private void HookButtons()
        {
            if (prevButton != null)
                prevButton.onClick.AddListener(() => selectionManager?.PreviousCharacter());

            if (nextButton != null)
                nextButton.onClick.AddListener(() => selectionManager?.NextCharacter());

            if (toggleSelectButton != null)
                toggleSelectButton.onClick.AddListener(() => selectionManager?.ToggleCurrentCharacterSelection());

            if (confirmTeamButton != null)
                confirmTeamButton.onClick.AddListener(() => selectionManager?.ConfirmSelection());

            for (int i = 0; i < selectedSlots.Count; i++)
            {
                int index = i;
                selectedSlots[i].SetSlotIndex(index);
                selectedSlots[i].OnRemoveClicked += (idx) => selectionManager?.DeselectSlot(idx);
                selectedSlots[i].OnSlotClicked += (idx) =>
                {
                    var bound = selectedSlots[idx].BoundCharacter;
                    if (bound != null)
                    {
                        selectionManager?.SelectCharacterById(bound.CharacterId);
                    }
                };
            }
        }

        private void HandleCurrentCharacterChanged(CharacterDataSO character)
        {
            if (character == null) return;

            // 1. Update text headers
            if (characterNameText != null) characterNameText.text = character.CharacterName;
            if (characterTitleText != null) characterTitleText.text = character.CharacterTitle;
            if (characterRoleText != null) characterRoleText.text = character.Role.ToString().ToUpper();

            if (characterCounterText != null && selectionManager != null && selectionManager.Database != null)
            {
                characterCounterText.text = $"{selectionManager.CurrentBrowsingIndex + 1} / {selectionManager.Database.CharacterCount}";
            }

            // 2. Update Stats
            if (healthText != null) healthText.text = $"HP: {character.BaseStats.MaxHealth}";
            if (attackText != null) attackText.text = $"ATK: {character.BaseStats.Attack}";
            if (defenseText != null) defenseText.text = $"DEF: {character.BaseStats.Defense}";
            if (speedText != null) speedText.text = $"SPD: {character.BaseStats.Speed}";

            // 3. Update 2D Selection Artwork with smooth transition
            if (visual2DController != null)
            {
                visual2DController.DisplayCharacterArtwork(character.SelectionArtwork);
            }

            // 4. Update Abilities
            UpdateAbilities(character.Abilities);

            // 5. Update Selection Toggle Button State
            UpdateToggleButtonState(character.CharacterId);
        }

        private void UpdateAbilities(IReadOnlyList<AbilityDataSO> abilities)
        {
            if (abilitiesContainer == null) return;

            int count = abilities != null ? abilities.Count : 0;

            while (_spawnedAbilitySlots.Count < count)
            {
                if (abilitySlotPrefab != null)
                {
                    var slot = Instantiate(abilitySlotPrefab, abilitiesContainer);
                    _spawnedAbilitySlots.Add(slot);
                }
                else
                {
                    break;
                }
            }

            for (int i = 0; i < _spawnedAbilitySlots.Count; i++)
            {
                if (i < count)
                {
                    _spawnedAbilitySlots[i].gameObject.SetActive(true);
                    _spawnedAbilitySlots[i].Bind(abilities[i]);
                }
                else
                {
                    _spawnedAbilitySlots[i].gameObject.SetActive(false);
                }
            }
        }

        private void HandleSelectionsUpdated(IReadOnlyList<CharacterDataSO> selectedCharacters)
        {
            int selectedCount = selectedCharacters != null ? selectedCharacters.Count : 0;

            for (int i = 0; i < selectedSlots.Count; i++)
            {
                if (i < selectedCount)
                {
                    selectedSlots[i].Bind(selectedCharacters[i]);
                }
                else
                {
                    selectedSlots[i].Bind(null);
                }
            }

            if (teamStatusText != null)
            {
                teamStatusText.text = $"Selected: {selectedCount} / {CharacterSelectionManager.RequiredSelectionCount}";
            }

            if (confirmTeamButton != null)
            {
                confirmTeamButton.interactable = (selectedCount == CharacterSelectionManager.RequiredSelectionCount);
            }

            if (selectionManager != null && selectionManager.CurrentCharacter != null)
            {
                UpdateToggleButtonState(selectionManager.CurrentCharacter.CharacterId);
            }
        }

        private void UpdateToggleButtonState(string characterId)
        {
            if (toggleSelectButton == null || selectionManager == null) return;

            bool isSelected = selectionManager.IsCharacterSelected(characterId);

            if (toggleSelectButtonText != null)
            {
                toggleSelectButtonText.text = isSelected ? "REMOVE FROM TEAM" : "SELECT FOR TEAM";
            }

            if (toggleSelectButtonImage != null)
            {
                toggleSelectButtonImage.color = isSelected ? deselectColor : selectColor;
            }
        }

        private void HandleSelectionError(string message)
        {
            ShowFeedback(message, true);
        }

        private void HandleSelectionConfirmed(PlayerSelectionData data)
        {
            ShowFeedback($"Team Confirmed & Saved! ({data.SelectedCharacterIds.Count} Heroes)", false);
        }

        private void ShowFeedback(string message, bool isError)
        {
            if (feedbackBanner != null && feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = isError ? new Color(1f, 0.35f, 0.35f) : new Color(0.4f, 1f, 0.4f);
                feedbackBanner.SetActive(true);
                _feedbackTimer = feedbackDuration;
            }
            else
            {
                Debug.Log($"[SelectionUI] Feedback: {message}");
            }
        }
    }
}
