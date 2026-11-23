using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Clashura.Characters.Data;
using Clashura.Characters.Managers;

namespace Clashura.Multiplayer
{
    /// <summary>
    /// Controller for the Versus (VS) Matchup Panel.
    /// Displays both players' names, levels, ranking points, and 3-hero selected teams
    /// with full-screen fighting game aesthetics, screen shake, impact flashes, and dynamic countdown.
    /// </summary>
    public class VersusMatchupUI : MonoBehaviour
    {
        [Header("Root & Database")]
        [SerializeField] private GameObject versusPanelRoot;
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private CharacterDatabaseSO characterDatabase;

        [Header("Center VS Elements")]
        [SerializeField] private RectTransform vsCenterContainer;
        [SerializeField] private RectTransform vsBadgeRect;
        [SerializeField] private Image vsGlowBurstImage;
        [SerializeField] private RectTransform energyRaysRect;
        [SerializeField] private RectTransform dividerBeamRect;
        [SerializeField] private TMP_Text vsMatchTitleText;
        [SerializeField] private TMP_Text vsArenaNameText;
        [SerializeField] private TMP_Text vsCountdownText;

        [Header("FX Overlays")]
        [SerializeField] private CanvasGroup screenFlashGroup;

        [Header("Player 1 (Left - Host)")]
        [SerializeField] private RectTransform p1CardRect;
        [SerializeField] private CanvasGroup p1CanvasGroup;
        [SerializeField] private TMP_Text p1TagText;
        [SerializeField] private TMP_Text p1NameText;
        [SerializeField] private TMP_Text p1LevelText;
        [SerializeField] private TMP_Text p1PointsText;
        [SerializeField] private TMP_Text p1TierText;
        [SerializeField] private Image[] p1HeroPortraits = new Image[3];
        [SerializeField] private TMP_Text[] p1HeroNames = new TMP_Text[3];
        [SerializeField] private TMP_Text[] p1HeroRoles = new TMP_Text[3];
        [SerializeField] private CanvasGroup[] p1HeroSlotGroups = new CanvasGroup[3];

        [Header("Player 2 (Right - Challenger)")]
        [SerializeField] private RectTransform p2CardRect;
        [SerializeField] private CanvasGroup p2CanvasGroup;
        [SerializeField] private TMP_Text p2TagText;
        [SerializeField] private TMP_Text p2NameText;
        [SerializeField] private TMP_Text p2LevelText;
        [SerializeField] private TMP_Text p2PointsText;
        [SerializeField] private TMP_Text p2TierText;
        [SerializeField] private Image[] p2HeroPortraits = new Image[3];
        [SerializeField] private TMP_Text[] p2HeroNames = new TMP_Text[3];
        [SerializeField] private TMP_Text[] p2HeroRoles = new TMP_Text[3];
        [SerializeField] private CanvasGroup[] p2HeroSlotGroups = new CanvasGroup[3];

        private Coroutine _matchupSequenceCoroutine;
        private Coroutine _ambientPulseCoroutine;
        private Coroutine _shakeCoroutine;
        private Coroutine _flashCoroutine;
        private bool _isPlayingSequence;

        public bool IsShowing => versusPanelRoot != null && versusPanelRoot.activeSelf;

        private void Awake()
        {
            if (versusPanelRoot == null) versusPanelRoot = gameObject;
            if (rootCanvasGroup == null) rootCanvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// Populates match data and starts the animated presentation sequence.
        /// </summary>
        public void ShowMatchup(PlayerMatchProfile p1Profile, PlayerMatchProfile p2Profile, string roomName, Action onComplete = null)
        {
            if (versusPanelRoot != null)
            {
                versusPanelRoot.SetActive(true);
                versusPanelRoot.transform.SetAsLastSibling();
            }

            PopulatePlayer1(p1Profile);
            PopulatePlayer2(p2Profile);

            if (vsArenaNameText != null)
            {
                vsArenaNameText.text = string.IsNullOrWhiteSpace(roomName) ? "ARENA: BATTLEGROUND" : $"ARENA: {roomName.ToUpper()}";
            }

            if (_matchupSequenceCoroutine != null) StopCoroutine(_matchupSequenceCoroutine);
            _matchupSequenceCoroutine = StartCoroutine(MatchupAnimationSequence(onComplete));
        }

        /// <summary>
        /// Immediately hides the versus matchup screen.
        /// </summary>
        public void HideMatchup()
        {
            if (_matchupSequenceCoroutine != null)
            {
                StopCoroutine(_matchupSequenceCoroutine);
                _matchupSequenceCoroutine = null;
            }

            if (_ambientPulseCoroutine != null)
            {
                StopCoroutine(_ambientPulseCoroutine);
                _ambientPulseCoroutine = null;
            }

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }

            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }

