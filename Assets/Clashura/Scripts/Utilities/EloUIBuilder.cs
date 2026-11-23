#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using Clashura.Elo;
using Clashura.Multiplayer;

namespace Clashura.Utilities
{
    public static class EloUIBuilder
    {
        [MenuItem("Clashura/Multiplayer/Build Elo Matchmaking UI", false, 110)]
        public static void BuildEloMatchmakingUI()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/Clashura/Scenes/Multiplayer/PvP_Matchmaking.unity", 
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            // 1. Ensure [MatchmakingManagers] has EloManager and EloHttpServer
            var managersGo = GameObject.Find("[MatchmakingManagers]");
            if (managersGo != null)
            {
                if (managersGo.GetComponent<EloManager>() == null)
                {
                    managersGo.AddComponent<EloManager>();
                }
                if (managersGo.GetComponent<EloHttpServer>() == null)
                {
                    managersGo.AddComponent<EloHttpServer>();
                }
            }

            var safeArea = GameObject.Find("MatchmakingCanvas/SafeArea");
            if (safeArea == null)
            {
                Debug.LogError("[EloUIBuilder] MatchmakingCanvas/SafeArea not found!");
                return;
            }

            var card = GameObject.Find("MatchmakingCanvas/SafeArea/MatchmakingCard");
            var mmUi = UnityEngine.Object.FindFirstObjectByType<MatchmakingUI>();

            // 2. Build or update RatingBadge in MatchmakingCard
            var existingBadge = card.transform.Find("RatingBadge");
            if (existingBadge != null)
            {
                UnityEngine.Object.DestroyImmediate(existingBadge.gameObject);
            }

