using SlowWalk.Configuration;
using SlowWalk.Input;
using SlowWalk.Player;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace SlowWalk.Systems
{
    public class SlowWalkSystem : ModSystem
    {
        private const string ConfigFileName = "slowWalkConfig.json";

        private InputHandler inputHandler = null!;
        private SpeedController speedController = null!;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Settings settings = api.LoadModConfig<Settings>(ConfigFileName);

            if (settings is null)
            {
                settings = new Settings();
                api.StoreModConfig(settings, ConfigFileName);
            }
            else if (settings.SpeedStepPercent < 1)
            {
                settings.SpeedStepPercent = 1;
                api.StoreModConfig(settings, ConfigFileName);
            }

            speedController = new SpeedController(api, settings.SpeedStepPercent);
            speedController.Start();

            inputHandler = new InputHandler(api, speedController);
            inputHandler.Start();
        }

        public override void Dispose()
        {
            inputHandler.Dispose();
            speedController.Dispose();
        }
    }
}
