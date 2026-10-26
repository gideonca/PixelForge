using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Microsoft.Extensions.Logging;
using PixelForge.Engine.Diagnostics;

namespace PixelForge.Engine.Core
{
    public abstract class Game : IDisposable
    {
        private readonly IWindow _window;
        private readonly ILogger<Game> _logger;
        private GL? _gl;

        protected GL Gl =>
            _gl ?? throw new InvalidOperationException("The OpenGL context is not initialized.");

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

            _window.Load += Initialize;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
            _window.FramebufferResize += OnFramebufferResize;
            _window.Closing += Shutdown;
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

        private void Initialize()
        {
            _gl = GL.GetApi(_window);
            Gl.Enable(EnableCap.DepthTest);
            OnFramebufferResize(_window.FramebufferSize);
            OnLoad();
        }

        private void OnFramebufferResize(Vector2D<int> size)
        {
            if (_gl is not null)
                Gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        }

        private void Shutdown()
        {
            try
            {
                OnUnload();
            }
            finally
            {
                _gl?.Dispose();
                _gl = null;
            }
        }

        protected virtual void OnLoad() { }
        protected virtual void OnUpdate(double delta) { }
        protected virtual void OnRender(double delta) { }
        protected virtual void OnUnload() { }

        public void Dispose()
        {
            _window.Dispose();
            GC.SuppressFinalize(this);
        }
    }

}
