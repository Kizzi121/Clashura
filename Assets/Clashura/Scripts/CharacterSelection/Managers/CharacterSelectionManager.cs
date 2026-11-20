using System;
using System.Collections.Generic;
using UnityEngine;
using Clashura.Characters.Data;
using Clashura.Characters.Managers;
using Clashura.CharacterSelection.SaveSystem;

namespace Clashura.CharacterSelection.Managers
{
    /// <summary>
    /// Master coordinator for character browsing, 3-character team selection, duplicate validation,
    /// and persistent save/load operations.
    /// </summary>
    public class CharacterSelectionManager : MonoBehaviour
    {
        public const int RequiredSelectionCount = 3;

        [Header("Data References")]
        [SerializeField] private CharacterDatabaseSO characterDatabase;
        [SerializeField] private string localPlayerId = "Player_Local";

        [Header("Auto Load Settings")]
        [SerializeField] private bool autoLoadOnStart = true;

        // Current navigation state
        private int _currentBrowsingIndex = 0;
        private readonly List<string> _selectedCharacterIds = new(RequiredSelectionCount);
        private ISelectionSaveSystem _saveSystem;

        // Public Events
        public event Action<CharacterDataSO> OnCurrentCharacterChanged;
        public event Action<IReadOnlyList<CharacterDataSO>> OnSelectionsUpdated;
        public event Action<string> OnSelectionError;
        public event Action<PlayerSelectionData> OnSelectionConfirmed;

        public CharacterDatabaseSO Database => characterDatabase;
        public int CurrentBrowsingIndex => _currentBrowsingIndex;
        public CharacterDataSO CurrentCharacter => characterDatabase != null ? characterDatabase.GetCharacterByIndex(_currentBrowsingIndex) : null;
        public IReadOnlyList<string> SelectedCharacterIds => _selectedCharacterIds;
        public bool IsTeamFull => _selectedCharacterIds.Count >= RequiredSelectionCount;

        private void Awake()
        {
            _saveSystem = new PlayerPrefsSelectionSaveSystem();
        }

        private void Start()
        {
            if (characterDatabase == null)
            {
                Debug.LogError("[CharacterSelectionManager] CharacterDatabaseSO is not assigned!", this);
                return;
            }

            if (_saveSystem == null)
            {
                _saveSystem = new PlayerPrefsSelectionSaveSystem();
            }

            if (autoLoadOnStart || _selectedCharacterIds.Count == 0)
            {
                LoadSavedSelections();
            }

            // Broadcast initial character
            BroadcastCurrentCharacter();
            BroadcastSelections();
        }

        public void SetSaveSystem(ISelectionSaveSystem customSaveSystem)
        {
            _saveSystem = customSaveSystem ?? new PlayerPrefsSelectionSaveSystem();
        }

        #region Navigation

        public void NextCharacter()
        {
            if (characterDatabase == null || characterDatabase.CharacterCount == 0) return;
            _currentBrowsingIndex = (_currentBrowsingIndex + 1) % characterDatabase.CharacterCount;
            BroadcastCurrentCharacter();
        }

        public void PreviousCharacter()
        {
            if (characterDatabase == null || characterDatabase.CharacterCount == 0) return;
            _currentBrowsingIndex = (_currentBrowsingIndex - 1 + characterDatabase.CharacterCount) % characterDatabase.CharacterCount;
            BroadcastCurrentCharacter();
        }

        public void SelectCharacterByIndex(int index)
        {
            if (characterDatabase == null || index < 0 || index >= characterDatabase.CharacterCount) return;
            _currentBrowsingIndex = index;
            BroadcastCurrentCharacter();
        }

        public void SelectCharacterById(string characterId)
        {
            if (characterDatabase == null) return;
            int idx = characterDatabase.IndexOfId(characterId);
            if (idx >= 0)
            {
                _currentBrowsingIndex = idx;
                BroadcastCurrentCharacter();
            }
        }

        #endregion

        #region Team Selection Logic

        public bool IsCharacterSelected(string characterId)
        {
            return _selectedCharacterIds.Contains(characterId);
        }

