using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Clashura.Characters.Preview
{
    /// <summary>
    /// Controls the 2D Character Selection artwork presentation and smooth 2D transitions
    /// between character selections.
    /// </summary>
    public class CharacterVisual2DController : MonoBehaviour
    {
        [Header("Artwork Images")]
        [Tooltip("Primary image displaying current character 2D selection artwork.")]
        [SerializeField] private Image primaryImage;

        [Tooltip("Secondary image used for smooth cross-fade 2D transitions.")]
        [SerializeField] private Image secondaryImage;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 0.25f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private CanvasGroup _primaryGroup;
        private CanvasGroup _secondaryGroup;
        private Coroutine _transitionRoutine;

        private void Awake()
        {
            EnsureCanvasGroups();
        }

        private void EnsureCanvasGroups()
        {
            if (primaryImage != null)
            {
                _primaryGroup = primaryImage.GetComponent<CanvasGroup>();
                if (_primaryGroup == null) _primaryGroup = primaryImage.gameObject.AddComponent<CanvasGroup>();
                primaryImage.preserveAspect = true;
            }

            if (secondaryImage != null)
            {
                _secondaryGroup = secondaryImage.GetComponent<CanvasGroup>();
                if (_secondaryGroup == null) _secondaryGroup = secondaryImage.gameObject.AddComponent<CanvasGroup>();
                secondaryImage.preserveAspect = true;
                _secondaryGroup.alpha = 0f;
            }
        }

        public void DisplayCharacterArtwork(Sprite newArtwork)
        {
            if (newArtwork == null) return;

            EnsureCanvasGroups();

            if (primaryImage == null) return;

            // First time setup without transition
            if (primaryImage.sprite == null || !gameObject.activeInHierarchy)
            {
                primaryImage.sprite = newArtwork;
                if (_primaryGroup != null) _primaryGroup.alpha = 1f;
                return;
            }

            // Same artwork already displayed
            if (primaryImage.sprite == newArtwork) return;

            // Perform smooth 2D transition
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
            }
            _transitionRoutine = StartCoroutine(CrossFadeRoutine(newArtwork));
        }

        private IEnumerator CrossFadeRoutine(Sprite newArtwork)
        {
            if (secondaryImage == null || _secondaryGroup == null || _primaryGroup == null)
            {
                primaryImage.sprite = newArtwork;
                yield break;
            }

            // Set up secondary image with new artwork
            secondaryImage.sprite = newArtwork;
            secondaryImage.transform.localPosition = new Vector3(20f, 0f, 0f); // Subtle slide
            _secondaryGroup.alpha = 0f;

            float elapsed = 0f;
            Vector3 primaryStartPos = primaryImage.transform.localPosition;
            Vector3 primaryEndPos = primaryStartPos - new Vector3(20f, 0f, 0f);

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float curvedT = transitionCurve.Evaluate(t);

                _primaryGroup.alpha = 1f - curvedT;
                _secondaryGroup.alpha = curvedT;

                secondaryImage.transform.localPosition = Vector3.Lerp(new Vector3(20f, 0f, 0f), Vector3.zero, curvedT);

                yield return null;
            }

            // Swap: primary now has new artwork
            primaryImage.sprite = newArtwork;
            primaryImage.transform.localPosition = primaryStartPos;
            _primaryGroup.alpha = 1f;

            _secondaryGroup.alpha = 0f;
            secondaryImage.transform.localPosition = Vector3.zero;
            _transitionRoutine = null;
        }
    }
}
