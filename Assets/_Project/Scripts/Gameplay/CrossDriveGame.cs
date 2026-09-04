using System.Collections.Generic;
using CrossDrive.Core;
using CrossDrive.Input;
using CrossDrive.Scoring;
using CrossDrive.UI;
using UnityEngine;

namespace CrossDrive.Gameplay
{
    public sealed class CrossDriveGame : MonoBehaviour
    {
        private static Sprite whiteSprite;

        private void Start()
        {
            Application.targetFrameRate = 60;

            Camera camera = CreateCamera();
            ArenaManager arena = CreateArena();
            LocalKeyboardInput keyboardInput = new LocalKeyboardInput();
            List<PlayerIdentity> players = CreatePlayers();
            CarController[] cars = CreateCars(players, keyboardInput, arena);
            ScoringCoordinator scoring = new ScoringCoordinator(ScoringMode.Commission);

            RoundTelemetryRecorder telemetry = gameObject.AddComponent<RoundTelemetryRecorder>();
            telemetry.Initialize(players);

            foreach (CarController car in cars)
            {
                car.GetComponent<CarContactTracker>().Initialize(car, scoring, telemetry);
            }

            RespawnSystem respawn = gameObject.AddComponent<RespawnSystem>();
            respawn.Initialize(arena, cars, scoring, telemetry);

            GemSystem gems = gameObject.AddComponent<GemSystem>();
            gems.Initialize(cars, scoring, telemetry, transform, GetWhiteSprite());

            RoundManager round = gameObject.AddComponent<RoundManager>();
            respawn.BindRound(round);
            gems.BindRound(round);
            round.Initialize(players, cars, keyboardInput, respawn, gems, scoring, telemetry);

            PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
            hud.Initialize(players, keyboardInput, round);

            camera.transform.SetParent(transform);
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Prototype Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.25f;
            camera.backgroundColor = new Color(0.025f, 0.03f, 0.055f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private ArenaManager CreateArena()
        {
            GameObject arenaObject = new GameObject("Test Arena");
            arenaObject.transform.SetParent(transform);
            ArenaManager arena = arenaObject.AddComponent<ArenaManager>();
            arena.Initialize();

            CreateRectangle("Arena Platform", Vector2.zero, new Vector2(16f, 10f), new Color(0.19f, 0.23f, 0.28f), -2, arenaObject.transform);
            CreateRectangle("Inner Track", Vector2.zero, new Vector2(15.4f, 9.4f), new Color(0.26f, 0.31f, 0.35f), -1, arenaObject.transform);

            foreach (Rect pit in arena.Pits)
            {
                CreateRectangle("Pit", pit.center, pit.size, new Color(0.025f, 0.03f, 0.055f), 0, arenaObject.transform);
            }

            CreateRectangle("Center Stripe", Vector2.zero, new Vector2(0.12f, 8.7f), new Color(0.42f, 0.46f, 0.49f), 0, arenaObject.transform);
            return arena;
        }

        private static List<PlayerIdentity> CreatePlayers()
        {
            return new List<PlayerIdentity>
            {
                new PlayerIdentity(0, "Player 1", "Red", new Color(0.95f, 0.2f, 0.18f)),
                new PlayerIdentity(1, "Player 2", "Cyan", new Color(0.1f, 0.8f, 0.95f)),
                new PlayerIdentity(2, "Player 3", "Yellow", new Color(1f, 0.78f, 0.08f)),
                new PlayerIdentity(3, "Player 4", "Magenta", new Color(0.88f, 0.18f, 0.78f)),
            };
        }

        private CarController[] CreateCars(IReadOnlyList<PlayerIdentity> players, LocalKeyboardInput input, ArenaManager arena)
        {
            CarController[] cars = new CarController[players.Count];
            for (int owner = 0; owner < players.Count; owner++)
            {
                GameObject carObject = CreateRectangle(
                    $"{players[owner].DisplayName} Car",
                    arena.GetSpawnPoint(owner),
                    new Vector2(0.8f, 1.25f),
                    players[owner].CarColor,
                    4,
                    transform);

                BoxCollider2D collider = carObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(0.9f, 0.95f);
                Rigidbody2D body = carObject.AddComponent<Rigidbody2D>();
                body.mass = 0.8f;

                CarMovement movement = carObject.AddComponent<CarMovement>();
                CarController controller = carObject.AddComponent<CarController>();
                controller.Initialize(owner, movement, input);
                carObject.AddComponent<CarContactTracker>();
                movement.ResetMotion(arena.GetSpawnPoint(owner), arena.GetSpawnRotation(owner));

                AddCarDetails(carObject.transform);
                cars[owner] = controller;
            }

            return cars;
        }

        private static void AddCarDetails(Transform car)
        {
            CreateRectangle("Windshield", new Vector2(0f, 0.14f), new Vector2(0.58f, 0.34f), new Color(0.75f, 0.9f, 0.96f), 6, car);
            CreateRectangle("Front Bumper", new Vector2(0f, 0.51f), new Vector2(0.72f, 0.1f), new Color(0.95f, 0.95f, 0.95f), 6, car);
            CreateRectangle("Left Wheel", new Vector2(-0.47f, -0.06f), new Vector2(0.13f, 0.35f), new Color(0.04f, 0.04f, 0.05f), 3, car);
            CreateRectangle("Right Wheel", new Vector2(0.47f, -0.06f), new Vector2(0.13f, 0.35f), new Color(0.04f, 0.04f, 0.05f), 3, car);
        }

        private static GameObject CreateRectangle(string name, Vector2 localPosition, Vector2 size, Color color, int sortingOrder, Transform parent)
        {
            GameObject rectangle = new GameObject(name);
            rectangle.transform.SetParent(parent, false);
            rectangle.transform.localPosition = localPosition;
            rectangle.transform.localScale = size;
            SpriteRenderer renderer = rectangle.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return rectangle;
        }

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null)
            {
                return whiteSprite;
            }

            Texture2D texture = new Texture2D(1, 1);
            texture.name = "Runtime White Pixel";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            whiteSprite.name = "Runtime Rectangle Sprite";
            return whiteSprite;
        }
    }
}
