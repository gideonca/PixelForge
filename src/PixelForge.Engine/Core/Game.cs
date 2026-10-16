using Silk.NET.Maths;
using Silk.NET.Windowing;
using Microsoft.Extensions.Logging;
using PixelForge.Engine.Diagnostics;    

namespace PixelForge.Engine.Core
{
    public abstract class Game : IDisposable
    {
        private readonly IWindow _window;
        private readonly ILogger<Game> _logger;

        protected Game(string title, int width, int height, ILogger<Game> logger)
        {
            var options = WindowOptions.Default;

            options.Title = title;
            options.Size = new Vector2D<int>(width, height);
            options.API = new GraphicsAPI(
                ContextAPI.OpenGL,
                ContextProfile.Core,
                ContextFlags.Default,
                new APIVersion(3, 3)
            );

            _window = Window.Create(options);

            _logger = logger;

            _window.Load += OnLoad;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
        }

        public void Run()
        {
            EngineLog.Starting(_logger);

            try
            {
                _window.Run();
                EngineLog.Stopped(_logger);
            } catch (Exception ex)
            {
                EngineLog.Failed(_logger, ex);
                throw;
            }
        }

        protected virtual void OnLoad() { }
        protected virtual void OnUpdate(double delta) { }
        protected virtual void OnRender(double delta) { }

        public void Dispose()
        {
            _window.Dispose();
            GC.SuppressFinalize(this);
        }
    }

}
