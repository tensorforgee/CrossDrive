using System;
using System.Collections.Generic;
using CrossDrive.Gameplay;
using UnityEngine;

namespace CrossDrive.Networking
{
    public sealed class NetworkInputSource
    {
        private uint sequence;

        public NetworkInputCommand Capture(bool touchLeft, bool touchRight, bool touchBoost)
        {
            float steer = 0f;
            if (touchLeft || UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)) steer += 1f;
            if (touchRight || UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) steer -= 1f;
            bool boost = touchBoost || UnityEngine.Input.GetKeyDown(KeyCode.W) ||
                         UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || UnityEngine.Input.GetKeyDown(KeyCode.Space);
            return new NetworkInputCommand(steer, boost, ++sequence);
        }
    }

    public sealed class NetworkInputBuffer
    {
        private readonly Dictionary<string, NetworkInputCommand> latestByPlayer = new Dictionary<string, NetworkInputCommand>();

        public bool TrySubmit(string sessionPlayerId, NetworkInputCommand command)
        {
            if (string.IsNullOrWhiteSpace(sessionPlayerId)) return false;
            if (latestByPlayer.TryGetValue(sessionPlayerId, out NetworkInputCommand previous) && command.Sequence <= previous.Sequence) return false;
            latestByPlayer[sessionPlayerId] = command;
            return true;
        }

        public bool TryGetLatest(string sessionPlayerId, out NetworkInputCommand command)
        {
            return latestByPlayer.TryGetValue(sessionPlayerId, out command);
        }

        public void Remove(string sessionPlayerId) => latestByPlayer.Remove(sessionPlayerId);
        public void Clear() => latestByPlayer.Clear();
    }

    public sealed class NetworkCarSimulationAdapter
    {
        public bool ApplyAuthoritativeInput(bool hasStateAuthority, string sessionPlayerId, NetworkInputCommand input,
            NetworkRoundCoordinator round, IReadOnlyList<CarController> cars)
        {
            if (!hasStateAuthority) return false;
            if (round == null) throw new ArgumentNullException(nameof(round));
            if (cars == null) throw new ArgumentNullException(nameof(cars));
            if (!round.IsDrivingEnabled) return false;
            int controlledCarId = round.ResolveControlledCar(sessionPlayerId);
            if (controlledCarId < 0 || controlledCarId >= cars.Count || cars[controlledCarId].IsRespawning) return false;
            cars[controlledCarId].Movement.SubmitInput(input.Steer, input.BoostPressed);
            return true;
        }
    }
}
