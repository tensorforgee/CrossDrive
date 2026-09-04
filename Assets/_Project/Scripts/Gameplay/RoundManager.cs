using System.Collections.Generic;
using CrossDrive.Core;
using CrossDrive.Input;
using CrossDrive.Scoring;
using UnityEngine;

namespace CrossDrive.Gameplay
{
    public sealed class RoundManager : MonoBehaviour
    {
        private static readonly RoundDurations Durations = new RoundDurations(
            Phase1PrototypeConfig.PreRoundSeconds,
            Phase1PrototypeConfig.AnonymousSeconds,
            Phase1PrototypeConfig.RevealSeconds,
            Phase1PrototypeConfig.KnownDriverSeconds,
            Phase1PrototypeConfig.ResultsSeconds);

        private IReadOnlyList<PlayerIdentity> players;
        private CarController[] cars;
        private LocalKeyboardInput input;
        private RespawnSystem respawnSystem;
        private GemSystem gemSystem;
        private ScoringCoordinator scoring;
        private RoundTelemetryRecorder telemetry;
        private ControlAssignmentService assignmentService;
        private RoundStateMachine stateMachine;
        private RoundPhase observedPhase;
        private int briefingPlayerIndex;
        private bool briefingIsRevealed;
        private ScoringMode nextRoundMode;

        public RoundPhase Phase => stateMachine?.Phase ?? RoundPhase.PreRoundAssignment;
        public float TimeRemaining => stateMachine?.TimeRemaining ?? 0f;
        public ControlAssignment Assignment { get; private set; }
        public int BriefingPlayerIndex => briefingPlayerIndex;
        public bool BriefingIsRevealed => briefingIsRevealed;
        public bool IsBriefingComplete => briefingPlayerIndex >= players.Count;
        public bool CanSelectScoringMode => Debug.isDebugBuild &&
                                            (Phase == RoundPhase.PreRoundAssignment || Phase == RoundPhase.RoundEnd);
        public ScoringMode NextRoundMode => nextRoundMode;
        public ScoringCoordinator Scoring => scoring;

        public void Initialize(IReadOnlyList<PlayerIdentity> playerIdentities, CarController[] carControllers,
            LocalKeyboardInput keyboardInput, RespawnSystem carRespawnSystem, GemSystem gems,
            ScoringCoordinator scoringCoordinator, RoundTelemetryRecorder telemetryRecorder)
        {
            players = playerIdentities;
            cars = carControllers;
            input = keyboardInput;
            respawnSystem = carRespawnSystem;
            gemSystem = gems;
            scoring = scoringCoordinator;
            telemetry = telemetryRecorder;
            assignmentService = new ControlAssignmentService();
            nextRoundMode = scoring.Mode;
            StartNewRound();
        }

        public PlayerIdentity GetPlayer(int index) => players[index];

        private void StartNewRound()
        {
            stateMachine = new RoundStateMachine(Durations);
            observedPhase = stateMachine.Phase;
            briefingPlayerIndex = 0;
            briefingIsRevealed = false;
            scoring.TrySelectMode(nextRoundMode, RoundPhase.PreRoundAssignment, players.Count);
            Assignment = assignmentService.Create(players.Count);

            for (int owner = 0; owner < cars.Length; owner++)
            {
                cars[owner].AssignDriver(Assignment.GetDriverForCarOwner(owner));
            }

            scoring.Reset(players.Count);
            telemetry.BeginRound(scoring.Mode, Assignment);
            scoring.NotifyRoundStarted(Assignment);
            respawnSystem.SetRoundDrivingEnabled(false);
            gemSystem.SetCollectingEnabled(false);
            respawnSystem.ResetAllCars();
            gemSystem.ResetAll();
        }

        private void Update()
        {
            HandleScoringSelection();

            if (Phase == RoundPhase.PreRoundAssignment && !IsBriefingComplete)
            {
                UpdatePrivateBriefing();
                return;
            }

            if (Phase == RoundPhase.RoundEnd && UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                StartNewRound();
                return;
            }

            stateMachine.Tick(Time.deltaTime);
            if (observedPhase != Phase) OnPhaseChanged(Phase);

            if (!stateMachine.IsComplete) scoring.NotifyTick(TimeRemaining);

            if (stateMachine.IsComplete) StartNewRound();
        }

        private void HandleScoringSelection()
        {
            if (!CanSelectScoringMode) return;
            ScoringMode? requested = null;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) requested = ScoringMode.Commission;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) requested = ScoringMode.Siphon;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) requested = ScoringMode.SplitPurse;
            if (!requested.HasValue) return;

            nextRoundMode = requested.Value;
            if (Phase == RoundPhase.RoundEnd || requested.Value == scoring.Mode) return;

            if (scoring.TrySelectMode(requested.Value, Phase, players.Count))
            {
                telemetry.UpdateScoringModel(scoring.Mode);
                scoring.NotifyRoundStarted(Assignment);
            }
        }

        private void UpdatePrivateBriefing()
        {
            if (!input.ReadBoostDown(briefingPlayerIndex)) return;
            if (!briefingIsRevealed)
            {
                briefingIsRevealed = true;
                return;
            }

            briefingIsRevealed = false;
            briefingPlayerIndex++;
            if (briefingPlayerIndex == players.Count) stateMachine.CompleteAssignments();
        }

        private void OnPhaseChanged(RoundPhase phase)
        {
            observedPhase = phase;
            bool driving = phase == RoundPhase.AnonymousFirstHalf || phase == RoundPhase.KnownDriverSecondHalf;
            respawnSystem.SetRoundDrivingEnabled(driving);
            gemSystem.SetCollectingEnabled(driving);

            if (TryGetScoringPhase(phase, out ScoringPhase scoringPhase))
            {
                telemetry.RecordPhase(scoringPhase, scoring.NotifyPhaseChanged(scoringPhase));
            }

            if (phase == RoundPhase.RoundEnd)
            {
                IReadOnlyList<ScoreDelta> deltas = scoring.NotifyRoundEnded();
                telemetry.EndRound(scoring.Strategy.GetScores(), deltas);
            }
        }

        private static bool TryGetScoringPhase(RoundPhase phase, out ScoringPhase scoringPhase)
        {
            switch (phase)
            {
                case RoundPhase.AnonymousFirstHalf:
                    scoringPhase = ScoringPhase.Anonymous;
                    return true;
                case RoundPhase.HalftimeReveal:
                    scoringPhase = ScoringPhase.Reveal;
                    return true;
                case RoundPhase.KnownDriverSecondHalf:
                    scoringPhase = ScoringPhase.Known;
                    return true;
                default:
                    scoringPhase = default;
                    return false;
            }
        }
    }
}
