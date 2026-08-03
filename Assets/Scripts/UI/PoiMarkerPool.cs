using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UI
{
    /// <summary>
    /// Used for managing the recycling/setup of POI marker objects
    /// </summary>
    public class PoiMarkerPool
    {
        AsyncOperationHandle<GameObject> markerHandle;
        PoiMarker markerPrefab;
        Transform mainParent;
        readonly Stack<PoiMarker> pool = new();
        readonly List<PoiMarker> allMarkers = new();

        public PoiMarker GetMarker()
        {
            if (pool.Count == 0)
                PopulatePool();

            PoiMarker marker = pool.Pop();
            return marker;
        }

        void PopulatePool()
        {
            PoiMarker marker = Object.Instantiate(markerPrefab, mainParent);
            marker.gameObject.SetActive(false);
            allMarkers.Add(marker);
            pool.Push(marker);
        }

        public void ReturnMarker(PoiMarker marker)
        {
            marker.gameObject.SetActive(false);
            pool.Push(marker);
        }

        public async Task Initialise(Transform mainParent)
        {
            this.mainParent = mainParent;
            markerHandle = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/PoiMarker.prefab");
            await markerHandle.Task;
            markerPrefab = markerHandle.Result.GetComponent<PoiMarker>();
        }

        public void Release()
        {
            foreach (var marker in allMarkers)
            {
                if (marker != null)
                    Object.Destroy(marker.gameObject);
            }

            markerPrefab = null;
            allMarkers.Clear();
            pool.Clear();
            
            Addressables.Release(markerHandle);
        }
    }
}