            _isPlayingSequence = false;
            if (screenFlashGroup != null)
            {
                screenFlashGroup.alpha = 0f;
                screenFlashGroup.gameObject.SetActive(false);
            }

            if (versusPanelRoot != null) versusPanelRoot.SetActive(false);
        }

        #region Data Population

        private static string GetTierName(int rating)
        {
            if (rating >= 1700) return "DIAMOND TIER I";
            if (rating >= 1400) return "PLATINUM TIER II";
            if (rating >= 1100) return "GOLD TIER III";
            if (rating >= 800) return "SILVER TIER II";
            return "BRONZE TIER I";
        }

        private void PopulatePlayer1(PlayerMatchProfile p1)
        {
            if (p1 == null) p1 = PlayerMatchProfile.CreateLocalProfile(true);

            if (p1NameText != null) p1NameText.text = p1.playerName.ToUpper();
            if (p1LevelText != null) p1LevelText.text = $"LVL {p1.playerLevel}";
            if (p1PointsText != null) p1PointsText.text = $"{p1.playerPoints} ELO";
            if (p1TagText != null) p1TagText.text = "HOST · ARENA CHAMPION";
            if (p1TierText != null) p1TierText.text = GetTierName(p1.playerPoints);

            PopulateTeam(p1.teamCharacterIds, p1HeroPortraits, p1HeroNames, p1HeroRoles);
        }

        private void PopulatePlayer2(PlayerMatchProfile p2)
        {
            if (p2 == null) p2 = PlayerMatchProfile.CreateRivalPreviewProfile();

            if (p2NameText != null) p2NameText.text = p2.playerName.ToUpper();
            if (p2LevelText != null) p2LevelText.text = $"LVL {p2.playerLevel}";
            if (p2PointsText != null) p2PointsText.text = $"{p2.playerPoints} ELO";
            if (p2TagText != null) p2TagText.text = "CHALLENGER · RIVAL";
            if (p2TierText != null) p2TierText.text = GetTierName(p2.playerPoints);

            PopulateTeam(p2.teamCharacterIds, p2HeroPortraits, p2HeroNames, p2HeroRoles);
        }

        private void PopulateTeam(List<string> characterIds, Image[] portraitImgs, TMP_Text[] nameTexts, TMP_Text[] roleTexts)
        {
            if (characterDatabase == null)
            {
                characterDatabase = Resources.Load<CharacterDatabaseSO>("CharacterDatabase");
            }

            for (int i = 0; i < 3; i++)
            {
                string id = (characterIds != null && i < characterIds.Count) ? characterIds[i] : null;
                CharacterDataSO charData = null;

                if (!string.IsNullOrEmpty(id) && characterDatabase != null)
                {
                    charData = characterDatabase.GetCharacterById(id);
                }

                if (charData == null && characterDatabase != null && characterDatabase.CharacterCount > i)
                {
                    charData = characterDatabase.GetCharacterByIndex(i);
                }

                if (portraitImgs != null && i < portraitImgs.Length && portraitImgs[i] != null)
                {
                    if (charData != null)
                    {
                        Sprite portrait = charData.SelectionArtwork != null ? charData.SelectionArtwork : charData.Portrait;
                        portraitImgs[i].sprite = portrait;
                        portraitImgs[i].color = Color.white;
                    }
                    else
                    {
                        portraitImgs[i].color = new Color(0.2f, 0.25f, 0.35f, 0.8f);
                    }
                }

                if (nameTexts != null && i < nameTexts.Length && nameTexts[i] != null)
                {
                    nameTexts[i].text = charData != null ? charData.CharacterName.ToUpper() : $"HERO #{i + 1}";
                }

                if (roleTexts != null && i < roleTexts.Length && roleTexts[i] != null)
                {
                    string roleStr = charData != null ? charData.Role.ToString().ToUpper() : "WARRIOR";
                    roleTexts[i].text = roleStr;

                    // Color code roles
                    switch (roleStr)
                    {
                        case "VANGUARD":
                            roleTexts[i].color = new Color(1.0f, 0.85f, 0.30f); // Gold
                            break;
                        case "STRIKER":
                            roleTexts[i].color = new Color(1.0f, 0.35f, 0.30f); // Crimson Flame
                            break;
                        case "DUELIST":
                            roleTexts[i].color = new Color(0.35f, 0.85f, 1.0f); // Electric Sky
                            break;
                        case "SPECIALIST":
                            roleTexts[i].color = new Color(0.85f, 0.45f, 1.0f); // Mystic Violet
                            break;
                        default:
                            roleTexts[i].color = new Color(0.9f, 0.9f, 0.95f);
                            break;
                    }
                }
            }
        }

