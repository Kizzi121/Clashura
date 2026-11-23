using System;
using System.Collections.Generic;
using UnityEngine;
using Clashura.Characters.Data;
using Clashura.Characters.Managers;

namespace Clashura.AI.Career
{
    /// <summary>
    /// Entry representing an AI combatant with placeholder designation,
    /// canonical character ID, ScriptableObject data, and instantiated prefab reference.
    /// </summary>
    [Serializable]
    public class AICharacterEntry
    {
        [Tooltip("Organizational placeholder identifier (e.g., 'Easy AI 01').")]
        public string placeholderName;

        [Tooltip("Canonical database Character ID (e.g., 'clashura_char_01').")]
        public string characterId;

        [Tooltip("Display name of the character (e.g., 'Aethelgard').")]
        public string characterName;

        [Tooltip("ScriptableObject data holding portraits, stats, abilities, and bio.")]
        public CharacterDataSO characterData;

        [Tooltip("Instantiatable 2D/3D character prefab.")]
        public GameObject characterPrefab;

        public Sprite Portrait => characterData != null ? characterData.Portrait : null;
        public string RoleName => characterData != null ? characterData.Role.ToString() : "Fighter";

        public AICharacterEntry() { }

        public AICharacterEntry(string placeholder, string charId, string name, CharacterDataSO data, GameObject prefab)
        {
            placeholderName = placeholder;
            characterId = charId;
            characterName = name;
            characterData = data;
            characterPrefab = prefab;
        }
    }

    /// <summary>
    /// Master AI Character Database organizing 18 character references into
    /// three distinct tiers (Easy, Medium, Hard) of 6 characters each.
    /// Exposes Inspector fields for manual prefab assignments.
    /// </summary>
    public class AICharacterDatabase : MonoBehaviour
    {
        private static AICharacterDatabase _instance;
        public static AICharacterDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AICharacterDatabase>();
                }
                return _instance;
            }
        }

        [Header("Master Character Database Reference")]
        [SerializeField] private CharacterDatabaseSO masterDatabase;

        [Header("EASY AI LIST (6 Characters)")]
        [SerializeField] private List<AICharacterEntry> easyAIList = new List<AICharacterEntry>(6);

        [Header("MEDIUM AI LIST (6 Characters)")]
        [SerializeField] private List<AICharacterEntry> mediumAIList = new List<AICharacterEntry>(6);

        [Header("HARD AI LIST (6 Characters)")]
        [SerializeField] private List<AICharacterEntry> hardAIList = new List<AICharacterEntry>(6);

        public IReadOnlyList<AICharacterEntry> EasyAIList => easyAIList;
        public IReadOnlyList<AICharacterEntry> MediumAIList => mediumAIList;
        public IReadOnlyList<AICharacterEntry> HardAIList => hardAIList;
        public CharacterDatabaseSO MasterDatabase => masterDatabase;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            EnsurePopulated();
        }

        public IReadOnlyList<AICharacterEntry> GetCharactersForDifficulty(AIDifficulty difficulty)
        {
            EnsurePopulated();
            return difficulty switch
            {
                AIDifficulty.Easy => easyAIList,
                AIDifficulty.Medium => mediumAIList,
                AIDifficulty.Hard => hardAIList,
                _ => mediumAIList
            };
        }

        public AICharacterEntry GetCharacterById(string charId)
        {
            if (string.IsNullOrWhiteSpace(charId)) return null;
            EnsurePopulated();

            foreach (var e in easyAIList) if (string.Equals(e.characterId, charId, StringComparison.OrdinalIgnoreCase)) return e;
            foreach (var e in mediumAIList) if (string.Equals(e.characterId, charId, StringComparison.OrdinalIgnoreCase)) return e;
            foreach (var e in hardAIList) if (string.Equals(e.characterId, charId, StringComparison.OrdinalIgnoreCase)) return e;

            // Fallback lookup from master database
            if (masterDatabase != null)
            {
                var c = masterDatabase.GetCharacterById(charId);
                if (c != null)
                {
                    return new AICharacterEntry("AI Reserve", c.CharacterId, c.CharacterName, c, c.CharacterPrefab2D);
                }
            }

            return null;
        }

        /// <summary>
        /// Validates that all lists are filled with 6 entries. If empty or unassigned,
        /// auto-populates defaults from the 18 Clashura character assets.
        /// </summary>
        public void EnsurePopulated()
        {
            if (masterDatabase == null)
            {
                masterDatabase = Resources.Load<CharacterDatabaseSO>("CharacterDatabase");
                if (masterDatabase == null)
                {
#if UNITY_EDITOR
                    masterDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabaseSO>("Assets/Clashura/Data/Characters/ScriptableObjects/CharacterDatabase.asset");
#endif
                }
            }

            if (easyAIList.Count < 6 || mediumAIList.Count < 6 || hardAIList.Count < 6)
            {
                PopulateDefaultLists();
            }
        }

        /// <summary>
        /// Populates the 18 characters evenly into 6 Easy, 6 Medium, and 6 Hard characters.
        /// </summary>
        public void PopulateDefaultLists()
        {
            if (masterDatabase == null) return;

            // Tier 1: Easy (6 characters)
            string[] easyIds = { "clashura_char_01", "clashura_char_02", "clashura_char_03", "clashura_char_04", "clashura_char_05", "clashura_char_06" };
            // Tier 2: Medium (6 characters)
            string[] medIds  = { "clashura_char_07", "clashura_char_08", "clashura_char_09", "clashura_char_10", "clashura_char_11", "clashura_char_15" };
            // Tier 3: Hard (6 characters)
            string[] hardIds = { "clashura_char_12", "clashura_char_13", "clashura_char_14", "clashura_char_16", "clashura_char_17", "clashura_char_18" };

            easyAIList.Clear();
            for (int i = 0; i < easyIds.Length; i++)
            {
                var c = masterDatabase.GetCharacterById(easyIds[i]);
                if (c != null)
                {
                    easyAIList.Add(new AICharacterEntry($"Easy AI 0{i + 1}", c.CharacterId, c.CharacterName, c, c.CharacterPrefab2D));
                }
            }

            mediumAIList.Clear();
            for (int i = 0; i < medIds.Length; i++)
            {
                var c = masterDatabase.GetCharacterById(medIds[i]);
                if (c != null)
                {
                    mediumAIList.Add(new AICharacterEntry($"Medium AI 0{i + 1}", c.CharacterId, c.CharacterName, c, c.CharacterPrefab2D));
                }
            }

            hardAIList.Clear();
            for (int i = 0; i < hardIds.Length; i++)
            {
                var c = masterDatabase.GetCharacterById(hardIds[i]);
                if (c != null)
                {
                    hardAIList.Add(new AICharacterEntry($"Hard AI 0{i + 1}", c.CharacterId, c.CharacterName, c, c.CharacterPrefab2D));
                }
            }
        }
    }
}
