using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Data;

namespace WebApi
{
    /// <summary>
    /// We aren't using this, but it's here to simply serve as a basic example of getting our tile data from the REST API.
    /// In a real production I would not use JsonUtility (and possibly not UnityWebRequests depending on requirements),
    /// but for the sake of the prototype these are accessible in every unity project.
    /// </summary>
    public class PoiService : IPoiService
    {
        const string ENDPOINT = "https://api.test.com/v1/pois";

        /// <summary>
        /// Refer to the Assets/SampleJson.json file for a sample of what a payload might look like from the REST API
        /// </summary>
        [Serializable]
        class PoiResponseData
        {
            public List<PoiTileGroup> groups;
        }
        
        public async Task<(Result result, List<PoiTileGroup> groups)> GetPoiData(List<TileCoord> tiles)
        {
            string url = ENDPOINT + "?";
            for (int i = 0; i < tiles.Count; i++)
            {
                url += $"tile={tiles[i].x},{tiles[i].y}";
                if (i < tiles.Count - 1) url += "&";
            }

            using var request = UnityWebRequest.Get(url);

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (new Result { statusCode = (int)request.responseCode, message = request.error }, null);
            }

            try
            {
                var responseData = JsonUtility.FromJson<PoiResponseData>(request.downloadHandler.text);
                return (Result.Success, responseData.groups);
            }
            catch (Exception ex)
            {
                return (new Result { statusCode = 500, message = $"JSON Deserialization Error: {ex.Message}" }, null);
            }
        }
    }
}