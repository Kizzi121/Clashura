using UnityEngine;

namespace Clashura.UI.Responsive
{
    /// <summary>
    /// Adjusts the RectTransform anchors to match the device's hardware safe area (notches, rounded corners, pill cutouts).
    /// Fully responsive across mobile (iOS/Android) and desktop window resizing.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeAreaHandler : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool conformX = true;
        [SerializeField] private bool conformY = true;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (HasScreenChanged())
            {
                ApplySafeArea();
            }
        }

        private bool HasScreenChanged()
        {
            return _lastSafeArea != Screen.safeArea ||
                   _lastScreenSize.x != Screen.width ||
                   _lastScreenSize.y != Screen.height ||
                   _lastOrientation != Screen.orientation;
        }

        public void ApplySafeArea()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
                if (_rectTransform == null) return;
            }

            Rect safeArea = Screen.safeArea;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

            // Guard against zero-size screens in editor initialization
            if (screenSize.x <= 0 || screenSize.y <= 0) return;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;

            if (!conformX)
            {
                anchorMin.x = 0;
                anchorMax.x = 1;
            }

            if (!conformY)
            {
                anchorMin.y = 0;
                anchorMax.y = 1;
            }

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;
            _lastOrientation = Screen.orientation;
        }
    }
}