        public bool ToggleCurrentCharacterSelection()
        {
            var current = CurrentCharacter;
            if (current == null) return false;

            if (IsCharacterSelected(current.CharacterId))
            {
                return DeselectCharacter(current.CharacterId);
            }
            else
            {
                return SelectCharacter(current.CharacterId);
            }
        }

        public bool SelectCharacter(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return false;

            // 1. Prevent duplicate selection
            if (_selectedCharacterIds.Contains(characterId))
            {
                OnSelectionError?.Invoke($"'{GetCharacterName(characterId)}' is already in your selected team!");
                return false;
            }

            // 2. Prevent exceeding 3 characters
            if (_selectedCharacterIds.Count >= RequiredSelectionCount)
            {
                OnSelectionError?.Invoke($"Team is full! You can only select {RequiredSelectionCount} characters. Deselect one first.");
                return false;
            }

            _selectedCharacterIds.Add(characterId);
            SaveCurrentSelections();
            BroadcastSelections();
            return true;
        }

        public bool DeselectCharacter(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return false;

            bool removed = _selectedCharacterIds.Remove(characterId);
            if (removed)
            {
                SaveCurrentSelections();
                BroadcastSelections();
            }
            return removed;
        }

        public bool DeselectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _selectedCharacterIds.Count) return false;
            _selectedCharacterIds.RemoveAt(slotIndex);
            SaveCurrentSelections();
            BroadcastSelections();
            return true;
        }

        public void ClearSelections()
        {
            _selectedCharacterIds.Clear();
            SaveCurrentSelections();
            BroadcastSelections();
        }

        public bool CanConfirm()
        {
            return _selectedCharacterIds.Count == RequiredSelectionCount;
        }

        public bool ConfirmSelection()
        {
            if (!CanConfirm())
            {
                OnSelectionError?.Invoke($"Please select exactly {RequiredSelectionCount} unique characters to continue.");
                return false;
            }

            SaveCurrentSelections();

            var data = new PlayerSelectionData(localPlayerId, _selectedCharacterIds);
            OnSelectionConfirmed?.Invoke(data);
            return true;
        }

        #endregion

        #region Persistence

        public void SaveCurrentSelections()
        {
            if (_saveSystem == null) return;
            var data = new PlayerSelectionData(localPlayerId, _selectedCharacterIds);
            _saveSystem.SaveSelection(data);

            // Synchronize directly with TeamSelectionManager persistent cache
            Clashura.Multiplayer.TeamSelectionManager.SaveTeam(_selectedCharacterIds);
        }

        public void LoadSavedSelections()
        {
            if (_saveSystem == null)
            {
                _saveSystem = new PlayerPrefsSelectionSaveSystem();
            }
            if (characterDatabase == null) return;

            var data = _saveSystem.LoadSelection(localPlayerId);
            _selectedCharacterIds.Clear();

            if (data?.SelectedCharacterIds != null)
            {
                foreach (var id in data.SelectedCharacterIds)
                {
                    // Validate ID against database and normalize to canonical CharacterId
                    var charData = characterDatabase.GetCharacterById(id);
                    if (charData != null && !_selectedCharacterIds.Contains(charData.CharacterId))
                    {
                        if (_selectedCharacterIds.Count < RequiredSelectionCount)
                        {
                            _selectedCharacterIds.Add(charData.CharacterId);
                        }
                    }
                }
            }

            BroadcastSelections();
        }

        #endregion

        #region Private Helpers

        private void BroadcastCurrentCharacter()
        {
            var charData = CurrentCharacter;
            OnCurrentCharacterChanged?.Invoke(charData);
        }

        private void BroadcastSelections()
        {
            var selectedList = new List<CharacterDataSO>();
            if (characterDatabase != null)
            {
                foreach (var id in _selectedCharacterIds)
                {
                    var cd = characterDatabase.GetCharacterById(id);
                    if (cd != null) selectedList.Add(cd);
                }
            }
            OnSelectionsUpdated?.Invoke(selectedList);
        }

        private string GetCharacterName(string characterId)
        {
            if (characterDatabase == null) return characterId;
            var c = characterDatabase.GetCharacterById(characterId);
            return c != null ? c.CharacterName : characterId;
        }

        #endregion
    }
}
