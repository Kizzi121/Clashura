using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Clashura.UI.Animation
{
    /// <summary>
    /// Delivers responsive micro-interactions and animations for fighting game UI buttons:
    /// - Smooth hover scale-up and color glow
    /// - Punchy click press-down feedback
    /// - Optional breathing / pulse animation for primary Call-To-Action buttons
    /// Operates using unscaledDeltaTime for smooth 60-120fps motion regardless of timeScale.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonAnimator : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler, 
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Header("Target Elements")]
        [SerializeField] private RectTransform targetTransform;
        [SerializeField] private Graphic targetGraphic;

        [Header("Scale Micro-Interactions")]
        [SerializeField] private float normalScale = 1.0f;
        [SerializeField] private float hoverScale = 1.06f;
        [SerializeField] private float pressScale = 0.93f;
        [SerializeField] private float transitionSpeed = 18f;

        [Header("Color Tint / Glow")]
        [SerializeField] private bool enableColorGlow = true;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = new Color(1.18f, 1.18f, 1.18f, 1f);
        [SerializeField] private Color pressColor = new Color(0.82f, 0.82f, 0.82f, 1f);

        [Header("Call-To-Action Pulse (Optional)")]
        [SerializeField] private bool enablePulse = false;
        [SerializeField] private float pulseFrequency = 2.4f;
        [SerializeField] private float pulseAmplitude = 0.035f;

        private float _currentScale = 1f;
        private float _targetScale = 1f;
        private Color _currentColor = Color.white;
        private Color _targetColor = Color.white;
        private bool _isHovered = false;
        private bool _isPressed = false;
        private float _pulseTimer = 0f;

        public bool EnablePulse
        {
            get => enablePulse;
            set => enablePulse = value;
        }

        private void Awake()
        {
            if (targetTransform == null)
            {
                targetTransform = GetComponent<RectTransform>();
            }

            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            _currentScale = normalScale;
            _targetScale = normalScale;
            _currentColor = normalColor;
            _targetColor = normalColor;
        }

        private void OnEnable()
        {
            _currentScale = normalScale;
            _targetScale = normalScale;
            _currentColor = normalColor;
            _targetColor = normalColor;
            _isHovered = false;
            _isPressed = false;

            if (targetTransform != null)
            {
                targetTransform.localScale = Vector3.one * normalScale;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Compute target scale with optional pulse
            float baseTarget = _targetScale;
            if (enablePulse && !_isPressed)
            {
                _pulseTimer += dt * pulseFrequency;
                float pulseOffset = Mathf.Sin(_pulseTimer) * pulseAmplitude;
                baseTarget += pulseOffset;
            }

            // Smooth scale interpolation
            _currentScale = Mathf.Lerp(_currentScale, baseTarget, dt * transitionSpeed);
            if (targetTransform != null)
            {
                targetTransform.localScale = new Vector3(_currentScale, _currentScale, 1f);
            }

            // Smooth color interpolation
            if (enableColorGlow && targetGraphic != null)
            {
                _currentColor = Color.Lerp(_currentColor, _targetColor, dt * transitionSpeed);
                targetGraphic.color = _currentColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (!_isPressed)
            {
                _targetScale = hoverScale;
                _targetColor = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (!_isPressed)
            {
                _targetScale = normalScale;
                _targetColor = normalColor;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            _targetScale = pressScale;
            _targetColor = pressColor;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            _targetScale = _isHovered ? hoverScale : normalScale;
            _targetColor = _isHovered ? hoverColor : normalColor;
        }

        public void OnSelect(BaseEventData eventData)
        {
            _isHovered = true;
            _targetScale = hoverScale;
            _targetColor = hoverColor;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
            _targetScale = normalScale;
            _targetColor = normalColor;
        }
    }
}
