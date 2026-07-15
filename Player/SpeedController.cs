using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace SlowWalk.Player
{
    internal class SpeedController
    {
        private const string WalkSpeedStat = "walkspeed";
        private const string StatCode = "slowwalk";
        private const int MinimumSpeed = 5;
        private readonly ICoreClientAPI clientApi;
        private readonly int speedStep;
        private EntityPlayer? player;
        private int? speedPercent;
        private bool applyingModifier;

        public SpeedController(ICoreClientAPI clientApi, int speedStep)
        {
            this.clientApi = clientApi;
            this.speedStep = speedStep;
        }

        public void Start()
        {
            clientApi.Event.PlayerEntitySpawn += OnPlayerEntitySpawn;
        }

        public int? Adjust(int direction)
        {
            if (player is null) return null;

            float walkSpeed = player.Stats.GetBlended(WalkSpeedStat);
            float walkSpeedWithoutSlowWalk = walkSpeed - GetCurrentModifier();
            int currentSpeedPercent = (int)Math.Round(walkSpeed * 100);
            int maximumSpeedPercent = Math.Max(
                (int)Math.Round(walkSpeedWithoutSlowWalk * 100),
                MinimumSpeed
            );

            int adjustedSpeedPercent = Math.Clamp(
                currentSpeedPercent + direction * speedStep,
                MinimumSpeed,
                maximumSpeedPercent
            );

            speedPercent = direction > 0 && adjustedSpeedPercent == maximumSpeedPercent
                ? null
                : adjustedSpeedPercent;

            ApplyModifier();
            return (int)Math.Round(player.Stats.GetBlended(WalkSpeedStat) * 100);
        }

        private void OnPlayerEntitySpawn(IClientPlayer spawnedPlayer)
        {
            if (spawnedPlayer.PlayerUID != clientApi.World.Player.PlayerUID) return;

            Attach(spawnedPlayer.Entity);
        }

        private void Attach(EntityPlayer newPlayer)
        {
            if (player == newPlayer) return;

            if (player is not null)
            {
                player.WatchedAttributes.UnregisterListener(OnStatsChanged);
            }

            player = newPlayer;

            // Wearable updates can replace the client stats tree, modifier needs applying again
            player.WatchedAttributes.RegisterModifiedListener("stats", OnStatsChanged);
            ApplyModifier();
        }

        private void OnStatsChanged()
        {
            if (applyingModifier) return;

            ApplyModifier();
        }

        private void ApplyModifier()
        {
            EntityPlayer currentPlayer = player!;
            applyingModifier = true;

            float currentModifier = GetCurrentModifier();
            float walkSpeedWithoutSlowWalk = currentPlayer.Stats.GetBlended(WalkSpeedStat) - currentModifier;
            float newModifier = 0;

            if (speedPercent.HasValue)
            {
                float minimumWalkSpeed = MinimumSpeed / 100f;
                float requestedWalkSpeed = speedPercent.Value / 100f;
                float allowedWalkSpeed = Math.Min(
                    requestedWalkSpeed,
                    Math.Max(walkSpeedWithoutSlowWalk, minimumWalkSpeed)
                );
                newModifier = Math.Min(0, allowedWalkSpeed - walkSpeedWithoutSlowWalk);
            }

            if (newModifier != currentModifier)
            {
                if (newModifier == 0)
                {
                    currentPlayer.Stats.Remove(WalkSpeedStat, StatCode);
                }
                else
                {
                    currentPlayer.Stats.Set(WalkSpeedStat, StatCode, newModifier);
                }
            }

            applyingModifier = false;
        }

        private float GetCurrentModifier()
        {
            return player!.Stats[WalkSpeedStat].ValuesByKey.TryGetValue(StatCode, out EntityStat<float> stat)
                ? stat.Value
                : 0;
        }

        public void Dispose()
        {
            clientApi.Event.PlayerEntitySpawn -= OnPlayerEntitySpawn;
            if (player is null) return;

            player.WatchedAttributes.UnregisterListener(OnStatsChanged);

            if (GetCurrentModifier() != 0)
            {
                player.Stats.Remove(WalkSpeedStat, StatCode);
            }
        }
    }
}
