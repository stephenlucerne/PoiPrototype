using Data;
    using UI;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using WebApi;

    public class PoiDemo : MonoBehaviour
    {
        PoiViewHandler poiViewHandler;
        RequestHandler requestHandler;
        PoiRepository poiRepository;
        AsyncOperationHandle<GameObject> poiViewHandlerHandle;
        
        async void  Start()
        {
            requestHandler = new RequestHandler(new PoiServiceTest());
            poiRepository = new PoiRepository(requestHandler);
            
            // NOTE: This could be unloaded and loaded on demand, e.g., when the player loads into the open world.
            // For now I am always loading it when the prototype plays in the editor main scene
            poiViewHandlerHandle = Addressables.InstantiateAsync("Assets/Prefabs/PoiViewHandler.prefab");
            await poiViewHandlerHandle.Task;
            poiViewHandler = poiViewHandlerHandle.Result.GetComponent<PoiViewHandler>();
            poiViewHandler.Initialise(poiRepository);
        }
        
        void OnDestroy()
        {
            poiViewHandler?.Shutdown();

            if (poiViewHandlerHandle.IsValid())
                Addressables.ReleaseInstance(poiViewHandlerHandle);
        }
    }
