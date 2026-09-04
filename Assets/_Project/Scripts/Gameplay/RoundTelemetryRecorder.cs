using System;
using System.Collections.Generic;
using System.IO;
using CrossDrive.Core;
using CrossDrive.Scoring;
using UnityEngine;

namespace CrossDrive.Gameplay
{
    public sealed class RoundTelemetryRecorder : MonoBehaviour
    {
        [Serializable]
        private sealed class RoundRecord
        {
            public string timestamp;
            public string scoringModel;
            public List<PlayerMappingRecord> mappings = new List<PlayerMappingRecord>();
            public List<EventRecord> events = new List<EventRecord>();
            public List<int> finalScores = new List<int>();
        }

        [Serializable]
        private sealed class PlayerMappingRecord
        {
            public int playerId;
            public string playerName;
            public int ownedCarId;
            public int controlledCarId;
            public int ownCarDriverId;
        }

        [Serializable]
        private sealed class EventRecord
        {
            public string timestamp;
            public float roundSeconds;
            public string type;
            public string phase;
            public int gemId = -1;
            public int carId = -1;
            public int ownerId = -1;
            public int driverId = -1;
            public int carA = -1;
            public int carB = -1;
            public float impulse;
            public int lastContactCarId = -1;
            public float lastContactAgeSeconds = -1f;
            public int shoveAttributedPlayerId = -1;
            public List<ScoreDeltaRecord> scoreDeltas = new List<ScoreDeltaRecord>();
        }

        [Serializable]
        private sealed class ScoreDeltaRecord
        {
            public int playerId;
            public int amount;
        }

        private IReadOnlyList<PlayerIdentity> players;
        private RoundRecord currentRound;
        private ControlAssignment assignment;
        private float roundStartedAt;

        public string OutputDirectory => Path.Combine(Application.persistentDataPath, "CrossDriveTelemetry");
        public string LastOutputPath { get; private set; }

        public void Initialize(IReadOnlyList<PlayerIdentity> playerIdentities)
        {
            players = playerIdentities;
        }

        public void BeginRound(ScoringMode scoringMode, ControlAssignment controlAssignment)
        {
            assignment = controlAssignment;
            roundStartedAt = Time.realtimeSinceStartup;
            currentRound = new RoundRecord
            {
                timestamp = DateTime.UtcNow.ToString("O"),
                scoringModel = scoringMode.ToString(),
            };

            for (int player = 0; player < players.Count; player++)
            {
                currentRound.mappings.Add(new PlayerMappingRecord
                {
                    playerId = player,
                    playerName = players[player].DisplayName,
                    ownedCarId = player,
                    controlledCarId = assignment.GetControlledCarOwner(player),
                    ownCarDriverId = assignment.GetDriverForCarOwner(player),
                });
            }
        }

        public void UpdateScoringModel(ScoringMode scoringMode)
        {
            if (currentRound != null) currentRound.scoringModel = scoringMode.ToString();
        }

        public void RecordPhase(ScoringPhase phase, IReadOnlyList<ScoreDelta> deltas)
        {
            EventRecord value = NewEvent("phase_changed", deltas);
            value.phase = phase.ToString();
            currentRound?.events.Add(value);
        }

        public void RecordGem(GemCollectedEvent collected, IReadOnlyList<ScoreDelta> deltas)
        {
            EventRecord value = NewEvent("gem_collected", deltas);
            value.gemId = collected.GemId;
            value.carId = collected.CarId;
            value.ownerId = collected.OwnerId;
            value.driverId = collected.DriverId;
            currentRound?.events.Add(value);
        }

        public void RecordPit(CarEnteredPitEvent pit, IReadOnlyList<ScoreDelta> deltas)
        {
            EventRecord value = NewEvent("car_entered_pit", deltas);
            value.carId = pit.CarId;
            value.ownerId = pit.OwnerId;
            value.driverId = pit.DriverId;
            value.lastContactCarId = pit.LastContactCarId;
            value.lastContactAgeSeconds = pit.LastContactAgeSeconds;
            bool qualifies = pit.LastContactCarId >= 0 && pit.LastContactCarId != pit.CarId &&
                             pit.LastContactAgeSeconds >= 0f &&
                             pit.LastContactAgeSeconds <= Phase1PrototypeConfig.ShoveAttributionSeconds;
            if (qualifies) value.shoveAttributedPlayerId = assignment.GetDriverForCarOwner(pit.LastContactCarId);
            currentRound?.events.Add(value);
        }

        public void RecordBump(CarBumpEvent bump, IReadOnlyList<ScoreDelta> deltas)
        {
            EventRecord value = NewEvent("car_bump", deltas);
            value.carA = bump.CarA;
            value.carB = bump.CarB;
            value.impulse = bump.Impulse;
            currentRound?.events.Add(value);
        }

        public void RecordRespawn(CarRespawnedEvent respawned, IReadOnlyList<ScoreDelta> deltas)
        {
            EventRecord value = NewEvent("car_respawned", deltas);
            value.carId = respawned.CarId;
            currentRound?.events.Add(value);
        }

        public void EndRound(IReadOnlyList<int> scores, IReadOnlyList<ScoreDelta> deltas)
        {
            if (currentRound == null) return;
            currentRound.events.Add(NewEvent("round_ended", deltas));
            currentRound.finalScores.Clear();
            for (int player = 0; player < scores.Count; player++) currentRound.finalScores.Add(scores[player]);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Directory.CreateDirectory(OutputDirectory);
            string filename = $"round-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.json";
            LastOutputPath = Path.Combine(OutputDirectory, filename);
            File.WriteAllText(LastOutputPath, JsonUtility.ToJson(currentRound, true));
            Debug.Log($"CrossDrive telemetry written to {LastOutputPath}");
#endif
            currentRound = null;
        }

        private EventRecord NewEvent(string type, IReadOnlyList<ScoreDelta> deltas)
        {
            EventRecord value = new EventRecord
            {
                timestamp = DateTime.UtcNow.ToString("O"),
                roundSeconds = Time.realtimeSinceStartup - roundStartedAt,
                type = type,
            };
            if (deltas != null)
            {
                for (int index = 0; index < deltas.Count; index++)
                {
                    value.scoreDeltas.Add(new ScoreDeltaRecord
                    {
                        playerId = deltas[index].PlayerId,
                        amount = deltas[index].Amount,
                    });
                }
            }
            return value;
        }
    }
}
