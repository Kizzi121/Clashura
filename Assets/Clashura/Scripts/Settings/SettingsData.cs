using System;
using UnityEngine;

namespace Clashura.Settings
{
    public enum SettingsCategory
    {
        Audio = 0,
        Graphics = 1,
        Controls = 2,
        Gameplay = 3,
        Accessibility = 4
    }

    public enum GraphicsQualityTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    public enum FPSLimitMode
    {
        FPS_30 = 30,
        FPS_60 = 60,
        FPS_90 = 90,
        FPS_120 = 120,
        Unlimited = -1
    }

    public enum ControlButtonSize
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }

    public enum ControlLayoutMode
    {
        Default = 0,
        Inverted = 1,
        LeftHanded = 2
    }

    public enum TextSizeMode
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }

    /// <summary>
    /// Constants and PlayerPrefs key definitions for CLASHURA Settings.
    /// All keys are prefixed with 'Clashura_' to prevent conflict with other game systems.
    /// </summary>
    public static class SettingsKeys
    {
        // Audio
        public const string MasterVolume = "Clashura_MasterVolume";
        public const string MusicVolume = "Clashura_MusicVolume";
        public const string SFXVolume = "Clashura_SFXVolume";
        public const string MuteAll = "Clashura_MuteAll";

        // Graphics
        public const string GraphicsQuality = "Clashura_GraphicsQuality";
        public const string FPSLimit = "Clashura_FPSLimit";
        public const string VSync = "Clashura_VSync";
        public const string Fullscreen = "Clashura_Fullscreen";
        public const string ResolutionIndex = "Clashura_ResolutionIndex";

        // Controls
        public const string TouchSensitivity = "Clashura_TouchSensitivity";
        public const string ButtonSize = "Clashura_ButtonSize";
        public const string Vibration = "Clashura_Vibration";
        public const string ControlLayout = "Clashura_ControlLayout";

        // Gameplay
        public const string DamageNumbers = "Clashura_DamageNumbers";
        public const string ScreenShake = "Clashura_ScreenShake";
        public const string BattleNotifications = "Clashura_BattleNotifications";
        public const string CombatVisualEffects = "Clashura_CombatVisualEffects";

        // Accessibility
        public const string UIScale = "Clashura_UIScale";
        public const string TextSize = "Clashura_TextSize";
        public const string ReduceScreenEffects = "Clashura_ReduceScreenEffects";
        public const string HighContrast = "Clashura_HighContrast";

        // Defaults
        public const float DefaultMasterVolume = 80.0f;
        public const float DefaultMusicVolume = 75.0f;
        public const float DefaultSFXVolume = 85.0f;
        public const bool DefaultMuteAll = false;

        public const GraphicsQualityTier DefaultGraphicsQuality = GraphicsQualityTier.High;
        public const FPSLimitMode DefaultFPSLimit = FPSLimitMode.FPS_60;
        public const bool DefaultVSync = false;
        public const bool DefaultFullscreen = true;

        public const float DefaultTouchSensitivity = 50.0f;
        public const ControlButtonSize DefaultButtonSize = ControlButtonSize.Medium;
        public const bool DefaultVibration = true;
        public const ControlLayoutMode DefaultControlLayout = ControlLayoutMode.Default;

        public const bool DefaultDamageNumbers = true;
        public const bool DefaultScreenShake = true;
        public const bool DefaultBattleNotifications = true;
        public const bool DefaultCombatVisualEffects = true;

        public const float DefaultUIScale = 100.0f;
        public const TextSizeMode DefaultTextSize = TextSizeMode.Medium;
        public const bool DefaultReduceScreenEffects = false;
        public const bool DefaultHighContrast = false;
    }
}
