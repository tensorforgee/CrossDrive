using System.Collections;
using System.Collections.Generic;
using CrossDrive.Core;
using CrossDrive.Scoring;
using UnityEngine;

namespace CrossDrive.Gameplay
{
    public sealed class GemSystem : MonoBehaviour
    {
        private sealed class Gem
        {
            public int Id;
            public GameObject GameObject;
            public bool Active;
        }

        private static readonly Vector2[] SpawnNodes =
        {
            new Vector2(-6.4f, 0f), new Vector2(-5.2f, 3.6f), new Vector2(-5.2f, -3.6f),
            new Vector2(-1.1f, 3.7f), new Vector2(-1.1f, -3.7f), new Vector2(0f, 0f),
            new Vector2(1.2f, 3.7f), new Vector2(1.2f, -3.7f), new Vector2(5.2f, 3.6f),
            new Vector2(5.2f, -3.6f), new Vector2(6.4f, 0f), new Vector2(0f, 2.2f),
        };

        private readonly List<Gem> gems = new List<Gem>();
        private CarController[] cars;
        private ScoringCoordinator scoring;
        private RoundTelemetryRecorder telemetry;
        private RoundManager round;
        private bool collectingEnabled;

        public void Initialize(CarController[] carControllers, ScoringCoordinator scoringCoordinator,
            RoundTelemetryRecorder recorder, Transform visualParent, Sprite sprite)
        {
            cars = carControllers;
            scoring = scoringCoordinator;
            telemetry = recorder;

            for (int id = 0; id < Phase1PrototypeConfig.ActiveGemCount; id++)
            {
                GameObject gemObject = new GameObject($"Gem {id + 1}");
                gemObject.transform.SetParent(visualParent, false);
                gemObject.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
                gemObject.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
                SpriteRenderer renderer = gemObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = new Color(0.25f, 1f, 0.48f);
                renderer.sortingOrder = 2;
                gems.Add(new Gem { Id = id, GameObject = gemObject, Active = true });
            }
            ResetAll();
        }

        public void BindRound(RoundManager roundManager) => round = roundManager;
        public void SetCollectingEnabled(bool enabled) => collectingEnabled = enabled;

        public void ResetAll()
        {
            StopAllCoroutines();
            for (int index = 0; index < gems.Count; index++)
            {
                gems[index].Active = true;
                gems[index].GameObject.SetActive(true);
                gems[index].GameObject.transform.position = SpawnNodes[index];
            }
        }

        private void Update()
        {
            if (!collectingEnabled || round == null) return;

            foreach (Gem gem in gems)
            {
                if (!gem.Active) continue;
                foreach (CarController car in cars)
                {
                    if (car.IsRespawning) continue;
                    if (((Vector2)car.transform.position - (Vector2)gem.GameObject.transform.position).sqrMagnitude > 0.5f) continue;
                    Collect(gem, car.OwnerPlayerIndex);
                    break;
                }
            }
        }

        private void Collect(Gem gem, int carId)
        {
            gem.Active = false;
            gem.GameObject.SetActive(false);
            GemCollectedEvent value = ScoringEventFactory.CreateGemCollected(gem.Id, carId, round.Assignment);
            telemetry.RecordGem(value, scoring.NotifyGemCollected(value));
            StartCoroutine(Respawn(gem));
        }

        private IEnumerator Respawn(Gem gem)
        {
            yield return new WaitForSeconds(Phase1PrototypeConfig.GemRespawnSeconds);
            gem.GameObject.transform.position = FindAvailableNode();
            gem.Active = true;
            gem.GameObject.SetActive(true);
        }

        private Vector2 FindAvailableNode()
        {
            int start = Random.Range(0, SpawnNodes.Length);
            for (int offset = 0; offset < SpawnNodes.Length; offset++)
            {
                Vector2 candidate = SpawnNodes[(start + offset) % SpawnNodes.Length];
                bool occupied = false;
                foreach (Gem gem in gems)
                {
                    if (gem.Active && ((Vector2)gem.GameObject.transform.position - candidate).sqrMagnitude < 0.25f)
                    {
                        occupied = true;
                        break;
                    }
                }
                if (!occupied) return candidate;
            }
            return SpawnNodes[start];
        }
    }
}
