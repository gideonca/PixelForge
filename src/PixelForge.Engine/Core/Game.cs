using Silk.NET.Input;
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
        private IInputContext? _input;
        private System.Numerics.Vector2? _lastMousePosition;

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
            // Forward key presses from every connected keyboard to the game.
            _input = _window.CreateInput();
            foreach (var keyboard in _input.Keyboards)
                keyboard.KeyDown += (_, key, _) => OnKeyDown(key);
            foreach (var mouse in _input.Mice)
                mouse.MouseMove += (_, position) => HandleMouseMove(position);
            Gl.Enable(EnableCap.DepthTest);
            OnFramebufferResize(_window.FramebufferSize);
            OnLoad();
        }

        private void HandleMouseMove(System.Numerics.Vector2 position)
        {
            // Report movement relative to the previous position; the first event has no reference.
            if (_lastMousePosition is { } last)
                OnMouseMove(position - last);
            _lastMousePosition = position;
        }

        /// <summary>Returns whether the key is currently held down on any keyboard.</summary>
        protected bool IsKeyDown(Key key) =>
            _input?.Keyboards.Any(keyboard => keyboard.IsKeyPressed(key)) ?? false;

        /// <summary>Returns whether the mouse button is currently held down on any mouse.</summary>
        protected bool IsMouseButtonDown(MouseButton button) =>
            _input?.Mice.Any(mouse => mouse.IsButtonPressed(button)) ?? false;

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
                _input?.Dispose();
                _input = null;
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

        /// <summary>Called when a key is pressed on any keyboard.</summary>
        protected virtual void OnKeyDown(Key key) { }

        /// <summary>Called when the mouse moves; <paramref name="delta"/> is the movement in pixels.</summary>
        protected virtual void OnMouseMove(System.Numerics.Vector2 delta) { }

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
