using System.Collections.Generic;
using CrossDrive.Core;
using CrossDrive.Gameplay;
using CrossDrive.Input;
using CrossDrive.Scoring;
using UnityEngine;

namespace CrossDrive.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private IReadOnlyList<PlayerIdentity> players;
        private LocalKeyboardInput input;
        private RoundManager round;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle centeredStyle;
        private GUIStyle panelStyle;

        public void Initialize(IReadOnlyList<PlayerIdentity> identities, LocalKeyboardInput keyboard, RoundManager manager)
        {
            players = identities;
            input = keyboard;
            round = manager;
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (round.Phase == RoundPhase.PreRoundAssignment && !round.IsBriefingComplete)
            {
                DrawPrivateBriefing();
                return;
            }

            DrawGameHud();
            if (round.Phase == RoundPhase.HalftimeReveal ||
                round.Phase == RoundPhase.KnownDriverSecondHalf ||
                round.Phase == RoundPhase.RoundEnd)
            {
                DrawRevealedAssignments();
            }
        }

        private void DrawGameHud()
        {
            GUI.Box(new Rect(14f, 14f, 650f, 300f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(30f, 24f, 620f, 30f), $"{GetPhaseTitle()}  •  {round.Scoring.Strategy.DisplayName}", titleStyle);
            GUI.Label(new Rect(30f, 54f, 620f, 23f), round.Scoring.Strategy.RuleSummary, bodyStyle);

            IReadOnlyList<int> scores = round.Scoring.Strategy.GetScores();
            float y = 84f;
            for (int playerId = 0; playerId < players.Count; playerId++)
            {
                PlayerIdentity player = players[playerId];
                PlayerIdentity controlled = players[round.Assignment.GetControlledCarOwner(playerId)];
                string driver = round.Phase == RoundPhase.AnonymousFirstHalf || round.Phase == RoundPhase.PreRoundAssignment
                    ? "???"
                    : players[round.Assignment.GetDriverForCarOwner(playerId)].DisplayName;

                GUI.color = player.CarColor;
                GUI.DrawTexture(new Rect(30f, y + 3f, 16f, 16f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(54f, y, 585f, 22f),
                    $"{player.DisplayName}  SCORE {scores[playerId]}  |  MY CAR: {player.CarColorName}  |  CAR I CONTROL: {controlled.CarColorName}  |  MY DRIVER: {driver}",
                    bodyStyle);

                string feedback = round.Scoring.Strategy.GetPlayerFeedback(playerId);
                if (!string.IsNullOrEmpty(feedback))
                {
                    GUI.Label(new Rect(54f, y + 20f, 560f, 18f), feedback, smallStyle);
                }
                else
                {
                    GUI.Label(new Rect(54f, y + 20f, 560f, 18f), input.GetScheme(playerId).Summary, smallStyle);
                }
                y += 48f;
            }

            if (round.CanSelectScoringMode)
            {
                GUI.Label(new Rect(30f, 276f, 610f, 24f), GetModelSelectionText(), bodyStyle);
            }
        }

        private void DrawPrivateBriefing()
        {
            int playerId = round.BriefingPlayerIndex;
            PlayerIdentity player = players[playerId];
            Rect panel = new Rect(Screen.width * 0.5f - 290f, Screen.height * 0.5f - 175f, 580f, 350f);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 25f, panel.y + 20f, 530f, 36f),
                $"PRIVATE BRIEFING — {player.DisplayName} — {round.Scoring.Strategy.DisplayName}", centeredStyle);
            GUI.Label(new Rect(panel.x + 30f, panel.y + 58f, 520f, 40f), round.Scoring.Strategy.RuleSummary, smallStyle);

            if (!round.BriefingIsRevealed)
            {
                GUI.Label(new Rect(panel.x + 35f, panel.y + 105f, 510f, 80f),
                    "Ask the other players to look away.\nYour assignment is hidden.", centeredStyle);
                GUI.Label(new Rect(panel.x + 35f, panel.y + 210f, 510f, 42f),
                    $"Press {input.GetScheme(playerId).Boost} to reveal", centeredStyle);
            }
            else
            {
                PlayerIdentity controlled = players[round.Assignment.GetControlledCarOwner(playerId)];
                GUI.Label(new Rect(panel.x + 35f, panel.y + 102f, 510f, 100f),
                    $"MY CAR: {player.CarColorName}\nCAR I CONTROL: {controlled.CarColorName} ({controlled.DisplayName}'s)\nMY DRIVER: ???",
                    centeredStyle);
                GUI.Label(new Rect(panel.x + 35f, panel.y + 220f, 510f, 46f),
                    $"Press {input.GetScheme(playerId).Boost} again to hide and pass.", centeredStyle);
            }

            GUI.Label(new Rect(panel.x + 30f, panel.y + 305f, 520f, 24f), GetModelSelectionText(), bodyStyle);
        }

        private void DrawRevealedAssignments()
        {
            const float width = 405f;
            float x = Screen.width - width - 14f;
            GUI.Box(new Rect(x, 14f, width, 184f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 16f, 24f, width - 32f, 30f), "FULL DRIVER MAPPING", titleStyle);
            float y = 60f;
            for (int owner = 0; owner < players.Count; owner++)
            {
                int driver = round.Assignment.GetDriverForCarOwner(owner);
                GUI.Label(new Rect(x + 16f, y, width - 32f, 24f),
                    $"{players[owner].CarColorName} ({players[owner].DisplayName}) ← {players[driver].DisplayName}", bodyStyle);
                y += 27f;
            }
        }

        private string GetPhaseTitle()
        {
            if (round.Phase == RoundPhase.PreRoundAssignment) return $"STARTING IN {Mathf.CeilToInt(round.TimeRemaining)}";
            if (round.Phase == RoundPhase.AnonymousFirstHalf) return $"ANONYMOUS  {Mathf.CeilToInt(round.TimeRemaining)}s";
            if (round.Phase == RoundPhase.HalftimeReveal) return $"REVEAL  {Mathf.CeilToInt(round.TimeRemaining)}s";
            if (round.Phase == RoundPhase.KnownDriverSecondHalf) return $"KNOWN  {Mathf.CeilToInt(round.TimeRemaining)}s";
            return $"RESULTS  {Mathf.CeilToInt(round.TimeRemaining)}s";
        }

        private string GetModelSelectionText()
        {
            string next = round.Phase == RoundPhase.RoundEnd ? $"  Next: {round.NextRoundMode}" : string.Empty;
            return $"DEV SCORING: [1] COMMISSION   [2] SIPHON   [3] SPLIT PURSE{next}";
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.92f, 0.94f, 0.98f) } };
            smallStyle = new GUIStyle(bodyStyle) { fontSize = 12, normal = { textColor = new Color(0.7f, 0.78f, 0.88f) } };
            centeredStyle = new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 18, wordWrap = true };
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = MakePanelTexture();
        }

        private static Texture2D MakePanelTexture()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, new Color(0.035f, 0.045f, 0.07f, 0.92f));
            texture.Apply();
            return texture;
        }
    }
}
