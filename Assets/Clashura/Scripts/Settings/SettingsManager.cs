using System;
using System.Collections.Generic;
using UnityEngine;

namespace Clashura.Settings
{
    /// <summary>
    /// Centralized persistent settings manager for CLASHURA.
    /// Handles PlayerPrefs persistence, automatic startup loading,
    /// dynamic application to Unity native systems, and decoupled event dispatch.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        private static SettingsManager _instance;
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SettingsManager>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        #region Public Events
        // Audio Events
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;
        public event Action<bool> OnMuteAllChanged;

        // Graphics Events
        public event Action<GraphicsQualityTier> OnGraphicsQualityChanged;
        public event Action<FPSLimitMode> OnFPSLimitChanged;
        public event Action<bool> OnVSyncChanged;
        public event Action<bool> OnFullscreenChanged;
        public event Action<int> OnResolutionChanged;

        // Controls Events
        public event Action<float> OnTouchSensitivityChanged;
        public event Action<ControlButtonSize> OnButtonSizeChanged;
        public event Action<bool> OnVibrationChanged;
        public event Action<ControlLayoutMode> OnControlLayoutChanged;

        // Gameplay Events
        public event Action<bool> OnDamageNumbersChanged;
        public event Action<bool> OnScreenShakeChanged;
        public event Action<bool> OnBattleNotificationsChanged;
        public event Action<bool> OnCombatVisualEffectsChanged;

        // Accessibility Events
        public event Action<float> OnUIScaleChanged;
        public event Action<TextSizeMode> OnTextSizeChanged;
        public event Action<bool> OnReduceScreenEffectsChanged;
        public event Action<bool> OnHighContrastChanged;

        // General
        public event Action OnSettingsReset;
        #endregion

        #region State Fields
        // Audio
        private float _masterVolume;
        private float _musicVolume;
        private float _sfxVolume;
        private bool _muteAll;

        // Graphics
        private GraphicsQualityTier _graphicsQuality;
        private FPSLimitMode _fpsLimit;
        private bool _vSync;
        private bool _fullscreen;
        private int _resolutionIndex;

        // Controls
        private float _touchSensitivity;
        private ControlButtonSize _buttonSize;
        private bool _vibration;
        private ControlLayoutMode _controlLayout;

        // Gameplay
        private bool _damageNumbers;
        private bool _screenShake;
        private bool _battleNotifications;
        private bool _combatVisualEffects;

        // Accessibility
        private float _uiScale;
        private TextSizeMode _textSize;
        private bool _reduceScreenEffects;
        private bool _highContrast;
        #endregion

        #region Properties
        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;
        public bool MuteAll => _muteAll;

        public GraphicsQualityTier GraphicsQuality => _graphicsQuality;
        public FPSLimitMode FPSLimit => _fpsLimit;
        public bool VSync => _vSync;
        public bool Fullscreen => _fullscreen;
        public int ResolutionIndex => _resolutionIndex;

        public float TouchSensitivity => _touchSensitivity;
        public ControlButtonSize ButtonSize => _buttonSize;
        public bool Vibration => _vibration;
        public ControlLayoutMode ControlLayout => _controlLayout;

        public bool DamageNumbers => _damageNumbers;
        public bool ScreenShake => _screenShake;
        public bool BattleNotifications => _battleNotifications;
        public bool CombatVisualEffects => _combatVisualEffects;

