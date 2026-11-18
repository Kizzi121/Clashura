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

        [Header("Combat Stat Bars")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image attackBarFill;
        [SerializeField] private Image defenseBarFill;
        [SerializeField] private Image speedBarFill;

        [Header("Abilities Section")]
        [SerializeField] private Transform abilitiesContainer;
        [SerializeField] private AbilitySlotUI abilitySlotPrefab;
        private readonly List<AbilitySlotUI> _spawnedAbilitySlots = new();

        [Header("Selected Team (3 Slots)")]
        [SerializeField] private List<SelectedSlotUI> selectedSlots = new();
        [SerializeField] private Button confirmTeamButton;
        [SerializeField] private TextMeshProUGUI teamStatusText;

        [Header("Smart Team Selection & Status System")]
        [SerializeField] private GameObject statusPanelRoot;
        [SerializeField] private TextMeshProUGUI statusHeadingText;
        [SerializeField] private TextMeshProUGUI statusSubtitleText;
        [SerializeField] private Image statusIconImage;

        [Header("Confirmation Validation Popup")]
        [SerializeField] private ConfirmationPopupUI confirmationPopup;
        private bool _isConfirmingSuccess = false;

        [Header("Feedback Banner / Toast")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private float feedbackDuration = 3f;
        private float _feedbackTimer = 0f;

        [Header("Screen Navigation")]
        [SerializeField] private Button backButton;
        public event System.Action OnBackRequested;

        private void Awake()
        {
            if (selectionManager == null)
            {
                selectionManager = FindFirstObjectByType<CharacterSelectionManager>();
            }
            HookButtons();
        }

        private void Start()
        {
            if (selectionManager == null)
            {
                selectionManager = FindFirstObjectByType<CharacterSelectionManager>();
            }
            if (selectionManager != null && selectionManager.CurrentCharacter != null)
            {
                HandleCurrentCharacterChanged(selectionManager.CurrentCharacter);
            }
        }

        private void OnEnable()
        {
            if (selectionManager == null)
            {
                selectionManager = FindFirstObjectByType<CharacterSelectionManager>();
            }

            if (selectionManager != null)
            {
                selectionManager.OnCurrentCharacterChanged -= HandleCurrentCharacterChanged;
                selectionManager.OnSelectionsUpdated -= HandleSelectionsUpdated;
                selectionManager.OnSelectionError -= HandleSelectionError;
                selectionManager.OnSelectionConfirmed -= HandleSelectionConfirmed;

                selectionManager.OnCurrentCharacterChanged += HandleCurrentCharacterChanged;
                selectionManager.OnSelectionsUpdated += HandleSelectionsUpdated;
                selectionManager.OnSelectionError += HandleSelectionError;
                selectionManager.OnSelectionConfirmed += HandleSelectionConfirmed;
            }
            RefreshView();
        }

        public void RefreshView()
        {
            if (selectionManager == null)
            {
                selectionManager = FindFirstObjectByType<CharacterSelectionManager>();
            }

            if (selectionManager != null && selectionManager.CurrentCharacter != null)
            {
                HandleCurrentCharacterChanged(selectionManager.CurrentCharacter);
                var selected = new List<CharacterDataSO>();
                if (selectionManager.Database != null)
                {
                    foreach (var id in selectionManager.SelectedCharacterIds)
                    {
                        var cd = selectionManager.Database.GetCharacterById(id);
                        if (cd != null) selected.Add(cd);
                    }
                }
                HandleSelectionsUpdated(selected);
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
            if (selectionManager == null)
            {
                selectionManager = FindFirstObjectByType<CharacterSelectionManager>();
            }

            if (prevButton != null)
                prevButton.onClick.AddListener(() => selectionManager?.PreviousCharacter());

            if (nextButton != null)
                nextButton.onClick.AddListener(() => selectionManager?.NextCharacter());

            if (toggleSelectButton != null)
                toggleSelectButton.onClick.AddListener(() => selectionManager?.ToggleCurrentCharacterSelection());

            if (confirmTeamButton != null)
                confirmTeamButton.onClick.AddListener(HandleConfirmButtonClicked);

            if (backButton != null)
            {
                backButton.onClick.AddListener(() =>
                {
                    OnBackRequested?.Invoke();
                    if (Clashura.UI.Navigation.UINavigationController.Instance != null)
                    {
                        Clashura.UI.Navigation.UINavigationController.Instance.OpenMainMenu();
                    }
                });
            }

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

            if (healthBarFill != null) healthBarFill.fillAmount = Mathf.Clamp01(character.BaseStats.MaxHealth / 2000f);
            if (attackBarFill != null) attackBarFill.fillAmount = Mathf.Clamp01(character.BaseStats.Attack / 200f);
            if (defenseBarFill != null) defenseBarFill.fillAmount = Mathf.Clamp01(character.BaseStats.Defense / 200f);
            if (speedBarFill != null) speedBarFill.fillAmount = Mathf.Clamp01(character.BaseStats.Speed / 100f);

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

            // 1. Gather all existing AbilitySlotUI children under abilitiesContainer
            var existingSlots = abilitiesContainer.GetComponentsInChildren<AbilitySlotUI>(true);
            _spawnedAbilitySlots.Clear();
            _spawnedAbilitySlots.AddRange(existingSlots);

            // 2. Remove any stray children that are not AbilitySlotUI
            for (int i = abilitiesContainer.childCount - 1; i >= 0; i--)
            {
                var child = abilitiesContainer.GetChild(i);
                if (child.GetComponent<AbilitySlotUI>() == null)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }

            // 3. If we have more slots than the character's abilities, destroy the extras
            while (_spawnedAbilitySlots.Count > count)
            {
                int lastIdx = _spawnedAbilitySlots.Count - 1;
                var extra = _spawnedAbilitySlots[lastIdx];
                _spawnedAbilitySlots.RemoveAt(lastIdx);
                if (extra != null)
                {
                    extra.gameObject.SetActive(false);
                    extra.transform.SetParent(null);
                    if (Application.isPlaying) Destroy(extra.gameObject);
                    else DestroyImmediate(extra.gameObject);
                }
            }

            // 4. If we have fewer than needed, instantiate new ones from the prefab
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

            // 5. Bind each ability to its slot
            for (int i = 0; i < _spawnedAbilitySlots.Count; i++)
            {
                if (_spawnedAbilitySlots[i] == null) continue;

                if (i < count && abilities != null && abilities[i] != null)
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

            // Smart Selection Status Message System (4 States)
            switch (selectedCount)
            {
                case 0:
                    if (statusHeadingText != null)
                    {
                        statusHeadingText.text = "PLEASE SELECT YOUR CHARACTERS";
                        statusHeadingText.color = new Color(1.0f, 0.85f, 0.35f, 1f); // Radiant Gold
                    }
                    if (statusSubtitleText != null)
                    {
                        statusSubtitleText.text = "Choose 3 characters to build your team.";
                        statusSubtitleText.color = new Color(0.78f, 0.88f, 0.98f, 0.9f);
                    }
                    break;

                case 1:
                    if (statusHeadingText != null)
                    {
                        statusHeadingText.text = "2 MORE CHARACTERS REQUIRED";
                        statusHeadingText.color = new Color(0.35f, 0.85f, 1.0f, 1f); // Azure Energy
                    }
                    if (statusSubtitleText != null)
                    {
                        statusSubtitleText.text = "Select two more characters to complete your team.";
                        statusSubtitleText.color = new Color(0.78f, 0.88f, 0.98f, 0.9f);
                    }
                    break;

                case 2:
                    if (statusHeadingText != null)
                    {
                        statusHeadingText.text = "ONE MORE CHARACTER REQUIRED";
                        statusHeadingText.color = new Color(1.0f, 0.65f, 0.25f, 1f); // Warning Amber
                    }
                    if (statusSubtitleText != null)
                    {
                        statusSubtitleText.text = "Select your final character to complete your team.";
                        statusSubtitleText.color = new Color(0.78f, 0.88f, 0.98f, 0.9f);
                    }
                    break;

                case 3:
                default:
                    if (statusHeadingText != null)
                    {
                        statusHeadingText.text = "YOUR TEAM IS READY!";
                        statusHeadingText.color = new Color(0.30f, 1.0f, 0.55f, 1f); // Emerald Victory
                    }
                    if (statusSubtitleText != null)
                    {
                        statusSubtitleText.text = "All 3 characters selected. Confirm your team to continue.";
                        statusSubtitleText.color = new Color(0.95f, 0.98f, 1.0f, 0.95f);
                    }
                    break;
            }

            if (teamStatusText != null && teamStatusText != statusHeadingText)
            {
                teamStatusText.text = $"Selected: {selectedCount} / {CharacterSelectionManager.RequiredSelectionCount}";
            }

            // Confirm Button is always interactable so clicking it triggers the corresponding validation popup
            if (confirmTeamButton != null)
            {
                confirmTeamButton.interactable = true;
                bool isTeamReady = (selectedCount == CharacterSelectionManager.RequiredSelectionCount);
                var confAnim = confirmTeamButton.GetComponent<Clashura.UI.Animation.UIButtonAnimator>();
                if (confAnim != null) confAnim.EnablePulse = isTeamReady;
            }

            if (selectionManager != null && selectionManager.CurrentCharacter != null)
            {
                UpdateToggleButtonState(selectionManager.CurrentCharacter.CharacterId);
            }
        }

        private void HandleConfirmButtonClicked()
        {
            if (confirmationPopup != null && confirmationPopup.IsSuccessSequenceRunning) return;

            if (selectionManager == null)
            {
                selectionManager = FindFirstObjectByType<CharacterSelectionManager>();
            }

            int selectedCount = selectionManager != null ? selectionManager.SelectedCharacterIds.Count : 0;

            if (confirmationPopup == null)
            {
                confirmationPopup = GetComponentInChildren<ConfirmationPopupUI>(true);
            }

            if (confirmationPopup == null)
            {
                Debug.LogWarning("[CharacterSelectionUI] ConfirmationPopupUI reference missing!");
                return;
            }

            switch (selectedCount)
            {
                case 0:
                    confirmationPopup.ShowState0_ZeroSelected();
                    break;
                case 1:
                    confirmationPopup.ShowState1_OneSelected();
                    break;
                case 2:
                    confirmationPopup.ShowState2_TwoSelected();
                    break;
                case 3:
                default:
                    _isConfirmingSuccess = true;
                    // 1. Confirm and persist selections
                    selectionManager.ConfirmSelection();

                    // 2. Gather the 3 CharacterDataSO instances
                    var heroes = new List<CharacterDataSO>();
                    if (selectionManager.Database != null)
                    {
                        foreach (var id in selectionManager.SelectedCharacterIds)
                        {
                            var cd = selectionManager.Database.GetCharacterById(id);
                            if (cd != null) heroes.Add(cd);
                        }
                    }

                    // 3. Show State 3 with portraits, 5-second countdown and auto-return to Main Menu
                    confirmationPopup.ShowState3_ThreeSelected(heroes, () =>
                    {
                        _isConfirmingSuccess = false;
                        if (Clashura.UI.Navigation.UINavigationController.Instance != null)
                        {
                            Clashura.UI.Navigation.UINavigationController.Instance.OpenMainMenu();
                        }
                    });
                    break;
            }
        }

        private void UpdateToggleButtonState(string characterId)
        {
            if (toggleSelectButton == null || selectionManager == null) return;

            bool isSelected = selectionManager.IsCharacterSelected(characterId);

            if (toggleSelectButtonText != null)
            {
                toggleSelectButtonText.text = isSelected ? "REMOVE FROM TEAM" : "SELECT FOR TEAM";
                toggleSelectButtonText.color = isSelected ? new Color(1.0f, 0.40f, 0.40f, 1f) : new Color(1.0f, 0.85f, 0.30f, 1f);
            }

            if (toggleSelectButtonImage != null)
            {
                toggleSelectButtonImage.color = isSelected ? new Color(0.9f, 0.5f, 0.5f, 1f) : Color.white;
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