        #endregion

        #region Animation Sequence

        private IEnumerator MatchupAnimationSequence(Action onComplete)
        {
            _isPlayingSequence = true;

            if (rootCanvasGroup != null) rootCanvasGroup.alpha = 1f;

            float p1TargetX = -485f;
            float p2TargetX = 485f;

            // Step 0: Hide elements off-screen initially
            if (p1CardRect != null)
            {
                p1CardRect.anchoredPosition = new Vector2(-1800f, p1CardRect.anchoredPosition.y);
            }
            if (p1CanvasGroup != null) p1CanvasGroup.alpha = 0f;

            if (p2CardRect != null)
            {
                p2CardRect.anchoredPosition = new Vector2(1800f, p2CardRect.anchoredPosition.y);
            }
            if (p2CanvasGroup != null) p2CanvasGroup.alpha = 0f;

            if (vsBadgeRect != null) vsBadgeRect.localScale = Vector3.zero;
            if (vsGlowBurstImage != null) vsGlowBurstImage.color = new Color(1f, 1f, 1f, 0f);
            if (energyRaysRect != null) energyRaysRect.localScale = Vector3.zero;

            if (dividerBeamRect != null)
            {
                dividerBeamRect.localScale = new Vector3(1f, 0f, 1f);
            }

            if (screenFlashGroup != null)
            {
                screenFlashGroup.alpha = 0f;
                screenFlashGroup.gameObject.SetActive(false);
            }

            SetHeroSlotAlphas(p1HeroSlotGroups, 0f, 0.5f, -80f);
            SetHeroSlotAlphas(p2HeroSlotGroups, 0f, 0.5f, -80f);

            if (vsCountdownText != null)
            {
                vsCountdownText.text = "MATCH FOUND!";
                vsCountdownText.color = new Color(1f, 0.88f, 0.35f);
            }

            yield return new WaitForSeconds(0.08f);

            // Step 1: High-Speed Card Swoosh with Overshoot (0.45s)
            float slideDuration = 0.45f;
            float elapsed = 0f;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                // EaseOutBack curve for punchy overshoot deceleration
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float easeOutBack = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);

