# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

MARS GeoRasterBlueprint — a minimal example [MARS](https://mars.haw-hamburg.de) agent-based simulation model demonstrating georeferenced raster and vector data. It simulates `Elephant` agents moving randomly within the perimeter of the Addo Elephant National Park (South Africa), seeking out water sources when their energy runs low. This is a *blueprint/template* repo intended to be studied and copied, not a production application — keep additions minimal and illustrative rather than feature-complete.

## Commands

This is a .NET 10.0 console project (`GeoRasterBlueprint/GeoRasterBlueprint.csproj`), built on the `Mars.Life.Simulations` NuGet package (v6.*). There are no tests in this repo.

Run all commands from the `GeoRasterBlueprint/` directory (config.json and Resources are referenced with relative paths):

```bash
cd GeoRasterBlueprint
dotnet restore
dotnet build
dotnet run -sm config.json   # -sm = start model with the given simulation config
# or simply:
./run.sh
```

Simulation output (a `Elephant_trips.geojson` file with agent trajectories) is written to `GeoRasterBlueprint/bin/Debug/net10.0/`. It can be visualized by loading it into [kepler.gl](https://kepler.gl).

## Architecture

The model follows the standard MARS layer/agent pattern, wired together in `Program.cs`:

1. A `ModelDescription` registers all layers (`LandscapeLayer`, `Perimeter`, `WaterLayer`) and the `Elephant` agent type (bound to `LandscapeLayer`).
2. `config.json` supplies the `SimulationConfig` — sim time range/step size, which data files back each layer, agent count/init file, and requested outputs (here, `trips`, i.e. GeoJSON trajectories).
3. `SimulationStarter.Start(description, config).Run()` executes the tick loop.

Layers (`GeoRasterBlueprint/Model/`):
- **`Perimeter`** (`RasterLayer`): loads `Resources/addo.asc`, an ASCII grid raster. `IsPointInside()` is the key method — it checks a coordinate is within the raster extent *and* its cell value isn't the "outside the park" NoData sentinel (`1`). This is how agents are kept within the park boundary.
- **`WaterLayer`** (`VectorLayer`): loads `Resources/WaterSpots.geojson`, exposing vector features (water holes) that agents can spatially query via `Explore(position, radius)`.
- **`LandscapeLayer`** (`AbstractLayer`): the layer agents actually live on. On init, it computes the simulation's bounding envelope from the `Perimeter`'s extent, builds a `GeoHashEnvironment<Elephant>` over it (grid resolution 1000), and spawns the `Elephant` agents into it via the `IAgentManager`.

Agent (`GeoRasterBlueprint/Model/Elephant.cs`):
- Implements `IAgent<LandscapeLayer>` and `IPositionable`. `Position`/`Latitude`/`Longitude`/`Energy` are `[PropertyDescription]`-annotated so they're bound from `Resources/elephants.csv` at spawn time (per the `mapping`/`file` entries in `config.json`).
- `Init()` validates the agent's starting position is inside the `Perimeter`, throwing otherwise.
- `Tick()`: decrements `Energy` each step; computes a candidate `Target` from the current bearing and a fixed move `Distance`. If `Energy < 40`, it queries `WaterLayer.Explore(...)` for nearby water and steers toward the nearest source (refilling `Energy` and changing bearing once close); otherwise it periodically randomizes its bearing to wander. Before moving, it re-validates the target against `Perimeter.IsPointInside()` — if the move would leave the park, it turns instead of moving.

When extending this blueprint (e.g. adding a new agent type or layer), mirror this structure: a layer class for spatial/data access, `[PropertyDescription]`-bound fields for CSV/config-driven parameterization, and perimeter checks before any movement that must stay within park bounds.

## Data files (`GeoRasterBlueprint/Resources/`)

- `addo.asc` (+ `.aux.xml`) — perimeter raster (ASCII grid), consumed by `Perimeter`.
- `addo.geojson` — vector version of the park boundary (reference/authoring data; not loaded by the model directly).
- `WaterSpots.geojson` — water hole vector features, consumed by `WaterLayer`.
- `elephants.csv` — initial agent parameters (position, energy), consumed per `config.json`'s `agents[].file`.

To regenerate/author these for a different area, see the linked MARS docs in `README.md` (raster/vector layer tutorial) and use QGIS or geojson.io for vector data.
