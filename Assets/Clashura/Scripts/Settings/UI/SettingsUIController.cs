using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Clashura.UI.Navigation;

namespace Clashura.Settings.UI
{
    /// <summary>
    /// Master UI Controller for the CLASHURA Settings screen.
    /// Manages category tab navigation, live data bindings for all 5 settings sections,
    /// Back navigation to the Main Menu, and the Reset to Defaults confirmation modal.
    /// </summary>
    public class SettingsUIController : MonoBehaviour
    {
        [Header("Category Navigation Tabs")]
        [SerializeField] private Button audioTabButton;
        [SerializeField] private Button graphicsTabButton;
        [SerializeField] private Button controlsTabButton;
        [SerializeField] private Button gameplayTabButton;
        [SerializeField] private Button accessibilityTabButton;

        [Header("Category Content Containers")]
        [SerializeField] private GameObject audioContentPanel;
        [SerializeField] private GameObject graphicsContentPanel;
        [SerializeField] private GameObject controlsContentPanel;
        [SerializeField] private GameObject gameplayContentPanel;
        [SerializeField] private GameObject accessibilityContentPanel;

        [Header("Header Actions")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetDefaultsButton;
        [SerializeField] private ResetConfirmationPopupUI resetPopup;

        #region Category 1: Audio Controls
        [Header("Audio Controls")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeText;

        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private TextMeshProUGUI musicVolumeText;

        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI sfxVolumeText;

        [SerializeField] private Button muteAllToggleButton;
        [SerializeField] private TextMeshProUGUI muteAllToggleText;
        [SerializeField] private Image muteAllToggleFill;
        #endregion

        #region Category 2: Graphics Controls
        [Header("Graphics Controls")]
        [SerializeField] private Button qualityPrevButton;
        [SerializeField] private Button qualityNextButton;
        [SerializeField] private TextMeshProUGUI qualityText;

        [SerializeField] private Button fpsPrevButton;
        [SerializeField] private Button fpsNextButton;
        [SerializeField] private TextMeshProUGUI fpsText;

        [SerializeField] private Button vsyncToggleButton;
        [SerializeField] private TextMeshProUGUI vsyncToggleText;
        [SerializeField] private Image vsyncToggleFill;

        [SerializeField] private Button fullscreenToggleButton;
        [SerializeField] private TextMeshProUGUI fullscreenToggleText;
        [SerializeField] private Image fullscreenToggleFill;

        [SerializeField] private Button resPrevButton;
        [SerializeField] private Button resNextButton;
        [SerializeField] private TextMeshProUGUI resText;
        [SerializeField] private GameObject resolutionRowObj;
        #endregion

        #region Category 3: Controls Settings
        [Header("Controls Settings")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TextMeshProUGUI sensitivityText;

        [SerializeField] private Button btnSizePrevButton;
        [SerializeField] private Button btnSizeNextButton;
        [SerializeField] private TextMeshProUGUI btnSizeText;

        [SerializeField] private Button vibrationToggleButton;
        [SerializeField] private TextMeshProUGUI vibrationToggleText;
        [SerializeField] private Image vibrationToggleFill;

        [SerializeField] private Button layoutPrevButton;
        [SerializeField] private Button layoutNextButton;
        [SerializeField] private TextMeshProUGUI layoutText;
        #endregion

        #region Category 4: Gameplay Settings
        [Header("Gameplay Settings")]
        [SerializeField] private Button damageNumbersToggleButton;
        [SerializeField] private TextMeshProUGUI damageNumbersToggleText;
        [SerializeField] private Image damageNumbersToggleFill;

        [SerializeField] private Button screenShakeToggleButton;
        [SerializeField] private TextMeshProUGUI screenShakeToggleText;
        [SerializeField] private Image screenShakeToggleFill;

        [SerializeField] private Button battleNotifsToggleButton;
        [SerializeField] private TextMeshProUGUI battleNotifsToggleText;
        [SerializeField] private Image battleNotifsToggleFill;

        [SerializeField] private Button combatVFXToggleButton;
        [SerializeField] private TextMeshProUGUI combatVFXToggleText;
        [SerializeField] private Image combatVFXToggleFill;
        #endregion

        #region Category 5: Accessibility Settings
        [Header("Accessibility Settings")]
        [SerializeField] private Slider uiScaleSlider;
        [SerializeField] private TextMeshProUGUI uiScaleText;

        [SerializeField] private Button textSizePrevButton;
        [SerializeField] private Button textSizeNextButton;
        [SerializeField] private TextMeshProUGUI textSizeText;

        [SerializeField] private Button reduceEffectsToggleButton;
        [SerializeField] private TextMeshProUGUI reduceEffectsToggleText;
        [SerializeField] private Image reduceEffectsToggleFill;

        [SerializeField] private Button highContrastToggleButton;
        [SerializeField] private TextMeshProUGUI highContrastToggleText;
        [SerializeField] private Image highContrastToggleFill;
        #endregion

        private SettingsCategory _currentCategory = SettingsCategory.Audio;

        // Stepper lists
        private static readonly GraphicsQualityTier[] QualityTiers = {
            GraphicsQualityTier.Low, GraphicsQualityTier.Medium, GraphicsQualityTier.High, GraphicsQualityTier.Ultra
        };

        private static readonly FPSLimitMode[] FPSModes = {
            FPSLimitMode.FPS_30, FPSLimitMode.FPS_60, FPSLimitMode.FPS_90, FPSLimitMode.FPS_120, FPSLimitMode.Unlimited
        };

        private static readonly ControlButtonSize[] ButtonSizes = {
            ControlButtonSize.Small, ControlButtonSize.Medium, ControlButtonSize.Large
        };

        private static readonly ControlLayoutMode[] LayoutModes = {
            ControlLayoutMode.Default, ControlLayoutMode.Inverted, ControlLayoutMode.LeftHanded
        };

        private static readonly TextSizeMode[] TextSizes = {
            TextSizeMode.Small, TextSizeMode.Medium, TextSizeMode.Large
        };

        private void Awake()
        {
            HookCategoryTabs();
            HookHeaderActions();
            HookAudioControls();
            HookGraphicsControls();
            HookControlsSettings();
            HookGameplaySettings();
            HookAccessibilitySettings();
        }

        private void OnEnable()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSettingsReset += RefreshAllUI;
            }
            SelectCategory(_currentCategory);
            RefreshAllUI();
        }

        private void OnDisable()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSettingsReset -= RefreshAllUI;
            }
        }

