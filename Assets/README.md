# POI Prototype

## Summary
You can control the player camera with WASD / Space / Shift. You can also filter POIs by type and priority by toggling the checkboxes on screen. As well as click the POI markers to see a panel with all of its details.

My main approach to building a world-space POI system was creating a spatial grid for storing and loading POI data.
Essentially, it only loads and tracks POIs that are within one tile of the player. I implemented a mock REST API that generates random data as you move around the world, simulating infinite amounts of POIs.
The instructions did state to supply a data set of POIs, either loading them from JSON or ScriptableObjects. Instead of loading the runtime data directly from the sample JSON, the prototype uses a mock REST API to generate POIs dynamically. I included `SampleJson.json` as an example of the payload shape that a real `PoiService` would consume.

I also want to point out that the `PoiIconProvider` is intentionally over-engineered. Realistically a simple `Sprite[] icons` field would be sufficient in the `PoiViewHandler` because it is a dependency and will be managed when Addressables loads the prefab.
However, seeing as performance seems to be the major focus of the prototype, I decided to make the `PoiIconProvider` to provide an example use case as the functionality would very likely become required in other areas of a large project, say with hundreds of icons instead of just three.
A similar thing could be said for the `PoiMarkerPool` being a dependency of the `PoiViewHandler` but I wanted to demonstrate a more general pattern for managing Addressable asset handles.

## Architecture Overview
Quick overview of the main classes:
- `PoiDemo` Starts the prototype and inits dependencies together.
- `PoiRepository` manages POI data caching and data retrieval.
- `RequestHandler` REST API for obtaining POI data from a server.
- `PoiViewHandler` manages marker visibility, filtering, LOD, and UI state.
- `PoiMarkerPool` manages pooled UI marker instances.
- `PoiIconProvider` manages Addressable icon handles.

## Performance Considerations
This is a quick rundown of things I was considering for performance:
- Spatial grid for partitioning POI data (Mentioned above). The tile size can be adjusted from `PoiRepository.TILE_SIZE`. You can also adjust the `PoiServiceTest.MIN/MAX_POI_PER_TILE` fields to control the amount of POIs generated per tile.
- Only the surrounding 3x3 tile area is requested at a time and cached for 5 minutes.
- Marker tracking is capped by `PoiViewHandler.MAX_POI_OBJECTS_TO_TRACK_SIMULTANEOUSLY` in case there are far too many markers in the immediate vicinity.
- Visible markers are capped by `PoiViewHandler.MAX_VISIBLE_POI_SCREEN_OBJECTS`.
- Markers are pooled and recycled by `PoiMarkerPool`.
- Every texture and prefab is loaded via Addressables.
- Distance checks use squared magnitude rather than Vector.Distance().
- Frustum checks happen after other cheaper distance/filter checks.
- Icons and prefabs are loaded and unloaded through Addressables. 
- Runtime collections (E.g., Lists, HashSets, Dictionaries) are reused where practical to reduce GC allocations.

## Trade-offs

- I intentionally made the mock REST API run at a very slow rate to simulate realistic latency.
- Seeing as POIs are purely informational, I leaned heavily on asynchronous loading to ensure everything is ready before displaying a marker.
- I didn't implement any Overlap / Decluttering for the UI. The visuals seemed to be the lowest priority for the prototype, and I felt like I'd spend too much time on it.
- The player controller is intentionally basic and I decided not to try and write a proper design/composition for it, as it also seemed out of scope for the prototype.
- For the sake of keeping the prototype simple, I didn't implement child-namespaces or asmdefs, e.g., `PoiPrototype.WebApi.Schemas` etc. However, for a much larger project I would likely want some more organisational structure.