        public float UIScale => _uiScale;
        public TextSizeMode TextSize => _textSize;
        public bool ReduceScreenEffects => _reduceScreenEffects;
        public bool HighContrast => _highContrast;
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadAllSettings();
            ApplyAllSettings();
        }

        #region Loading & Saving
        public void LoadAllSettings()
        {
            // Audio
            _masterVolume = PlayerPrefs.GetFloat(SettingsKeys.MasterVolume, SettingsKeys.DefaultMasterVolume);
            _musicVolume = PlayerPrefs.GetFloat(SettingsKeys.MusicVolume, SettingsKeys.DefaultMusicVolume);
            _sfxVolume = PlayerPrefs.GetFloat(SettingsKeys.SFXVolume, SettingsKeys.DefaultSFXVolume);
            _muteAll = PlayerPrefs.GetInt(SettingsKeys.MuteAll, SettingsKeys.DefaultMuteAll ? 1 : 0) == 1;

            // Graphics
            _graphicsQuality = (GraphicsQualityTier)PlayerPrefs.GetInt(SettingsKeys.GraphicsQuality, (int)SettingsKeys.DefaultGraphicsQuality);
            _fpsLimit = (FPSLimitMode)PlayerPrefs.GetInt(SettingsKeys.FPSLimit, (int)SettingsKeys.DefaultFPSLimit);
            _vSync = PlayerPrefs.GetInt(SettingsKeys.VSync, SettingsKeys.DefaultVSync ? 1 : 0) == 1;
            _fullscreen = PlayerPrefs.GetInt(SettingsKeys.Fullscreen, SettingsKeys.DefaultFullscreen ? 1 : 0) == 1;
            _resolutionIndex = PlayerPrefs.GetInt(SettingsKeys.ResolutionIndex, GetCurrentResolutionIndex());

            // Controls
            _touchSensitivity = PlayerPrefs.GetFloat(SettingsKeys.TouchSensitivity, SettingsKeys.DefaultTouchSensitivity);
            _buttonSize = (ControlButtonSize)PlayerPrefs.GetInt(SettingsKeys.ButtonSize, (int)SettingsKeys.DefaultButtonSize);
            _vibration = PlayerPrefs.GetInt(SettingsKeys.Vibration, SettingsKeys.DefaultVibration ? 1 : 0) == 1;
            _controlLayout = (ControlLayoutMode)PlayerPrefs.GetInt(SettingsKeys.ControlLayout, (int)SettingsKeys.DefaultControlLayout);

            // Gameplay
            _damageNumbers = PlayerPrefs.GetInt(SettingsKeys.DamageNumbers, SettingsKeys.DefaultDamageNumbers ? 1 : 0) == 1;
            _screenShake = PlayerPrefs.GetInt(SettingsKeys.ScreenShake, SettingsKeys.DefaultScreenShake ? 1 : 0) == 1;
            _battleNotifications = PlayerPrefs.GetInt(SettingsKeys.BattleNotifications, SettingsKeys.DefaultBattleNotifications ? 1 : 0) == 1;
            _combatVisualEffects = PlayerPrefs.GetInt(SettingsKeys.CombatVisualEffects, SettingsKeys.DefaultCombatVisualEffects ? 1 : 0) == 1;

            // Accessibility
            _uiScale = PlayerPrefs.GetFloat(SettingsKeys.UIScale, SettingsKeys.DefaultUIScale);
            _textSize = (TextSizeMode)PlayerPrefs.GetInt(SettingsKeys.TextSize, (int)SettingsKeys.DefaultTextSize);
            _reduceScreenEffects = PlayerPrefs.GetInt(SettingsKeys.ReduceScreenEffects, SettingsKeys.DefaultReduceScreenEffects ? 1 : 0) == 1;
            _highContrast = PlayerPrefs.GetInt(SettingsKeys.HighContrast, SettingsKeys.DefaultHighContrast ? 1 : 0) == 1;
        }

        public void ApplyAllSettings()
        {
            ApplyAudioSettings();
            ApplyGraphicsSettings();
            ApplyControlsSettings();
            ApplyGameplaySettings();
            ApplyAccessibilitySettings();
        }
        #endregion

        #region Setters & Real-Time Application
        // Audio
        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp(value, 0f, 100f);
            PlayerPrefs.SetFloat(SettingsKeys.MasterVolume, _masterVolume);
            PlayerPrefs.Save();
            ApplyAudioSettings();
            OnMasterVolumeChanged?.Invoke(_masterVolume);
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp(value, 0f, 100f);
            PlayerPrefs.SetFloat(SettingsKeys.MusicVolume, _musicVolume);
            PlayerPrefs.Save();
            OnMusicVolumeChanged?.Invoke(_musicVolume);
        }

        public void SetSFXVolume(float value)
        {
            _sfxVolume = Mathf.Clamp(value, 0f, 100f);
            PlayerPrefs.SetFloat(SettingsKeys.SFXVolume, _sfxVolume);
            PlayerPrefs.Save();
            OnSFXVolumeChanged?.Invoke(_sfxVolume);
        }

        public void SetMuteAll(bool muted)
        {
            _muteAll = muted;
            PlayerPrefs.SetInt(SettingsKeys.MuteAll, _muteAll ? 1 : 0);
            PlayerPrefs.Save();
            ApplyAudioSettings();
            OnMuteAllChanged?.Invoke(_muteAll);
        }

        private void ApplyAudioSettings()
        {
            AudioListener.volume = _muteAll ? 0f : (_masterVolume / 100f);
        }

        // Graphics
        public void SetGraphicsQuality(GraphicsQualityTier tier)
        {
            _graphicsQuality = tier;
            PlayerPrefs.SetInt(SettingsKeys.GraphicsQuality, (int)_graphicsQuality);
            PlayerPrefs.Save();
            ApplyQualityLevel(tier);
            OnGraphicsQualityChanged?.Invoke(_graphicsQuality);
        }

        public void SetFPSLimit(FPSLimitMode mode)
        {
            _fpsLimit = mode;
            PlayerPrefs.SetInt(SettingsKeys.FPSLimit, (int)_fpsLimit);
            PlayerPrefs.Save();
            Application.targetFrameRate = (int)_fpsLimit;
            OnFPSLimitChanged?.Invoke(_fpsLimit);
        }

        public void SetVSync(bool enabled)
        {
            _vSync = enabled;
            PlayerPrefs.SetInt(SettingsKeys.VSync, _vSync ? 1 : 0);
            PlayerPrefs.Save();
            QualitySettings.vSyncCount = _vSync ? 1 : 0;
            OnVSyncChanged?.Invoke(_vSync);
        }

        public void SetFullscreen(bool isFullscreen)
        {
            _fullscreen = isFullscreen;
            PlayerPrefs.SetInt(SettingsKeys.Fullscreen, _fullscreen ? 1 : 0);
            PlayerPrefs.Save();
#if UNITY_STANDALONE || UNITY_EDITOR
            Screen.fullScreen = _fullscreen;
#endif
            OnFullscreenChanged?.Invoke(_fullscreen);
        }

        public void SetResolution(int resolutionIndex)
        {
            _resolutionIndex = resolutionIndex;
            PlayerPrefs.SetInt(SettingsKeys.ResolutionIndex, _resolutionIndex);
            PlayerPrefs.Save();

            Resolution[] resolutions = Screen.resolutions;
            if (resolutions != null && resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
            {
                Resolution target = resolutions[resolutionIndex];
#if UNITY_STANDALONE || UNITY_EDITOR
                Screen.SetResolution(target.width, target.height, _fullscreen);
#endif
            }
            OnResolutionChanged?.Invoke(_resolutionIndex);
        }

        private void ApplyGraphicsSettings()
        {
            ApplyQualityLevel(_graphicsQuality);
            Application.targetFrameRate = (int)_fpsLimit;
            QualitySettings.vSyncCount = _vSync ? 1 : 0;
#if UNITY_STANDALONE || UNITY_EDITOR
            Screen.fullScreen = _fullscreen;
            Resolution[] resolutions = Screen.resolutions;
            if (resolutions != null && _resolutionIndex >= 0 && _resolutionIndex < resolutions.Length)
            {
                Resolution target = resolutions[_resolutionIndex];
                Screen.SetResolution(target.width, target.height, _fullscreen);
            }
#endif
        }

        private void ApplyQualityLevel(GraphicsQualityTier tier)
        {
            int totalLevels = QualitySettings.names.Length;
            if (totalLevels == 0) return;

            int targetLevel = 0;
            switch (tier)
            {
                case GraphicsQualityTier.Low:
                    targetLevel = Mathf.Min(1, totalLevels - 1);
                    break;
                case GraphicsQualityTier.Medium:
                    targetLevel = Mathf.Min(2, totalLevels - 1);
                    break;
                case GraphicsQualityTier.High:
                    targetLevel = Mathf.Min(3, totalLevels - 1);
                    break;
                case GraphicsQualityTier.Ultra:
                    targetLevel = totalLevels - 1;
                    break;
            }
            QualitySettings.SetQualityLevel(targetLevel, true);
        }

        // Controls
        public void SetTouchSensitivity(float value)
        {
            _touchSensitivity = Mathf.Clamp(value, 0f, 100f);
            PlayerPrefs.SetFloat(SettingsKeys.TouchSensitivity, _touchSensitivity);
            PlayerPrefs.Save();
            OnTouchSensitivityChanged?.Invoke(_touchSensitivity);
        }

        public void SetButtonSize(ControlButtonSize size)
        {
            _buttonSize = size;
            PlayerPrefs.SetInt(SettingsKeys.ButtonSize, (int)_buttonSize);
            PlayerPrefs.Save();
            OnButtonSizeChanged?.Invoke(_buttonSize);
        }

        public void SetVibration(bool enabled)
        {
            _vibration = enabled;
            PlayerPrefs.SetInt(SettingsKeys.Vibration, _vibration ? 1 : 0);
            PlayerPrefs.Save();
            OnVibrationChanged?.Invoke(_vibration);
        }

        public void SetControlLayout(ControlLayoutMode layout)
        {
            _controlLayout = layout;
            PlayerPrefs.SetInt(SettingsKeys.ControlLayout, (int)_controlLayout);
            PlayerPrefs.Save();
            OnControlLayoutChanged?.Invoke(_controlLayout);
        }

        private void ApplyControlsSettings()
        {
            OnTouchSensitivityChanged?.Invoke(_touchSensitivity);
            OnButtonSizeChanged?.Invoke(_buttonSize);
            OnVibrationChanged?.Invoke(_vibration);
            OnControlLayoutChanged?.Invoke(_controlLayout);
        }

        // Gameplay
        public void SetDamageNumbers(bool enabled)
        {
            _damageNumbers = enabled;
            PlayerPrefs.SetInt(SettingsKeys.DamageNumbers, _damageNumbers ? 1 : 0);
            PlayerPrefs.Save();
            OnDamageNumbersChanged?.Invoke(_damageNumbers);
        }

        public void SetScreenShake(bool enabled)
        {
            _screenShake = enabled;
            PlayerPrefs.SetInt(SettingsKeys.ScreenShake, _screenShake ? 1 : 0);
            PlayerPrefs.Save();
            OnScreenShakeChanged?.Invoke(_screenShake);
        }

        public void SetBattleNotifications(bool enabled)
        {
            _battleNotifications = enabled;
            PlayerPrefs.SetInt(SettingsKeys.BattleNotifications, _battleNotifications ? 1 : 0);
            PlayerPrefs.Save();
            OnBattleNotificationsChanged?.Invoke(_battleNotifications);
        }

        public void SetCombatVisualEffects(bool enabled)
        {
            _combatVisualEffects = enabled;
            PlayerPrefs.SetInt(SettingsKeys.CombatVisualEffects, _combatVisualEffects ? 1 : 0);
            PlayerPrefs.Save();
            OnCombatVisualEffectsChanged?.Invoke(_combatVisualEffects);
        }

        private void ApplyGameplaySettings()
        {
            OnDamageNumbersChanged?.Invoke(_damageNumbers);
            OnScreenShakeChanged?.Invoke(_screenShake);
            OnBattleNotificationsChanged?.Invoke(_battleNotifications);
            OnCombatVisualEffectsChanged?.Invoke(_combatVisualEffects);
        }

        // Accessibility
        public void SetUIScale(float value)
        {
            _uiScale = Mathf.Clamp(value, 80f, 130f);
            PlayerPrefs.SetFloat(SettingsKeys.UIScale, _uiScale);
            PlayerPrefs.Save();
            OnUIScaleChanged?.Invoke(_uiScale);
        }

        public void SetTextSize(TextSizeMode size)
        {
            _textSize = size;
            PlayerPrefs.SetInt(SettingsKeys.TextSize, (int)_textSize);
            PlayerPrefs.Save();
            OnTextSizeChanged?.Invoke(_textSize);
        }

        public void SetReduceScreenEffects(bool enabled)
        {
            _reduceScreenEffects = enabled;
            PlayerPrefs.SetInt(SettingsKeys.ReduceScreenEffects, _reduceScreenEffects ? 1 : 0);
            PlayerPrefs.Save();
            OnReduceScreenEffectsChanged?.Invoke(_reduceScreenEffects);
        }

        public void SetHighContrast(bool enabled)
        {
            _highContrast = enabled;
            PlayerPrefs.SetInt(SettingsKeys.HighContrast, _highContrast ? 1 : 0);
            PlayerPrefs.Save();
            OnHighContrastChanged?.Invoke(_highContrast);
        }

        private void ApplyAccessibilitySettings()
        {
            OnUIScaleChanged?.Invoke(_uiScale);
            OnTextSizeChanged?.Invoke(_textSize);
            OnReduceScreenEffectsChanged?.Invoke(_reduceScreenEffects);
            OnHighContrastChanged?.Invoke(_highContrast);
        }
        #endregion

        #region Reset to Defaults
        public void ResetToDefaults()
        {
            // Audio
            _masterVolume = SettingsKeys.DefaultMasterVolume;
            _musicVolume = SettingsKeys.DefaultMusicVolume;
            _sfxVolume = SettingsKeys.DefaultSFXVolume;
            _muteAll = SettingsKeys.DefaultMuteAll;

            // Graphics
            _graphicsQuality = SettingsKeys.DefaultGraphicsQuality;
            _fpsLimit = SettingsKeys.DefaultFPSLimit;
            _vSync = SettingsKeys.DefaultVSync;
            _fullscreen = SettingsKeys.DefaultFullscreen;
            _resolutionIndex = GetCurrentResolutionIndex();

            // Controls
            _touchSensitivity = SettingsKeys.DefaultTouchSensitivity;
            _buttonSize = SettingsKeys.DefaultButtonSize;
            _vibration = SettingsKeys.DefaultVibration;
            _controlLayout = SettingsKeys.DefaultControlLayout;

            // Gameplay
            _damageNumbers = SettingsKeys.DefaultDamageNumbers;
            _screenShake = SettingsKeys.DefaultScreenShake;
            _battleNotifications = SettingsKeys.DefaultBattleNotifications;
            _combatVisualEffects = SettingsKeys.DefaultCombatVisualEffects;

            // Accessibility
            _uiScale = SettingsKeys.DefaultUIScale;
            _textSize = SettingsKeys.DefaultTextSize;
            _reduceScreenEffects = SettingsKeys.DefaultReduceScreenEffects;
            _highContrast = SettingsKeys.DefaultHighContrast;

            // Persist all defaults
            PlayerPrefs.SetFloat(SettingsKeys.MasterVolume, _masterVolume);
            PlayerPrefs.SetFloat(SettingsKeys.MusicVolume, _musicVolume);
            PlayerPrefs.SetFloat(SettingsKeys.SFXVolume, _sfxVolume);
            PlayerPrefs.SetInt(SettingsKeys.MuteAll, _muteAll ? 1 : 0);

            PlayerPrefs.SetInt(SettingsKeys.GraphicsQuality, (int)_graphicsQuality);
            PlayerPrefs.SetInt(SettingsKeys.FPSLimit, (int)_fpsLimit);
            PlayerPrefs.SetInt(SettingsKeys.VSync, _vSync ? 1 : 0);
            PlayerPrefs.SetInt(SettingsKeys.Fullscreen, _fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(SettingsKeys.ResolutionIndex, _resolutionIndex);

            PlayerPrefs.SetFloat(SettingsKeys.TouchSensitivity, _touchSensitivity);
            PlayerPrefs.SetInt(SettingsKeys.ButtonSize, (int)_buttonSize);
            PlayerPrefs.SetInt(SettingsKeys.Vibration, _vibration ? 1 : 0);
            PlayerPrefs.SetInt(SettingsKeys.ControlLayout, (int)_controlLayout);

            PlayerPrefs.SetInt(SettingsKeys.DamageNumbers, _damageNumbers ? 1 : 0);
            PlayerPrefs.SetInt(SettingsKeys.ScreenShake, _screenShake ? 1 : 0);
            PlayerPrefs.SetInt(SettingsKeys.BattleNotifications, _battleNotifications ? 1 : 0);
            PlayerPrefs.SetInt(SettingsKeys.CombatVisualEffects, _combatVisualEffects ? 1 : 0);

            PlayerPrefs.SetFloat(SettingsKeys.UIScale, _uiScale);
            PlayerPrefs.SetInt(SettingsKeys.TextSize, (int)_textSize);
            PlayerPrefs.SetInt(SettingsKeys.ReduceScreenEffects, _reduceScreenEffects ? 1 : 0);
            PlayerPrefs.SetInt(SettingsKeys.HighContrast, _highContrast ? 1 : 0);

            PlayerPrefs.Save();

            ApplyAllSettings();
            OnSettingsReset?.Invoke();
        }
        #endregion

        #region Helpers
        public static int GetCurrentResolutionIndex()
        {
            Resolution[] res = Screen.resolutions;
            if (res == null || res.Length == 0) return 0;

            for (int i = 0; i < res.Length; i++)
            {
                if (res[i].width == Screen.width && res[i].height == Screen.height)
                {
                    return i;
                }
            }
            return Mathf.Max(0, res.Length - 1);
        }
        #endregion
    }
}