        #region Category Navigation
        private void HookCategoryTabs()
        {
            if (audioTabButton != null)
                audioTabButton.onClick.AddListener(() => SelectCategory(SettingsCategory.Audio));

            if (graphicsTabButton != null)
                graphicsTabButton.onClick.AddListener(() => SelectCategory(SettingsCategory.Graphics));

            if (controlsTabButton != null)
                controlsTabButton.onClick.AddListener(() => SelectCategory(SettingsCategory.Controls));

            if (gameplayTabButton != null)
                gameplayTabButton.onClick.AddListener(() => SelectCategory(SettingsCategory.Gameplay));

            if (accessibilityTabButton != null)
                accessibilityTabButton.onClick.AddListener(() => SelectCategory(SettingsCategory.Accessibility));
        }

        public void SelectCategory(SettingsCategory category)
        {
            _currentCategory = category;

            if (audioContentPanel != null) audioContentPanel.SetActive(category == SettingsCategory.Audio);
            if (graphicsContentPanel != null) graphicsContentPanel.SetActive(category == SettingsCategory.Graphics);
            if (controlsContentPanel != null) controlsContentPanel.SetActive(category == SettingsCategory.Controls);
            if (gameplayContentPanel != null) gameplayContentPanel.SetActive(category == SettingsCategory.Gameplay);
            if (accessibilityContentPanel != null) accessibilityContentPanel.SetActive(category == SettingsCategory.Accessibility);

            UpdateTabHighlight(audioTabButton, category == SettingsCategory.Audio);
            UpdateTabHighlight(graphicsTabButton, category == SettingsCategory.Graphics);
            UpdateTabHighlight(controlsTabButton, category == SettingsCategory.Controls);
            UpdateTabHighlight(gameplayTabButton, category == SettingsCategory.Gameplay);
            UpdateTabHighlight(accessibilityTabButton, category == SettingsCategory.Accessibility);
        }

