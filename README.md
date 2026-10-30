# PixelForge

PixelForge is an early-stage voxel engine prototype written in C# on .NET 10. It
uses Silk.NET and OpenGL 3.3 to render a small chunk preview in its sandbox.
The project explores engine architecture and the tradeoffs involved in building
game systems from the ground up; it is not yet a general-purpose game engine.

## Projects

- `src/PixelForge.Engine` — the engine library, including the window and game
  lifecycle, block chunks, mesh generation, and OpenGL rendering.
- `src/PixelForge.Sandbox` — an executable that displays generated chunks and
  lets you fly around them.

## Current implementation

- `Game` creates an OpenGL 3.3 window and provides load, update, render, input,
  and unload hooks.
- A chunk stores a 16 × 16 × 16 grid of blocks. The current generator can create
  an empty chunk or a chunk filled with dirt.
- The chunk mesher emits exposed block faces and per-block outline geometry.
  The renderer uploads and draws those meshes and can show chunk boundaries.
- `FlyCamera` provides free movement and perspective projection.

The sandbox currently shows four solid chunks arranged in a grid. Its controls
are:

| Input | Action |
| --- | --- |
| W / A / S / D | Move forward, left, backward, and right |
| Q / E | Move down and up |
| Left Shift | Move faster while held |
| Hold right mouse button and move mouse | Look around |
| F6 | Toggle chunk boundary outlines |

## Getting started

Install the .NET 10 SDK and use a system with a desktop OpenGL 3.3-capable
driver. From the repository root, build the solution:

```sh
dotnet build PixelForge.slnx
```

Run the sandbox to open the chunk preview:

```sh
dotnet run --project src/PixelForge.Sandbox
```

## Scope and next steps

This is a rendering prototype rather than a complete voxel game engine. Chunk
meshing currently treats space outside each chunk as air, and the sandbox uses
solid test chunks rather than varied terrain. Terrain streaming, persistence,
an asset pipeline, a game-object/component model, and a fixed-update simulation
are not implemented yet.

## Design goals

- Keep engine subsystems understandable and independently testable.
- Make interactions between the window lifecycle, input, and rendering
  explicit.
- Favor maintainable C# APIs and measure performance before optimizing.
- Document significant design decisions and the tradeoffs behind them.

PixelForge is an educational project. Building an engine is useful for learning
about game architecture and systems programming, but an established engine may
be a better choice when a project needs mature tooling, broad platform support,
or a fast path to shipping a game.
