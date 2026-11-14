using System;
using System.Collections.Generic;
using UnityEngine;
using Clashura.Characters.Data;

namespace Clashura.Characters.Managers
{
    /// <summary>
    /// Central ScriptableObject database containing all 18 playable characters.
    /// Located under Data/Characters/ScriptableObjects/.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Clashura/Characters/Character Database")]
    public class CharacterDatabaseSO : ScriptableObject
    {
        [Tooltip("Master list of all playable characters in the game.")]
        [SerializeField] private List<CharacterDataSO> characters = new();

        private Dictionary<string, CharacterDataSO> _idLookup;

        public IReadOnlyList<CharacterDataSO> Characters => characters;
        public int CharacterCount => characters != null ? characters.Count : 0;

        private void OnEnable()
        {
            BuildLookup();
        }

        public void SetCharacters(List<CharacterDataSO> newCharacters)
        {
            characters = newCharacters ?? new List<CharacterDataSO>();
            BuildLookup();
        }

        private void BuildLookup()
        {
            _idLookup = new Dictionary<string, CharacterDataSO>(StringComparer.OrdinalIgnoreCase);
            if (characters == null) return;

            for (int i = 0; i < characters.Count; i++)
            {
                var charData = characters[i];
                if (charData != null && !string.IsNullOrWhiteSpace(charData.CharacterId))
                {
                    if (!_idLookup.ContainsKey(charData.CharacterId))
                    {
                        _idLookup.Add(charData.CharacterId, charData);
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterDatabaseSO] Duplicate Character ID detected: '{charData.CharacterId}' at index {i}.", this);
                    }
                }
            }
        }

        public CharacterDataSO GetCharacterById(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return null;
            if (_idLookup == null || _idLookup.Count != characters.Count)
            {
                BuildLookup();
            }

            return _idLookup.TryGetValue(characterId, out var charData) ? charData : null;
        }

        public CharacterDataSO GetCharacterByIndex(int index)
        {
            if (characters == null || index < 0 || index >= characters.Count) return null;
            return characters[index];
        }

        public int IndexOf(CharacterDataSO character)
        {
            if (characters == null || character == null) return -1;
            return characters.IndexOf(character);
        }

        public int IndexOfId(string characterId)
        {
            if (characters == null || string.IsNullOrWhiteSpace(characterId)) return -1;
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] != null && string.Equals(characters[i].CharacterId, characterId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        public bool ValidateDatabase(out List<string> errors)
        {
            errors = new List<string>();
            if (characters == null || characters.Count == 0)
            {
                errors.Add("Character database is empty.");
                return false;
            }

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < characters.Count; i++)
            {
                var charData = characters[i];
                if (charData == null)
                {
                    errors.Add($"Slot {i} contains null character reference.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(charData.CharacterId))
                {
                    errors.Add($"Character at slot {i} ('{charData.name}') has empty CharacterId.");
                }
                else if (!seenIds.Add(charData.CharacterId))
                {
                    errors.Add($"Duplicate CharacterId detected: '{charData.CharacterId}' (slot {i}).");
                }

                if (charData.ValidateData(out var charErrors))
                {
                    errors.AddRange(charErrors);
                }
            }

            return errors.Count == 0;
        }
    }
}
