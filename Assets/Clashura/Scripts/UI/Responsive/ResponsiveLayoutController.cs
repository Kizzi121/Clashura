using System;
using UnityEngine;
using UnityEngine.UI;

namespace Clashura.UI.Responsive
{
    /// <summary>
    /// Monitors screen aspect ratio and resolution changes, dynamically tuning CanvasScaler
    /// and providing layout state events for cross-platform mobile and desktop responsiveness.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveLayoutController : MonoBehaviour
    {
        public enum LayoutMode
        {
            PortraitMobile,     // Aspect < 1.0 (Phones vertical)
            LandscapeMobile,    // Aspect 1.0 to 1.6
            DesktopStandard,    // Aspect 1.6 to 1.85 (16:9, 16:10)
            UltrawideDesktop    // Aspect > 1.85 (21:9, 32:9)
        }

        [Header("Canvas Scaling Configuration")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);
        [SerializeField] private float portraitMatch = 0f; // Match width on portrait
        [SerializeField] private float landscapeMatch = 1f; // Match height on landscape

        public event Action<LayoutMode> OnLayoutModeChanged;

        private CanvasScaler _scaler;
        private LayoutMode _currentMode = LayoutMode.DesktopStandard;
        private int _lastWidth;
        private int _lastHeight;

        public LayoutMode CurrentMode => _currentMode;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            SetupScaler();
            EvaluateLayout();
        }

        private void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                EvaluateLayout();
            }
        }

        private void SetupScaler()
        {
            if (_scaler == null) return;
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = referenceResolution;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        public void EvaluateLayout()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            if (_lastWidth <= 0 || _lastHeight <= 0) return;

            float aspect = (float)_lastWidth / _lastHeight;
            LayoutMode newMode;

            if (aspect < 1.0f)
            {
                newMode = LayoutMode.PortraitMobile;
                if (_scaler != null) _scaler.matchWidthOrHeight = portraitMatch;
            }
            else if (aspect < 1.6f)
            {
                newMode = LayoutMode.LandscapeMobile;
                if (_scaler != null) _scaler.matchWidthOrHeight = 0.5f;
            }
            else if (aspect <= 2.0f)
            {
                newMode = LayoutMode.DesktopStandard;
                if (_scaler != null) _scaler.matchWidthOrHeight = landscapeMatch;
            }
            else
            {
                newMode = LayoutMode.UltrawideDesktop;
                if (_scaler != null) _scaler.matchWidthOrHeight = landscapeMatch;
            }

            if (newMode != _currentMode)
            {
                _currentMode = newMode;
                OnLayoutModeChanged?.Invoke(_currentMode);
            }
        }
    }
}
