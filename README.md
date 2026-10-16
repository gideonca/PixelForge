# PixelForge

PixelForge is a voxel game engine written in C#, using OpenGL through Silk.NET.
This project is an exploration of what it takes to build an engine from the
ground up: the systems behind rendering, game loops, input, assets, and object
lifecycles, along with the architectural and performance tradeoffs involved.

The repository is at an early stage. The engine and sandbox projects currently
provide a starting point; the engine systems described here are planned work,
not implemented features.

## Projects

- `src/PixelForge.Engine` — the class library where the engine will be built.
- `src/PixelForge.Sandbox` — an executable project for experimenting with and
  demonstrating the engine.

## Getting started

Build the solution with the .NET 10 SDK:

```sh
dotnet build PixelForge.slnx
```

Run the sandbox:

```sh
dotnet run --project src/PixelForge.Sandbox
```

## Planned engine areas

- A predictable game loop and lifecycle for initializing, updating, and
  shutting down the engine.
- Rendering abstractions and a first working rendering backend.
- Input handling for keyboard, mouse, and other supported devices.
- Asset loading, organization, and lifetime management.
- Game objects and components with clear creation and destruction semantics.
- A sample experience in the sandbox that exercises the engine systems.

See [TODO.md](TODO.md) for the implementation checklist.

## Design goals

- Keep engine subsystems understandable and independently testable.
- Make the interactions between the game loop, input, and rendering explicit.
- Favor maintainable C# APIs while measuring performance before optimizing.
- Document significant design decisions and the tradeoffs behind them.

PixelForge is an educational project. Building an engine is useful for learning
about game architecture and systems programming, but an established engine may
be a better choice when a project needs mature tooling, broad platform support,
or a fast path to shipping a game.
