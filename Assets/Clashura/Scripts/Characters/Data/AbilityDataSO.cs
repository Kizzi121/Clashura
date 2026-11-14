using UnityEngine;
using Clashura.Core;

namespace Clashura.Characters.Data
{
    /// <summary>
    /// ScriptableObject defining an individual character ability.
    /// Located under Data/Characters/AbilityData/.
    /// </summary>
    [CreateAssetMenu(fileName = "NewAbilityData", menuName = "Clashura/Characters/Ability Data")]
    public class AbilityDataSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string abilityId;
        [SerializeField] private string abilityName;
        [SerializeField] private Sprite icon;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Parameters")]
        [SerializeField] private AbilityType abilityType = AbilityType.Skill;
        [SerializeField] private float cooldown = 5.0f;

        public string AbilityId => abilityId;
        public string AbilityName => abilityName;
        public Sprite Icon => icon;
        public string Description => description;
        public AbilityType AbilityType => abilityType;
        public float Cooldown => cooldown;

        public void Initialize(string id, string name, string desc, AbilityType type, float cd, Sprite iconSprite = null)
        {
            abilityId = id;
            abilityName = name;
            description = desc;
            abilityType = type;
            cooldown = cd;
            icon = iconSprite;
        }
    }
}
