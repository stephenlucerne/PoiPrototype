using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data;

namespace WebApi
{
    /// <summary>
    /// This is a test class for the PoiService that spits out random POI data with a slight random delay to simulate some latency
    /// </summary>
    public class PoiServiceTest : IPoiService
    {
        const int MIN_POI_PER_TILE = 30;
        const int MAX_POI_PER_TILE = 40;
        
        static readonly Random random = new();
        
        public async Task<(Result result, List<PoiTileGroup> groups)> GetPoiData(List<TileCoord> tiles)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(random.Next(200, 2000)));
            
            List<PoiTileGroup> group = new ();
            foreach (TileCoord tile in tiles)
            {
                int count = random.Next(MIN_POI_PER_TILE, MAX_POI_PER_TILE);
                PoiTileGroup poiTileGroup = new() { pois = new(), x = tile.x, y = tile.y };
                
                while(count > 0)
                {
                    count--;
                    PoiSchema schema = new();
                    schema.type = random.Next(0, 3);
                    schema.icon = random.Next(0, 3);
                    schema.priority = random.Next(0, 3);
                    schema.id = random.Next(100, 100000);
                    schema.name = $"POI {schema.id}";
                    schema.x = random.Next(tile.x * PoiRepository.POI_TILE_SIZE, tile.x * PoiRepository.POI_TILE_SIZE + PoiRepository.POI_TILE_SIZE);
                    schema.z = random.Next(tile.y * PoiRepository.POI_TILE_SIZE, tile.y * PoiRepository.POI_TILE_SIZE + PoiRepository.POI_TILE_SIZE);
                    schema.y = 0;
                    poiTileGroup.pois.Add(schema);
                }
                group.Add(poiTileGroup);
            }
            
            return (Result.Success, group);
        }
    }
}