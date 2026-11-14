using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Clashura.Characters.Data;

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
            }

            if (cooldownText != null)
            {
                cooldownText.text = ability.Cooldown > 0f ? $"{ability.Cooldown:0.#}s CD" : "PASSIVE";
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