            GameObject badgeGo = new GameObject("RatingBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
            badgeGo.transform.SetParent(card.transform, false);

            var badgeRect = badgeGo.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 0f);
            badgeRect.anchorMax = new Vector2(0.5f, 0f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(0f, 155f);
            badgeRect.sizeDelta = new Vector2(480f, 38f);

            var badgeImg = badgeGo.GetComponent<Image>();
            badgeImg.color = new Color(0.06f, 0.12f, 0.22f, 0.90f);

            var outline = badgeGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.80f, 0.30f, 0.55f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var badgeTextGo = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeTextGo.transform.SetParent(badgeGo.transform, false);
            var badgeTextRect = badgeTextGo.GetComponent<RectTransform>();
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            badgeTextRect.offsetMin = Vector2.zero;
            badgeTextRect.offsetMax = Vector2.zero;

            var badgeText = badgeTextGo.GetComponent<TextMeshProUGUI>();
            badgeText.text = "YOUR RATING: 1,000 ELO · 0W - 0L";
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.fontSize = 17f;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.color = new Color(1.0f, 0.88f, 0.40f);

            // Bind to MatchmakingUI
            if (mmUi != null)
            {
                var badgeField = typeof(MatchmakingUI).GetField("playerRatingBadgeText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                badgeField?.SetValue(mmUi, badgeText);
                EditorUtility.SetDirty(mmUi);
            }

            // 3. Build MatchResultPanel under SafeArea
            var existingResult = safeArea.transform.Find("MatchResultPanel");
            if (existingResult != null)
            {
                UnityEngine.Object.DestroyImmediate(existingResult.gameObject);
            }

            GameObject resultGo = new GameObject("MatchResultPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(MatchResultUI));
            resultGo.transform.SetParent(safeArea.transform, false);

            var resultRect = resultGo.GetComponent<RectTransform>();
            resultRect.anchorMin = Vector2.zero;
            resultRect.anchorMax = Vector2.one;
            resultRect.offsetMin = Vector2.zero;
            resultRect.offsetMax = Vector2.zero;

            var resultCg = resultGo.GetComponent<CanvasGroup>();
            var resultOverlayImg = resultGo.GetComponent<Image>();
            resultOverlayImg.color = new Color(0.02f, 0.04f, 0.08f, 0.95f);

            var resultComp = resultGo.GetComponent<MatchResultUI>();

            // Center Result Card
            var centerCardGo = new GameObject("CenterCard", typeof(RectTransform), typeof(Image), typeof(Outline));
            centerCardGo.transform.SetParent(resultGo.transform, false);
            var centerCardRect = centerCardGo.GetComponent<RectTransform>();
            centerCardRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerCardRect.pivot = new Vector2(0.5f, 0.5f);
            centerCardRect.sizeDelta = new Vector2(980f, 620f);
            centerCardRect.anchoredPosition = Vector2.zero;

            var centerCardImg = centerCardGo.GetComponent<Image>();
            centerCardImg.color = new Color(0.05f, 0.08f, 0.15f, 0.98f);
            var centerOutline = centerCardGo.GetComponent<Outline>();
            centerOutline.effectColor = new Color(0.85f, 0.70f, 0.25f, 0.6f);
            centerOutline.effectDistance = new Vector2(2f, -2f);

            // Banner Title ("VICTORY!" / "DEFEAT")
            var bannerGo = new GameObject("BannerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bannerGo.transform.SetParent(centerCardGo.transform, false);
            var bannerRect = bannerGo.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 1f);
            bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.anchoredPosition = new Vector2(0f, -25f);
            bannerRect.sizeDelta = new Vector2(900f, 70f);

            var bannerText = bannerGo.GetComponent<TextMeshProUGUI>();
            bannerText.text = "VICTORY!";
            bannerText.fontSize = 52f;
            bannerText.fontStyle = FontStyles.Bold;
            bannerText.alignment = TextAlignmentOptions.Center;
            bannerText.color = new Color(0.25f, 1.0f, 0.50f);

            // Subtitle
            var subGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGo.transform.SetParent(centerCardGo.transform, false);
            var subRect = subGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0f, -95f);
            subRect.sizeDelta = new Vector2(900f, 30f);

            var subText = subGo.GetComponent<TextMeshProUGUI>();
            subText.text = "OFFICIAL ELO RATING UPDATED IN DATABASE";
            subText.fontSize = 18f;
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = new Color(0.70f, 0.85f, 1.0f);

            // Match ID Text
            var matchIdGo = new GameObject("MatchIdText", typeof(RectTransform), typeof(TextMeshProUGUI));
            matchIdGo.transform.SetParent(centerCardGo.transform, false);
            var matchIdRect = matchIdGo.GetComponent<RectTransform>();
            matchIdRect.anchorMin = new Vector2(0.5f, 1f);
            matchIdRect.anchorMax = new Vector2(0.5f, 1f);
            matchIdRect.pivot = new Vector2(0.5f, 1f);
            matchIdRect.anchoredPosition = new Vector2(0f, -125f);
            matchIdRect.sizeDelta = new Vector2(900f, 25f);

            var matchIdText = matchIdGo.GetComponent<TextMeshProUGUI>();
            matchIdText.text = "MATCH ID: MATCH_0000 · STATUS: VERIFIED";
            matchIdText.fontSize = 15f;
            matchIdText.alignment = TextAlignmentOptions.Center;
            matchIdText.color = new Color(0.85f, 0.75f, 0.40f);

            // Left Card: Player Result
            var p1CardGo = new GameObject("PlayerCard", typeof(RectTransform), typeof(Image), typeof(Outline));
            p1CardGo.transform.SetParent(centerCardGo.transform, false);
            var p1CardRect = p1CardGo.GetComponent<RectTransform>();
            p1CardRect.anchorMin = new Vector2(0.5f, 0.5f);
            p1CardRect.anchorMax = new Vector2(0.5f, 0.5f);
            p1CardRect.pivot = new Vector2(0.5f, 0.5f);
            p1CardRect.anchoredPosition = new Vector2(-230f, -20f);
            p1CardRect.sizeDelta = new Vector2(410f, 290f);

            var p1CardImg = p1CardGo.GetComponent<Image>();
            p1CardImg.color = new Color(0.08f, 0.14f, 0.25f, 0.95f);
            var p1Outline = p1CardGo.GetComponent<Outline>();
            p1Outline.effectColor = new Color(0.25f, 0.65f, 1.0f, 0.5f);

            var p1NameText = CreateCardText(p1CardGo, "NameText", "YOU (PLAYER 1)", 24f, new Vector2(0f, -20f), FontStyles.Bold, Color.white);
            var p1OldRatingText = CreateCardText(p1CardGo, "OldRatingText", "OLD RATING: 400 ELO", 18f, new Vector2(0f, -70f), FontStyles.Normal, new Color(0.8f, 0.85f, 0.95f));
            var p1DeltaText = CreateCardText(p1CardGo, "DeltaText", "RATING GAINED: +18 ELO", 24f, new Vector2(0f, -125f), FontStyles.Bold, new Color(0.3f, 1.0f, 0.5f));
            var p1NewRatingText = CreateCardText(p1CardGo, "NewRatingText", "NEW RATING: 418 ELO", 22f, new Vector2(0f, -180f), FontStyles.Bold, new Color(1.0f, 0.88f, 0.4f));
            var p1CareerText = CreateCardText(p1CardGo, "CareerText", "RECORD: 1W - 0L (1 Matches)", 16f, new Vector2(0f, -235f), FontStyles.Normal, new Color(0.7f, 0.8f, 0.9f));

            // Right Card: Opponent Result
            var p2CardGo = new GameObject("OpponentCard", typeof(RectTransform), typeof(Image), typeof(Outline));
            p2CardGo.transform.SetParent(centerCardGo.transform, false);
            var p2CardRect = p2CardGo.GetComponent<RectTransform>();
            p2CardRect.anchorMin = new Vector2(0.5f, 0.5f);
            p2CardRect.anchorMax = new Vector2(0.5f, 0.5f);
            p2CardRect.pivot = new Vector2(0.5f, 0.5f);
            p2CardRect.anchoredPosition = new Vector2(230f, -20f);
            p2CardRect.sizeDelta = new Vector2(410f, 290f);

            var p2CardImg = p2CardGo.GetComponent<Image>();
            p2CardImg.color = new Color(0.18f, 0.08f, 0.12f, 0.95f);
            var p2Outline = p2CardGo.GetComponent<Outline>();
            p2Outline.effectColor = new Color(1.0f, 0.35f, 0.35f, 0.5f);

            var p2NameText = CreateCardText(p2CardGo, "NameText", "OPPONENT", 24f, new Vector2(0f, -20f), FontStyles.Bold, Color.white);
            var p2OldRatingText = CreateCardText(p2CardGo, "OldRatingText", "OLD RATING: 450 ELO", 18f, new Vector2(0f, -70f), FontStyles.Normal, new Color(0.8f, 0.85f, 0.95f));
            var p2DeltaText = CreateCardText(p2CardGo, "DeltaText", "RATING LOST: -18 ELO", 24f, new Vector2(0f, -125f), FontStyles.Bold, new Color(1.0f, 0.4f, 0.4f));
            var p2NewRatingText = CreateCardText(p2CardGo, "NewRatingText", "NEW RATING: 432 ELO", 22f, new Vector2(0f, -180f), FontStyles.Bold, new Color(1.0f, 0.88f, 0.4f));
            CreateCardText(p2CardGo, "StatusText", "DATABASE SYNCHRONIZED", 16f, new Vector2(0f, -235f), FontStyles.Normal, new Color(0.6f, 0.8f, 0.7f));

            // Buttons: Play Again & Main Menu
            var playAgainBtn = CreateButton(centerCardGo, "Btn_PlayAgain", "RETURN TO MATCHMAKING", new Vector2(-160f, -260f), new Vector2(280f, 50f), new Color(0.15f, 0.50f, 0.25f));
            var mainMenuBtn = CreateButton(centerCardGo, "Btn_MainMenu", "MAIN MENU", new Vector2(160f, -260f), new Vector2(280f, 50f), new Color(0.20f, 0.25f, 0.35f));

            // Wire fields onto MatchResultUI
            var resType = typeof(MatchResultUI);
            resType.GetField("resultPanelRoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, resultGo);
            resType.GetField("rootCanvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, resultCg);
            resType.GetField("outcomeBannerText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, bannerText);
            resType.GetField("matchIdText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, matchIdText);
            resType.GetField("statusSubtitleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, subText);

            resType.GetField("playerNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, p1NameText);
            resType.GetField("playerOldRatingText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, p1OldRatingText);
            resType.GetField("playerRatingDeltaText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, p1DeltaText);
            resType.GetField("playerNewRatingText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, p1NewRatingText);
            resType.GetField("playerCareerStatsText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, p1CareerText);

            resType.GetField("opponentNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, p2NameText);
            resType.GetField("opponentOldRatingText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, p2OldRatingText);
            resType.GetField("opponentRatingDeltaText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, p2DeltaText);
            resType.GetField("opponentNewRatingText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, p2NewRatingText);

            resType.GetField("playAgainButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, playAgainBtn);
            resType.GetField("mainMenuButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(resultComp, mainMenuBtn);

            // Initially hide MatchResultPanel
            resultGo.SetActive(false);

            EditorUtility.SetDirty(resultComp);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            Debug.Log("[EloUIBuilder] Successfully built and wired ELO Rating UI into PvP_Matchmaking scene!");
        }

        private static TextMeshProUGUI CreateCardText(GameObject parent, string name, string text, float fontSize, Vector2 anchoredY, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredY;
            rect.sizeDelta = new Vector2(-20f, 40f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            return tmp;
        }

        private static Button CreateButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size, Color btnColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = btnColor;
            var btn = go.GetComponent<Button>();

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 15f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }
    }
}
#endif
