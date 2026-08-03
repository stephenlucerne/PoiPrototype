using System.Collections.Generic;
using System.Threading.Tasks;
using Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UI
{
    /// <summary>
    /// This class is responsible for maintaining the Addressable handles and such for POI icons.
    /// The PoiViewHandler creates an instance of this and regularly informs it which icon types are needed for
    /// the POIs in the surrounding area. E.g., When the PoiViewHandler uses GetNearbyPoiObjects() it also pokes this
    /// class to let it know which handles to release and/or register.
    /// </summary>
    public class PoiIconProvider
    {
        Dictionary<PoiIcon, AsyncOperationHandle<Sprite>> handles = new();
        List<PoiIcon> unused = new();

        public async Task<Sprite> GetIcon(PoiIcon type)
        {
            if (!handles.TryGetValue(type, out var handle))
                return null;

            if (!handle.IsDone)
                await handle.Task;

            return handle.Result;
        }

        public void UpdateHandles(HashSet<PoiIcon> types)
        {
            // Creating a re-usable list to mitigate allocations as much as possible
            unused.Clear();
            
            // Release unused handles
            foreach (var kvp in handles)
            {
                if (!types.Contains(kvp.Key))
                {
                    Addressables.Release(kvp.Value);
                    unused.Add(kvp.Key);
                }
            }

            foreach (var type in unused)
                handles.Remove(type);
            
            // Register new handles
            foreach (var type in types)
            {
                if (handles.ContainsKey(type))
                    continue;

                var handle = Addressables.LoadAssetAsync<Sprite>($"Assets/Sprites/Shapes.png[{type}]");
                handles.Add(type, handle);
            }
        }
        
        public void Release()
        {
            foreach (var handle in handles.Values)
                Addressables.Release(handle);
            
            handles.Clear();
        }
    }
}