        private void UpdateTabHighlight(Button btn, bool isSelected)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = isSelected ? new Color(1.0f, 0.90f, 0.45f, 1f) : new Color(0.65f, 0.75f, 0.90f, 0.85f);
            }
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.color = isSelected ? new Color(1.0f, 0.85f, 0.30f, 1f) : Color.white;
                txt.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            }
        }
        #endregion

        #region Header Actions
        private void HookHeaderActions()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(() =>
                {
                    if (UINavigationController.Instance != null)
                    {
                        UINavigationController.Instance.OpenMainMenu();
                    }
                });
            }

            if (resetDefaultsButton != null)
            {
                resetDefaultsButton.onClick.AddListener(() =>
                {
                    if (resetPopup != null)
                    {
                        resetPopup.Show(() =>
                        {
                            SettingsManager.Instance?.ResetToDefaults();
                            RefreshAllUI();
                        });
                    }
                    else
                    {
                        SettingsManager.Instance?.ResetToDefaults();
                        RefreshAllUI();
                    }
                });
            }
        }
        #endregion

        #region Hook Controls
        private void HookAudioControls()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(val =>
                {
                    SettingsManager.Instance?.SetMasterVolume(val);
                    if (masterVolumeText != null) masterVolumeText.text = $"{Mathf.RoundToInt(val)}%";
                });
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(val =>
                {
                    SettingsManager.Instance?.SetMusicVolume(val);
                    if (musicVolumeText != null) musicVolumeText.text = $"{Mathf.RoundToInt(val)}%";
                });
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(val =>
                {
                    SettingsManager.Instance?.SetSFXVolume(val);
                    if (sfxVolumeText != null) sfxVolumeText.text = $"{Mathf.RoundToInt(val)}%";
                });
            }

            if (muteAllToggleButton != null)
            {
                muteAllToggleButton.onClick.AddListener(() =>
                {
                    if (SettingsManager.Instance != null)
                    {
                        bool next = !SettingsManager.Instance.MuteAll;
                        SettingsManager.Instance.SetMuteAll(next);
                        UpdateToggleVisual(muteAllToggleFill, muteAllToggleText, next);
                    }
                });
            }
        }

        private void HookGraphicsControls()
        {
            if (qualityPrevButton != null)
                qualityPrevButton.onClick.AddListener(() => StepQuality(-1));
            if (qualityNextButton != null)
                qualityNextButton.onClick.AddListener(() => StepQuality(1));

            if (fpsPrevButton != null)
                fpsPrevButton.onClick.AddListener(() => StepFPS(-1));
            if (fpsNextButton != null)
                fpsNextButton.onClick.AddListener(() => StepFPS(1));

            if (vsyncToggleButton != null)
            {
                vsyncToggleButton.onClick.AddListener(() =>
                {
                    if (SettingsManager.Instance != null)
                    {
                        bool next = !SettingsManager.Instance.VSync;
                        SettingsManager.Instance.SetVSync(next);
                        UpdateToggleVisual(vsyncToggleFill, vsyncToggleText, next);
                    }
                });
            }

            if (fullscreenToggleButton != null)
            {
                fullscreenToggleButton.onClick.AddListener(() =>
                {
                    if (SettingsManager.Instance != null)
                    {
                        bool next = !SettingsManager.Instance.Fullscreen;
                        SettingsManager.Instance.SetFullscreen(next);
                        UpdateToggleVisual(fullscreenToggleFill, fullscreenToggleText, next);
                    }
                });
            }

            if (resPrevButton != null)
                resPrevButton.onClick.AddListener(() => StepResolution(-1));
            if (resNextButton != null)
                resNextButton.onClick.AddListener(() => StepResolution(1));
        }

        private void HookControlsSettings()
        {
            if (sensitivitySlider != null)
            {
                sensitivitySlider.onValueChanged.AddListener(val =>
                {
                    SettingsManager.Instance?.SetTouchSensitivity(val);
                    if (sensitivityText != null) sensitivityText.text = $"{Mathf.RoundToInt(val)}%";
                });
            }

            if (btnSizePrevButton != null)
                btnSizePrevButton.onClick.AddListener(() => StepButtonSize(-1));
            if (btnSizeNextButton != null)
                btnSizeNextButton.onClick.AddListener(() => StepButtonSize(1));

            if (vibrationToggleButton != null)
            {
                vibrationToggleButton.onClick.AddListener(() =>
                {
                    if (SettingsManager.Instance != null)
                    {
                        bool next = !SettingsManager.Instance.Vibration;
                        SettingsManager.Instance.SetVibration(next);
                        UpdateToggleVisual(vibrationToggleFill, vibrationToggleText, next);
                    }
                });
            }

            if (layoutPrevButton != null)
                layoutPrevButton.onClick.AddListener(() => StepControlLayout(-1));
            if (layoutNextButton != null)
                layoutNextButton.onClick.AddListener(() => StepControlLayout(1));
        }

        private void HookGameplaySettings()
        {
            if (damageNumbersToggleButton != null)
            {
                damageNumbersToggleButton.onClick.AddListener(() =>
                {
                    if (SettingsManager.Instance != null)
                    {
                        bool next = !SettingsManager.Instance.DamageNumbers;
                        SettingsManager.Instance.SetDamageNumbers(next);
                        UpdateToggleVisual(damageNumbersToggleFill, damageNumbersToggleText, next);
                    }
                });
            }

            if (screenShakeToggleButton != null)
            {
                screenShakeToggleButton.onClick.AddListener(() =>
                {
                    if (SettingsManager.Instance != null)
                    {
                        bool next = !SettingsManager.Instance.ScreenShake;
                        SettingsManager.Instance.SetScreenShake(next);
                        UpdateToggleVisual(screenShakeToggleFill, screenShakeToggleText, next);
                    }
                });
            }

            if (battleNotifsToggleButton != null)
            {
                battleNotifsToggleButton.onClick.AddListener(() =>
                {
                    if (SettingsManager.Instance != null)
                    {
                        bool next = !SettingsManager.Instance.BattleNotifications;
                        SettingsManager.Instance.SetBattleNotifications(next);
                        UpdateToggleVisual(battleNotifsToggleFill, battleNotifsToggleText, next);
                    }
                });
            }

            if (combatVFXToggleButton != null)
            {
                combatVFXToggleButton.onClick.AddListener(() =>
                {
                    if (SettingsManager.Instance != null)
                    {
                        bool next = !SettingsManager.Instance.CombatVisualEffects;
                        SettingsManager.Instance.SetCombatVisualEffects(next);
                        UpdateToggleVisual(combatVFXToggleFill, combatVFXToggleText, next);
                    }
                });
            }
        }

        private void HookAccessibilitySettings()
        {
            if (uiScaleSlider != null)
            {
                uiScaleSlider.onValueChanged.AddListener(val =>
                {
                    SettingsManager.Instance?.SetUIScale(val);
                    if (uiScaleText != null) uiScaleText.text = $"{Mathf.RoundToInt(val)}%";
                });
            }

            if (textSizePrevButton != null)
                textSizePrevButton.onClick.AddListener(() => StepTextSize(-1));
            if (textSizeNextButton != null)
                textSizeNextButton.onClick.AddListener(() => StepTextSize(1));

            if (reduceEffectsToggleButton != null)
            {
                reduceEffectsToggleButton.onClick.AddListener(() =>
                {
                    if (SettingsManager.Instance != null)
                    {
                        bool next = !SettingsManager.Instance.ReduceScreenEffects;
                        SettingsManager.Instance.SetReduceScreenEffects(next);
                        UpdateToggleVisual(reduceEffectsToggleFill, reduceEffectsToggleText, next);
                    }
                });
            }

            if (highContrastToggleButton != null)
            {
                highContrastToggleButton.onClick.AddListener(() =>
                {
                    if (SettingsManager.Instance != null)
                    {
                        bool next = !SettingsManager.Instance.HighContrast;
                        SettingsManager.Instance.SetHighContrast(next);
                        UpdateToggleVisual(highContrastToggleFill, highContrastToggleText, next);
                    }
                });
            }
        }
        #endregion

        #region Steppers
        private void StepQuality(int direction)
        {
            if (SettingsManager.Instance == null) return;
            int idx = Array.IndexOf(QualityTiers, SettingsManager.Instance.GraphicsQuality);
            if (idx < 0) idx = 2;
            int next = Mathf.Clamp(idx + direction, 0, QualityTiers.Length - 1);
            SettingsManager.Instance.SetGraphicsQuality(QualityTiers[next]);
            if (qualityText != null) qualityText.text = QualityTiers[next].ToString().ToUpper();
        }

        private void StepFPS(int direction)
        {
            if (SettingsManager.Instance == null) return;
            int idx = Array.IndexOf(FPSModes, SettingsManager.Instance.FPSLimit);
            if (idx < 0) idx = 1;
            int next = Mathf.Clamp(idx + direction, 0, FPSModes.Length - 1);
            SettingsManager.Instance.SetFPSLimit(FPSModes[next]);
            if (fpsText != null)
                fpsText.text = FPSModes[next] == FPSLimitMode.Unlimited ? "UNLIMITED" : $"{(int)FPSModes[next]} FPS";
        }

        private void StepResolution(int direction)
        {
            if (SettingsManager.Instance == null) return;
            Resolution[] res = Screen.resolutions;
            if (res == null || res.Length == 0) return;

            int cur = SettingsManager.Instance.ResolutionIndex;
            int next = Mathf.Clamp(cur + direction, 0, res.Length - 1);
            SettingsManager.Instance.SetResolution(next);
            if (resText != null)
                resText.text = $"{res[next].width} x {res[next].height}";
        }

        private void StepButtonSize(int direction)
        {
            if (SettingsManager.Instance == null) return;
            int idx = Array.IndexOf(ButtonSizes, SettingsManager.Instance.ButtonSize);
            if (idx < 0) idx = 1;
            int next = Mathf.Clamp(idx + direction, 0, ButtonSizes.Length - 1);
            SettingsManager.Instance.SetButtonSize(ButtonSizes[next]);
            if (btnSizeText != null) btnSizeText.text = ButtonSizes[next].ToString().ToUpper();
        }

        private void StepControlLayout(int direction)
        {
            if (SettingsManager.Instance == null) return;
            int idx = Array.IndexOf(LayoutModes, SettingsManager.Instance.ControlLayout);
            if (idx < 0) idx = 0;
            int next = Mathf.Clamp(idx + direction, 0, LayoutModes.Length - 1);
            SettingsManager.Instance.SetControlLayout(LayoutModes[next]);
            if (layoutText != null) layoutText.text = LayoutModes[next].ToString().ToUpper();
        }

        private void StepTextSize(int direction)
        {
            if (SettingsManager.Instance == null) return;
            int idx = Array.IndexOf(TextSizes, SettingsManager.Instance.TextSize);
            if (idx < 0) idx = 1;
            int next = Mathf.Clamp(idx + direction, 0, TextSizes.Length - 1);
            SettingsManager.Instance.SetTextSize(TextSizes[next]);
            if (textSizeText != null) textSizeText.text = TextSizes[next].ToString().ToUpper();
        }
        #endregion

        #region Refresh UI
        public void RefreshAllUI()
        {
            var mgr = SettingsManager.Instance;
            if (mgr == null) return;

            // Audio
            if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(mgr.MasterVolume);
            if (masterVolumeText != null) masterVolumeText.text = $"{Mathf.RoundToInt(mgr.MasterVolume)}%";

            if (musicVolumeSlider != null) musicVolumeSlider.SetValueWithoutNotify(mgr.MusicVolume);
            if (musicVolumeText != null) musicVolumeText.text = $"{Mathf.RoundToInt(mgr.MusicVolume)}%";

            if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(mgr.SFXVolume);
            if (sfxVolumeText != null) sfxVolumeText.text = $"{Mathf.RoundToInt(mgr.SFXVolume)}%";

            UpdateToggleVisual(muteAllToggleFill, muteAllToggleText, mgr.MuteAll);

            // Graphics
            if (qualityText != null) qualityText.text = mgr.GraphicsQuality.ToString().ToUpper();
            if (fpsText != null)
                fpsText.text = mgr.FPSLimit == FPSLimitMode.Unlimited ? "UNLIMITED" : $"{(int)mgr.FPSLimit} FPS";

            UpdateToggleVisual(vsyncToggleFill, vsyncToggleText, mgr.VSync);
            UpdateToggleVisual(fullscreenToggleFill, fullscreenToggleText, mgr.Fullscreen);

            Resolution[] res = Screen.resolutions;
            if (resText != null && res != null && mgr.ResolutionIndex >= 0 && mgr.ResolutionIndex < res.Length)
            {
                resText.text = $"{res[mgr.ResolutionIndex].width} x {res[mgr.ResolutionIndex].height}";
            }

            // Controls
            if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(mgr.TouchSensitivity);
            if (sensitivityText != null) sensitivityText.text = $"{Mathf.RoundToInt(mgr.TouchSensitivity)}%";

            if (btnSizeText != null) btnSizeText.text = mgr.ButtonSize.ToString().ToUpper();
            UpdateToggleVisual(vibrationToggleFill, vibrationToggleText, mgr.Vibration);
            if (layoutText != null) layoutText.text = mgr.ControlLayout.ToString().ToUpper();

            // Gameplay
            UpdateToggleVisual(damageNumbersToggleFill, damageNumbersToggleText, mgr.DamageNumbers);
            UpdateToggleVisual(screenShakeToggleFill, screenShakeToggleText, mgr.ScreenShake);
            UpdateToggleVisual(battleNotifsToggleFill, battleNotifsToggleText, mgr.BattleNotifications);
            UpdateToggleVisual(combatVFXToggleFill, combatVFXToggleText, mgr.CombatVisualEffects);

            // Accessibility
            if (uiScaleSlider != null) uiScaleSlider.SetValueWithoutNotify(mgr.UIScale);
            if (uiScaleText != null) uiScaleText.text = $"{Mathf.RoundToInt(mgr.UIScale)}%";

            if (textSizeText != null) textSizeText.text = mgr.TextSize.ToString().ToUpper();
            UpdateToggleVisual(reduceEffectsToggleFill, reduceEffectsToggleText, mgr.ReduceScreenEffects);
            UpdateToggleVisual(highContrastToggleFill, highContrastToggleText, mgr.HighContrast);
        }

        private void UpdateToggleVisual(Image fill, TextMeshProUGUI text, bool isOn)
        {
            if (fill != null)
            {
                fill.color = isOn ? new Color(0.20f, 0.85f, 0.45f, 1f) : new Color(0.25f, 0.30f, 0.40f, 1f);
            }
            if (text != null)
            {
                text.text = isOn ? "ON" : "OFF";
                text.color = isOn ? new Color(0.30f, 1.0f, 0.55f, 1f) : new Color(0.65f, 0.75f, 0.88f, 0.9f);
            }
        }
        #endregion
    }
}
