using System.Collections.Generic;
using System.Threading.Tasks;
using Data;

namespace WebApi
{
    /// <summary>
    /// This is a simple mockup of getting responses from a REST API. This is where we can swap out the service interfaces
    /// for mocking data for testing, e.g., Dependency Injection (DI).
    /// I normally wouldn't advocate for using UnityWebRequests as they do not allow finer control for more complicated use
    /// cases, especially when the REST API is purpose-built for the application. For better performance and more granular
    /// control, I'd look at using other .NET plugins.
    /// </summary>
    public class RequestHandler
    {
        // Currently only one service exists, but this is where we would add other services as needed,
        // e.g., ILeaderboardService, IWalletService, IUserService, etc.
        IPoiService PoiService { get; }

        public RequestHandler(IPoiService poiService)
        {
            PoiService = poiService;
        }

        /// <summary>
        /// The idea here is that the REST API would similarly use a spatial grid for storing POI data. We pass in the
        /// x,y coord and request for all the surrounding POIs within this tile and the surrounding 8 other tiles.
        /// The standard schema style for this would be to use paginated responses, but if the REST API is purpose-built,
        /// we can slim down the responses based on the spatial grid and tweak it as we see fit.
        /// </summary>
        public Task<(Result result, List<PoiTileGroup> Pois)> GetPoiData(List<TileCoord> tiles)
        {
            return PoiService.GetPoiData(tiles);
        }
    }

    public class Result
    {
        public string message;
        public int statusCode;
        public bool IsError => statusCode != 200;

        public static Result Success => new Result { statusCode = 200 };
    }
}
