
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi;

namespace Data
{
    public class PoiTileCache
    {
        public List<PoiData> pois;
        public long timestamp;
    }
    
    /// <summary>
    /// This class is responsible for retrieving and keeping track of the POI object data from the webApi.
    /// This class could be extended depending on the requirements of the game. The cache could be permanent for each session,
    /// or it could be much longer, or possibly write the cache to disk if the player was able to play in offline mode.
    /// </summary>
    public class PoiRepository
    {
        /// <summary>
        /// This should match the REST API grid size.
        /// Out of scope for this prototype, but we could get this value from the API and use it to determine our spatial
        /// grid resolution, so that the REST API can be somewhat responsible for fidelity and performance, e.g., if tiles
        /// contained over 100 POIs, then it may wish to reduce tile sizes for smaller payloads. If the REST API reduced
        /// size down, then all clients would benefit from a performance improvement without updating the game.
        /// </summary>
        public const int POI_TILE_SIZE = 500;
        
        const int MAX_CACHE_AGE_IN_MILLISECONDS = 300000; // 5 minutes
        
        Dictionary<TileCoord, PoiTileCache> poiDataCache = new();

        RequestHandler requestHandler { get; }

        public PoiRepository(RequestHandler requestHandler)
        {
            this.requestHandler = requestHandler;
        }
        
        /// <summary>
        /// This essentially gets the 3x3 group of tiles, where the x and y provided determine the center tile.
        /// </summary>
        public async Task<List<PoiData>> GetPoiDataForSurroundingTiles(TileCoord center)
        {
            List<TileCoord> tiles = new();
            tiles.Add(center); // Add the center
            tiles.Add(new TileCoord(center.x - 1, center.y - 1));
            tiles.Add(new TileCoord(center.x - 1, center.y));
            tiles.Add(new TileCoord(center.x - 1, center.y + 1));
            tiles.Add(new TileCoord(center.x, center.y - 1));
            tiles.Add(new TileCoord(center.x, center.y + 1));
            tiles.Add(new TileCoord(center.x + 1, center.y - 1));
            tiles.Add(new TileCoord(center.x + 1, center.y));
            tiles.Add(new TileCoord(center.x + 1, center.y + 1));
            return await GetPoiDataForTiles(tiles);
        }

        public async Task<List<PoiData>> GetPoiDataForTiles(List<TileCoord> tiles)
        {
            // Check the cache for stale data
            CheckForStaleCacheData();
            
            // Check our local cache if we already have this POI data or get the tiles we don't have cached and fetch them from our API
            List<TileCoord> missingTiles = new();
            foreach(var tile in tiles)
            {
                if (!poiDataCache.ContainsKey(tile))
                {
                    missingTiles.Add(tile);
                }
            }
            
            if (missingTiles.Count > 0)
            {
                var request = await requestHandler.GetPoiData(missingTiles);
                if (!request.result.IsError)
                {
                    // Convert the PoiSchema to PoiData and Update the cache
                    foreach (var poiGroup in request.Pois)
                    {
                        TileCoord tile = new(poiGroup.x, poiGroup.y);

                        if (poiDataCache.TryGetValue(tile, out var cacheEntry))
                        {
                            cacheEntry.pois.Clear();
                        }
                        else
                        {
                            cacheEntry = new PoiTileCache();
                            cacheEntry.pois = new List<PoiData>();
                            poiDataCache.Add(tile, cacheEntry);
                        }

                        cacheEntry.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        foreach (var poi in poiGroup.pois)
                            cacheEntry.pois.Add(new PoiData(poi));
                    }
                }
            }
                
            // Now get all relevant POI data to return in a single list
            List<PoiData> pois = new();
            foreach (var tile in tiles)
            {
                if (poiDataCache.TryGetValue(tile, out var value))
                    pois.AddRange(value.pois);
            }
                
            // TODO Note: If there is an offline-mode for this game, we may want to keep a persistent cache of POI data locally, but that feels out of scope for this prototype
            return pois;
        }

        void CheckForStaleCacheData()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            List<TileCoord> staleTiles = new();

            foreach (var kvp in poiDataCache)
            {
                long cacheAge = now - kvp.Value.timestamp;

                if (cacheAge > MAX_CACHE_AGE_IN_MILLISECONDS)
                    staleTiles.Add(kvp.Key);
            }

            foreach (var tile in staleTiles)
            {
                poiDataCache.Remove(tile);
            }
        }
    }
}