                if (p1CardRect != null)
                {
                    p1CardRect.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-1800f, p1TargetX, easeOutBack), p1CardRect.anchoredPosition.y);
                }
                if (p1CanvasGroup != null) p1CanvasGroup.alpha = Mathf.Clamp01(t * 2f);

                if (p2CardRect != null)
                {
                    p2CardRect.anchoredPosition = new Vector2(Mathf.LerpUnclamped(1800f, p2TargetX, easeOutBack), p2CardRect.anchoredPosition.y);
                }
                if (p2CanvasGroup != null) p2CanvasGroup.alpha = Mathf.Clamp01(t * 2f);

                yield return null;
            }

            if (p1CardRect != null) p1CardRect.anchoredPosition = new Vector2(p1TargetX, p1CardRect.anchoredPosition.y);
            if (p2CardRect != null) p2CardRect.anchoredPosition = new Vector2(p2TargetX, p2CardRect.anchoredPosition.y);

            // Card collision impact shake
            TriggerShake(12f, 0.22f);

            // Step 2: Divider Beam Ignites Vertically
            if (dividerBeamRect != null)
            {
                StartCoroutine(IgniteDividerBeam(0.25f));
            }

            yield return new WaitForSeconds(0.05f);

            // Step 3: Massive VS Badge Slam-Down from High Scale (3.4 -> 1.0) with Shockwave & Screen Flash
            TriggerFlash(0.75f, 0.20f);
            TriggerShake(24f, 0.35f);

            float slamDuration = 0.32f;
            elapsed = 0f;

            while (elapsed < slamDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slamDuration);
                // Heavy impact slam curve: 3.4 -> 0.85 -> 1.05 -> 1.0
                float scale;
                if (t < 0.65f)
                {
                    scale = Mathf.Lerp(3.4f, 0.85f, t / 0.65f);
                }
                else
                {
                    scale = Mathf.Lerp(0.85f, 1.0f, (t - 0.65f) / 0.35f);
                }

                if (vsBadgeRect != null) vsBadgeRect.localScale = new Vector3(scale, scale, 1f);
                if (vsGlowBurstImage != null)
                {
                    float burstScale = Mathf.Lerp(0.4f, 1.6f, t);
                    vsGlowBurstImage.rectTransform.localScale = new Vector3(burstScale, burstScale, 1f);
                    vsGlowBurstImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.95f, 0.70f, t));
                }
                if (energyRaysRect != null)
                {
                    float rayScale = Mathf.Lerp(0.2f, 1.0f, t);
                    energyRaysRect.localScale = new Vector3(rayScale, rayScale, 1f);
                }

                yield return null;
            }

            if (vsBadgeRect != null) vsBadgeRect.localScale = Vector3.one;

            // Start ambient loop
            if (_ambientPulseCoroutine != null) StopCoroutine(_ambientPulseCoroutine);
            _ambientPulseCoroutine = StartCoroutine(AmbientVSPulseRoutine());

            // Step 4: Staggered spring reveal of hero cards (0.10s apart)
            for (int i = 0; i < 3; i++)
            {
                StartCoroutine(PopInHeroSlot(p1HeroSlotGroups != null && i < p1HeroSlotGroups.Length ? p1HeroSlotGroups[i] : null));
                StartCoroutine(PopInHeroSlot(p2HeroSlotGroups != null && i < p2HeroSlotGroups.Length ? p2HeroSlotGroups[i] : null));
                yield return new WaitForSeconds(0.10f);
            }

            yield return new WaitForSeconds(0.25f);

            // Step 5: High-Impact Dramatic Countdown (3... 2... 1... FIGHT!)
            int countdownSeconds = 3;
            Color[] countColors = new Color[]
            {
                new Color(1.0f, 0.85f, 0.30f), // 3 - Gold
                new Color(0.35f, 0.85f, 1.0f), // 2 - Cyan
                new Color(1.0f, 0.45f, 0.20f)  // 1 - Flaming Orange
            };

            while (countdownSeconds > 0)
            {
                if (vsCountdownText != null)
                {
                    vsCountdownText.text = $"{countdownSeconds}";
                    vsCountdownText.color = countColors[3 - countdownSeconds];
                    vsCountdownText.fontSize = 58;
                    StartCoroutine(PunchScale(vsCountdownText.rectTransform, 2.2f, 0.30f));
                    TriggerShake(7f, 0.15f);
                }
                yield return new WaitForSeconds(1.0f);
                countdownSeconds--;
            }

            // Final FIGHT! banner explosion
            if (vsCountdownText != null)
            {
                vsCountdownText.text = "FIGHT!";
                vsCountdownText.color = new Color(1.0f, 0.25f, 0.15f);
                vsCountdownText.fontSize = 72;
                StartCoroutine(PunchScale(vsCountdownText.rectTransform, 2.5f, 0.45f));
            }

            TriggerFlash(0.90f, 0.35f);
            TriggerShake(32f, 0.55f);

            yield return new WaitForSeconds(0.9f);

            _isPlayingSequence = false;
            onComplete?.Invoke();
        }

        private IEnumerator IgniteDividerBeam(float duration)
        {
            if (dividerBeamRect == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                dividerBeamRect.localScale = new Vector3(1f, Mathf.Sin(t * Mathf.PI * 0.5f), 1f);
                yield return null;
            }
            dividerBeamRect.localScale = Vector3.one;
        }

        private IEnumerator PopInHeroSlot(CanvasGroup group)
        {
            if (group == null) yield break;

            RectTransform rt = group.GetComponent<RectTransform>();
            float duration = 0.30f;
            float elapsed = 0f;
            Vector2 startPos = rt != null ? new Vector2(rt.anchoredPosition.x, -60f) : Vector2.zero;
            Vector2 targetPos = rt != null ? new Vector2(rt.anchoredPosition.x, 0f) : Vector2.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeOutBack = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);

                group.alpha = Mathf.Clamp01(t * 1.5f);
                if (rt != null)
                {
                    float s = Mathf.Clamp(easeOutBack, 0.5f, 1.15f);
                    rt.localScale = new Vector3(s, s, 1f);
                    rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                }
                yield return null;
            }

            group.alpha = 1f;
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchoredPosition = targetPos;
            }
        }

        private IEnumerator AmbientVSPulseRoutine()
        {
            float timer = 0f;
            while (_isPlayingSequence)
            {
                timer += Time.deltaTime * 3.5f;
                float breath = 1f + 0.05f * Mathf.Sin(timer);

                if (vsBadgeRect != null)
                {
                    vsBadgeRect.localScale = new Vector3(breath, breath, 1f);
                }

                if (vsGlowBurstImage != null)
                {
                    vsGlowBurstImage.rectTransform.Rotate(0f, 0f, Time.deltaTime * 18f);
                    vsGlowBurstImage.color = new Color(1f, 1f, 1f, 0.70f + 0.25f * Mathf.Sin(timer));
                }

                if (energyRaysRect != null)
                {
                    energyRaysRect.Rotate(0f, 0f, -Time.deltaTime * 15f);
                }

                if (dividerBeamRect != null)
                {
                    float pulse = 0.85f + 0.15f * Mathf.Sin(timer * 2f);
                    dividerBeamRect.localScale = new Vector3(pulse, 1f, 1f);
                }

                yield return null;
            }
        }

        private IEnumerator PunchScale(RectTransform rt, float maxScale, float duration)
        {
            if (rt == null) yield break;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float s = (t < 0.35f) ? Mathf.Lerp(1.0f, maxScale, t / 0.35f) : Mathf.Lerp(maxScale, 1.0f, (t - 0.35f) / 0.65f);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        #endregion

        #region FX: Screen Shake & Flash

        public void TriggerShake(float intensity, float duration)
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeRoutine(vsCenterContainer != null ? vsCenterContainer : (RectTransform)transform, intensity, duration));
        }

        private IEnumerator ShakeRoutine(RectTransform target, float intensity, float duration)
        {
            if (target == null) yield break;
            Vector2 origin = target.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float damp = 1f - Mathf.Clamp01(elapsed / duration);
                float rx = UnityEngine.Random.Range(-intensity, intensity) * damp;
                float ry = UnityEngine.Random.Range(-intensity, intensity) * damp;
                target.anchoredPosition = origin + new Vector2(rx, ry);
                yield return null;
            }

            target.anchoredPosition = origin;
            _shakeCoroutine = null;
        }

        public void TriggerFlash(float maxAlpha, float duration)
        {
            if (screenFlashGroup == null) return;
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine(maxAlpha, duration));
        }

        private IEnumerator FlashRoutine(float maxAlpha, float duration)
        {
            if (screenFlashGroup == null) yield break;
            screenFlashGroup.gameObject.SetActive(true);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                screenFlashGroup.alpha = Mathf.Lerp(maxAlpha, 0f, t);
                yield return null;
            }

            screenFlashGroup.alpha = 0f;
            screenFlashGroup.gameObject.SetActive(false);
            _flashCoroutine = null;
        }

        private void SetHeroSlotAlphas(CanvasGroup[] groups, float alpha, float scale, float offsetY = 0f)
        {
            if (groups == null) return;
            foreach (var g in groups)
            {
                if (g != null)
                {
                    g.alpha = alpha;
                    var rt = g.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.localScale = new Vector3(scale, scale, 1f);
                        if (offsetY != 0f)
                        {
                            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, offsetY);
                        }
                    }
                }
            }
        }

        #endregion

        #region Editor / Preview Helper

        [ContextMenu("Preview VS Screen With Mock Data")]
        public void PreviewWithMockData()
        {
            var p1 = PlayerMatchProfile.CreateLocalProfile(true);
            var p2 = PlayerMatchProfile.CreateRivalPreviewProfile();
            ShowMatchup(p1, p2, "TitanDuel", () => Debug.Log("<color=cyan>[VersusMatchupUI] Countdown Finished! Ready for battle.</color>"));
        }

        #endregion
    }
}
