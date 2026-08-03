using System.Collections.Generic;
using System.Threading.Tasks;
using Data;

namespace WebApi
{
    public interface IPoiService
    {
        public Task<(Result result, List<PoiTileGroup> groups)> GetPoiData(List<TileCoord> tiles);
    }
}