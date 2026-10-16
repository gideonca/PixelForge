# PixelForge implementation checklist

## Foundation

- [ ] Replace the engine placeholder with a documented engine entry point.
- [ ] Define engine configuration and lifecycle states.
- [ ] Add a logging and error-reporting approach for engine subsystems.
- [ ] Establish a test project and tests for core engine behavior.

## Game loop and lifecycle

- [ ] Implement initialization, update, and shutdown phases.
- [ ] Add a frame loop with elapsed-time and delta-time tracking.
- [ ] Support a fixed-update step for time-sensitive simulation.
- [ ] Define how loop timing, frame pacing, and shutdown requests work.

## Rendering

- [ ] Choose and document the initial rendering backend and supported targets.
- [ ] Define a renderer abstraction that can be used by the game loop.
- [ ] Create and present a first rendered frame in the sandbox.
- [ ] Add a minimal drawing API and demonstrate it in the sandbox.
- [ ] Define ownership and disposal rules for rendering resources.

## Input

- [ ] Define a device-independent input API.
- [ ] Implement keyboard and mouse input for the initial target platform.
- [ ] Make input state available consistently during each update.
- [ ] Add a sandbox example that responds to user input.

## Assets

- [ ] Define asset identifiers, loading APIs, and supported asset types.
- [ ] Implement loading and caching for an initial asset type.
- [ ] Define asset ownership, unloading, and error-reporting behavior.
- [ ] Add sample assets and demonstrate loading them in the sandbox.

## Objects and components

- [ ] Design game object and component APIs.
- [ ] Define object creation, update order, and destruction semantics.
- [ ] Integrate object updates with the engine's game loop.
- [ ] Add a sandbox example using multiple objects or components.

## Quality and documentation

- [ ] Document the architecture and how the major subsystems work together.
- [ ] Record important design decisions and alternatives considered.
- [ ] Measure startup and frame performance with a repeatable benchmark.
- [ ] Review allocations and hot paths before applying optimizations.
- [ ] Document when building a custom engine is appropriate versus using an
  existing engine.
