using System;
using SlowWalk.Player;
using Vintagestory.API.Client;

namespace SlowWalk.Input
{
    internal class InputHandler
    {
        private const string HotkeyCode = "slowwalk";

        private readonly ICoreClientAPI clientApi;
        private readonly SpeedController speedController;

        public InputHandler(ICoreClientAPI clientApi, SpeedController speedController)
        {
            this.clientApi = clientApi;
            this.speedController = speedController;
        }

        public void Start()
        {
            clientApi.Input.RegisterHotKey(HotkeyCode, "Walk speed", GlKeys.V, HotkeyType.MovementControls);
            clientApi.Event.MouseWheelMove += OnMouseWheel;
        }

        private void OnMouseWheel(MouseWheelEventArgs args)
        {
            if (!clientApi.Input.IsHotKeyPressed(HotkeyCode)) return;

            int direction = Math.Sign(args.deltaPrecise);
            if (direction == 0) return;

            args.SetHandled();

            int? speedPercent = speedController.Adjust(direction);
            if (speedPercent is null) return;

            clientApi.TriggerIngameError(this, "slowwalkspeed", $"Walk speed: {speedPercent}%");
        }

        public void Dispose()
        {
            clientApi.Event.MouseWheelMove -= OnMouseWheel;
            clientApi.Input.HotKeys.Remove(HotkeyCode);
        }
    }
}
