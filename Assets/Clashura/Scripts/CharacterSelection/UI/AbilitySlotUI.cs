using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Clashura.Characters.Data;
using Clashura.Core;

namespace Clashura.CharacterSelection.UI
{
    /// <summary>
    /// Displays a single ability with large icon, prominent name, type badge,
    /// cooldown, and full ability description text.
    /// </summary>
    public class AbilitySlotUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        public void Bind(AbilityDataSO ability)
        {
            if (ability == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (nameText != null)
            {
                nameText.text = ability.AbilityName;
            }

            if (typeText != null)
            {
                typeText.text = ability.AbilityType.ToString().ToUpper();
                switch (ability.AbilityType)
                {
                    case AbilityType.Primary:
                        typeText.color = new Color(1.0f, 0.70f, 0.25f); // Amber
                        break;
                    case AbilityType.Skill:
                        typeText.color = new Color(0.35f, 0.75f, 1.0f); // Azure Blue
                        break;
                    case AbilityType.Passive:
                        typeText.color = new Color(0.98f, 0.85f, 0.30f); // Golden Sun
                        break;
                    case AbilityType.Ultimate:
                        typeText.color = new Color(1.0f, 0.35f, 0.35f); // Crimson
                        break;
                }
            }

            if (cooldownText != null)
            {
                if (ability.Cooldown > 0f)
                {
                    cooldownText.gameObject.SetActive(true);
                    cooldownText.text = $"{ability.Cooldown:0.#}s CD";
                    cooldownText.color = new Color(1.0f, 0.85f, 0.30f);
                }
                else
                {
                    cooldownText.gameObject.SetActive(false);
                    cooldownText.text = "";
                }
            }

            if (descriptionText != null)
            {
                descriptionText.text = string.IsNullOrWhiteSpace(ability.Description) ? "" : ability.Description;
            }

            if (iconImage != null)
            {
                if (ability.Icon != null)
                {
                    iconImage.sprite = ability.Icon;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }
        }
    }
}
