using System.Collections.Generic;
using UnityEngine;
using Clashura.Core;

namespace Clashura.Characters.Data
{
    /// <summary>
    /// Master ScriptableObject holding 2D character information and visual references.
    /// Located under Data/Characters/ScriptableObjects/.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "Clashura/Characters/Character Data")]
    public class CharacterDataSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string characterId;
        [SerializeField] private string characterName;
        [SerializeField] private string characterTitle;
        [SerializeField] private CharacterRole role = CharacterRole.Vanguard;

        [Header("2D Visual System")]
        [Tooltip("2D Character Portrait for team slots, HUD, and icons.")]
        [SerializeField] private Sprite portrait;

        [Tooltip("Large 2D full-body or half-body illustration for the Character Selection Screen.")]
        [SerializeField] private Sprite selectionArtwork;

        [Tooltip("In-game 2D gameplay sprite.")]
        [SerializeField] private Sprite characterSprite;

        [Tooltip("In-game 2D gameplay prefab equipped with SpriteRenderer, 2D colliders, etc.")]
        [SerializeField] private GameObject characterPrefab2D;

        [Tooltip("Placeholder list of 2D animation sprite sheet frames (idle/run/attack).")]
        [SerializeField] private List<Sprite> spriteSheetFrames = new();

        [Header("Lore & Background")]
        [TextArea(2, 5)]
        [SerializeField] private string bio;

        [Header("Combat Stats")]
        [SerializeField] private CharacterStats baseStats = new CharacterStats(1000, 120, 80, 50);

        [Header("Abilities")]
        [SerializeField] private List<AbilityDataSO> abilities = new();

        public string CharacterId => characterId;
        public string CharacterName => characterName;
        public string CharacterTitle => characterTitle;
        public CharacterRole Role => role;
        public Sprite Portrait => portrait;
        public Sprite SelectionArtwork => selectionArtwork;
        public Sprite CharacterSprite => characterSprite;
        public GameObject CharacterPrefab2D => characterPrefab2D;
        public IReadOnlyList<Sprite> SpriteSheetFrames => spriteSheetFrames;
        public string Bio => bio;
        public CharacterStats BaseStats => baseStats;
        public IReadOnlyList<AbilityDataSO> Abilities => abilities;

        public void Initialize2D(
            string id,
            string charName,
            string title,
            CharacterRole charRole,
            Sprite charPortrait,
            Sprite charSelectionArtwork,
            Sprite charSprite,
            GameObject prefab2D,
            string biography,
            CharacterStats stats,
            List<AbilityDataSO> charAbilities,
            List<Sprite> frames = null)
        {
            characterId = id;
            characterName = charName;
            characterTitle = title;
            role = charRole;
            portrait = charPortrait;
            selectionArtwork = charSelectionArtwork;
            characterSprite = charSprite;
            characterPrefab2D = prefab2D;
            bio = biography;
            baseStats = stats;
            abilities = charAbilities ?? new List<AbilityDataSO>();
            spriteSheetFrames = frames ?? new List<Sprite>();
        }

        public bool ValidateData(out List<string> errors)
        {
            errors = new List<string>();
            if (string.IsNullOrWhiteSpace(characterId)) errors.Add($"Character '{name}' has empty CharacterId.");
            if (string.IsNullOrWhiteSpace(characterName)) errors.Add($"Character '{characterId}' has empty CharacterName.");
            if (portrait == null) errors.Add($"Character '{characterId}' has no portrait assigned.");
            if (selectionArtwork == null) errors.Add($"Character '{characterId}' has no 2D selection artwork assigned.");
            if (characterPrefab2D == null) errors.Add($"Character '{characterId}' has no 2D prefab assigned.");
            if (abilities == null || abilities.Count == 0) errors.Add($"Character '{characterId}' has no abilities assigned.");
            return errors.Count == 0;
        }
    }
}
