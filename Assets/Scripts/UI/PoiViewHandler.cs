using System.Collections.Generic;
using System.Threading.Tasks;
using Data;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// This class is the main orchestrator for the POI view. It is responsible for managing the POI objects that are
    /// currently visible and also for getting nearby POI data from the webApi, translating that data into POI objects
    /// </summary>
    public class PoiViewHandler : MonoBehaviour
    {
        [Header("World Canvas for POI Objects")]
        [SerializeField] Canvas worldCanvas;
        
        [Header("Filter Panel")]
        [SerializeField] Toggle PoiFilterPriorityHigh;
        [SerializeField] Toggle PoiFilterPriorityMedium;
        [SerializeField] Toggle PoiFilterPriorityLow;
        [SerializeField] Toggle PoiFilterTypeRed;
        [SerializeField] Toggle PoiFilterTypeGreen;
        [SerializeField] Toggle PoiFilterTypeBlue;
        
        [Header("Details Panel")]
        [SerializeField] GameObject PoiDetailsPanel;
        [SerializeField] TMP_Text PoiDetailsText;
        
        /// <summary>
        /// I think it is sensible to have a total cap on POI objects on screen at any given time. This would prioritise
        /// the higher priority POI objects
        /// </summary>
        const int MAX_VISIBLE_POI_SCREEN_OBJECTS = 50;
        
        /// <summary>
        /// Objects that are not in the camera frustum still need to be checked every frame to re-enable them when they
        /// are in the camera view. This field determines the absolute maximum number of POI objects to be keeping track
        /// of around the player, whereas the above field MAX_VISIBLE_POI_SCREEN_OBJECTS is the maximum number that can
        /// be visible at the same time to avoid too much visual clutter.
        /// </summary>
        const int MAX_POI_OBJECTS_TO_TRACK_SIMULTANEOUSLY = 300;

        /// <summary>
        /// This is the max distance of the POI object. If it exceeds this distance, it will be hidden
        /// </summary>
        const float POI_LOD_DISTANCE_THESHOLD_1 = 400f;

        /// <summary>
        /// This is the max distance for showing the POI object in its higher fidelity state, eg showing the text/name as well as the icon
        /// </summary>
        const float POI_LOD_DISTANCE_THESHOLD_2 = 200f;

        PoiRepository poiRepository;
        PoiMarkerPool markerPool;
        PoiIconProvider iconProvider;

        /// <summary>
        /// These are the current POI data objects that should be within the POI_LOD_DISTANCE_THESHOLD_1 for rendering (Or close enough for checking).
        /// The UpdatePoiScreenObjects() method will determine which POI objects will get displayed.
        /// Whenever the player moves to a new grid space the CacheNearestPoiObjects() method will update this currentPoiObjects field.
        /// </summary>
        List<PoiData> currentPoiData = new();

        /// <summary>
        /// These are the objects that have been grabbed from the pool and being tracked every frame.
        /// When the currentPoiData changes, e.g., when the player moves to a new grid tile, then this field will get updated with the new objects.
        /// </summary>
        Dictionary<int, Marker> currentPoiMarkersById = new();

        // Re-usable collections
        // This only gets used when updating surrounding POI objects. This may be a little bit extreme, but re-using the hashset will reduce GC allocations
        HashSet<PoiIcon> neededIcons = new ();
        HashSet<int> newPoiIds = new();
        List<int> idsToRemove = new();
        
        struct Marker
        {
            public PoiData data;
            public PoiMarker marker;
        }

        bool initialised = false;
        
        readonly Dictionary<PoiPriority, Toggle> priorityFilters = new();
        readonly Dictionary<PoiType, Toggle> typeFilters = new();

        public async void Initialise(PoiRepository poiRepository)
        {
            this.poiRepository = poiRepository;
            markerPool ??= new PoiMarkerPool();
            await markerPool.Initialise(worldCanvas.transform);
            iconProvider ??= new PoiIconProvider();
            
            priorityFilters.Clear();
            priorityFilters.Add(PoiPriority.High, PoiFilterPriorityHigh);
            priorityFilters.Add(PoiPriority.Medium, PoiFilterPriorityMedium);
            priorityFilters.Add(PoiPriority.Low, PoiFilterPriorityLow);

            typeFilters.Clear();
            typeFilters.Add(PoiType.Red, PoiFilterTypeRed);
            typeFilters.Add(PoiType.Green, PoiFilterTypeGreen);
            typeFilters.Add(PoiType.Blue, PoiFilterTypeBlue);
            
            initialised = true;
        }

        public void Shutdown()
        {
            initialised = false;
            markerPool.Release();
            iconProvider.Release();
        }

        Vector2Int lastGridPos = new(0, 0);
        Task getNearbyPoiObjectsTask = null;
        void Update()
        {
            if (!initialised)
                return;
            
            // TODO we could add a slight cooldown here in case the player jitters back and forth between two tiles, to avoid thrashing the UI
            // To avoid race condition, if we are already getting POI objects for a grid space we don't try to get new ones
            if (getNearbyPoiObjectsTask == null || getNearbyPoiObjectsTask.IsCompleted)
            {
                Vector3 pos = PlayerController.GetPosition();
                Vector2Int currentGridPos = new Vector2Int(
                    Mathf.FloorToInt(pos.x / PoiRepository.POI_TILE_SIZE),
                    Mathf.FloorToInt(pos.z / PoiRepository.POI_TILE_SIZE)
                );

                if (currentGridPos != lastGridPos)
                {
                    lastGridPos = currentGridPos;
                    getNearbyPoiObjectsTask = GetNearbyPoiObjects();
                }
            }
            
            UpdatePoiMarkers();
        }

        async Task GetNearbyPoiObjects()
        {
            Vector3 pos = PlayerController.GetPosition();
            int gridX = Mathf.FloorToInt(pos.x / PoiRepository.POI_TILE_SIZE);
            int gridY = Mathf.FloorToInt(pos.z / PoiRepository.POI_TILE_SIZE);
            
            var newPois = await poiRepository.GetPoiDataForSurroundingTiles(new (gridX, gridY));
            
            newPoiIds.Clear();
            foreach (var poi in newPois)
                newPoiIds.Add(poi.id);
            
            // Remove markers for POIs that are no longer in the surrounding tiles
            idsToRemove.Clear();
            foreach (var kvp in currentPoiMarkersById)
            {
                if (!newPoiIds.Contains(kvp.Key))
                {
                    idsToRemove.Add(kvp.Key);
                }
            }

            foreach (var id in idsToRemove)
            {
                var marker = currentPoiMarkersById[id];
                marker.marker.Clean();
                markerPool.ReturnMarker(marker.marker);
                currentPoiMarkersById.Remove(id);
            }

            // Update currentPoiData and sort by priority
            currentPoiData = newPois;
            currentPoiData.Sort((a, b) => ((int)a.priority).CompareTo((int)b.priority));

            // Add new markers for new POIs, up to the cap
            // Re-using the hashset to lower allocations
            neededIcons.Clear();
            
            int count = 0;
            foreach (var data in currentPoiData)
            {
                if (count >= MAX_POI_OBJECTS_TO_TRACK_SIMULTANEOUSLY) break;
                
                neededIcons.Add(data.icon);
                
                if (!currentPoiMarkersById.ContainsKey(data.id))
                {
                    PoiMarker poiMarker = markerPool.GetMarker();
                    // Dont turn the marker on yet, that is handled in the UpdatePoiMarkers() method each frame
                    poiMarker.t.position = data.position;
                    poiMarker.SetOnClickBehaviour(() => ShowPoiDetails(data));
                    currentPoiMarkersById.Add(data.id, new Marker { data = data, marker = poiMarker });
                }
                count++;
            }

            // Tell icon provider which icons we need. This will manage the icons that need to be loaded
            iconProvider?.UpdateHandles(neededIcons);
        }

        bool IsVisibleByFilter(PoiData data)
        {
            if (priorityFilters.TryGetValue(data.priority, out var priorityToggle))
            {
                if (!priorityToggle.isOn) return false;
            }

            if (typeFilters.TryGetValue(data.type, out var typeToggle))
            {
                if (!typeToggle.isOn) return false;
            }

            return true;
        }

        void UpdatePoiMarkers()
        {
            Vector3 playerPos = PlayerController.GetPosition();
            int visibleCount = 0;

            foreach (var markerItem in currentPoiMarkersById.Values)
            {
                Vector3 delta = playerPos - markerItem.data.position;
                float sqrDist = delta.sqrMagnitude;
                float visibleDistance = POI_LOD_DISTANCE_THESHOLD_1 * POI_LOD_DISTANCE_THESHOLD_1;

                bool closeEnoughToBeVisible = sqrDist <= visibleDistance;
                bool filtered = IsVisibleByFilter(markerItem.data);

                if (closeEnoughToBeVisible && filtered && visibleCount < MAX_VISIBLE_POI_SCREEN_OBJECTS)
                {
                    bool inFrustum = PlayerController.Instance.IsInCameraFrustum(markerItem.data.position);
                    if (inFrustum)
                    {
                        float visibleTextDistance = POI_LOD_DISTANCE_THESHOLD_2 * POI_LOD_DISTANCE_THESHOLD_2;
                        bool closeEnoughToShowText = closeEnoughToBeVisible && sqrDist <= visibleTextDistance;

                        // Update marker position/icon/text
                        UpdateMarkerAsync(markerItem, playerPos, closeEnoughToShowText);

                        visibleCount++;
                        continue;
                    }
                }

                if (markerItem.marker.gameObject.activeSelf)
                    markerItem.marker.gameObject.SetActive(false);
            }
        }

        async void UpdateMarkerAsync(Marker markerItem, Vector3 playerPos, bool textIsVisible)
        {
            // We assume if the marker is not active the icon hasn't been set yet. Likewise, if it is active, we know the icon has been loaded
            if (!markerItem.marker.gameObject.activeSelf)
            {
                Sprite icon = await iconProvider.GetIcon(markerItem.data.icon);
                markerItem.marker.gameObject.SetActive(true);
                markerItem.marker.Setup(markerItem.data, icon);
            }
            
            markerItem.marker.SetTextActive(textIsVisible);

            // Positioning (simplified, assuming world space canvas)
            markerItem.marker.transform.position = markerItem.data.position;
            markerItem.marker.transform.LookAt(playerPos);
            markerItem.marker.transform.Rotate(0, 180, 0); // Face player
        }

        /// <summary>
        /// This method is for when the player clicks on a POI object, this is when we can use Vector3.Distance.
        /// As performance is a main goal, we don't want to show that in every frame on the individual objects.
        /// (Unless we want to offer that feature to the user. But we would be accepting the performance cost of doing Distance every frame on lots of objects)
        /// </summary>
        void ShowPoiDetails(PoiData data)
        {
            PoiDetailsText.text = $"Id: {data.id}\n" +
                                  $"Name: {data.name}\n" +
                                  $"Position: {data.position.ToString()}\n" +
                                  $"Priority: {data.priority.ToString()}\n" +
                                  $"Type: {data.type.ToString()}\n" +
                                  $"Distance: {Vector3.Distance(data.position, PlayerController.GetPosition()).ToString()}";
            PoiDetailsPanel.SetActive(true);
        }
        
        public void HidePoiDetails()
        {
            PoiDetailsPanel.SetActive(false);
        }
    }
}
