using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Microsoft.Extensions.Logging;
using PixelForge.Engine.Diagnostics;

namespace PixelForge.Engine.Core
{
    /// <summary>
    /// Owns the window and OpenGL context and provides lifecycle hooks for a game implementation.
    /// </summary>
    public abstract class Game : IDisposable
    {
        private readonly IWindow _window;
        private readonly ILogger<Game> _logger;
        private GL? _gl;

        /// <summary>Gets the initialized OpenGL API, or throws if the window has not loaded.</summary>
        protected GL Gl =>
            _gl ?? throw new InvalidOperationException("The OpenGL context is not initialized.");

        /// <summary>Creates a window configured for an OpenGL 3.3 core context.</summary>
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

            // Route window events through the overridable game lifecycle.
            _window.Load += Initialize;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
            _window.FramebufferResize += OnFramebufferResize;
            _window.Closing += Shutdown;
        }

        /// <summary>Starts the window event loop and logs its completion or failure.</summary>
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
            // The OpenGL API is only available after the window creates its graphics context.
            _gl = GL.GetApi(_window);
            Gl.Enable(EnableCap.DepthTest);
            OnFramebufferResize(_window.FramebufferSize);
            OnLoad();
        }

        private void OnFramebufferResize(Vector2D<int> size)
        {
            // Ignore resize notifications that arrive before the graphics context exists.
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

        /// <summary>Called after the OpenGL context and viewport have been initialized.</summary>
        protected virtual void OnLoad() { }

        /// <summary>Called by the window update event; <paramref name="delta"/> is elapsed seconds.</summary>
        protected virtual void OnUpdate(double delta) { }

        /// <summary>Called by the window render event; <paramref name="delta"/> is elapsed seconds.</summary>
        protected virtual void OnRender(double delta) { }

        /// <summary>Called before the OpenGL context is released.</summary>
        protected virtual void OnUnload() { }

        /// <summary>Releases the window and its associated resources.</summary>
        public void Dispose()
        {
            _window.Dispose();
            GC.SuppressFinalize(this);
        }
    }

}
