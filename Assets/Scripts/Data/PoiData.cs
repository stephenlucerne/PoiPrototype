using UnityEngine;
using WebApi;

namespace Data
{
    // There is an argument to make this a struct instead, but it could likely be shared around lots of times, which may end up being less efficient in a larger project
    public class PoiData
    {
        public readonly int id;
        public readonly string name;
        public readonly Vector3 position;
        public readonly PoiType type;
        public readonly PoiIcon icon;
        public readonly PoiPriority priority;

        public PoiData(PoiSchema schema)
        {
            id = schema.id;
            name = schema.name;
            position = new Vector3(schema.x, schema.y, schema.z);
            type = (PoiType) schema.type;
            icon = (PoiIcon) schema.icon;
            priority = (PoiPriority) schema.priority;
        }
    }

    public enum PoiType
    {
        Red = 0,
        Green = 1,
        Blue = 2,
    }

    public enum PoiIcon
    {
        Circle = 0,
        Square = 1,
        Triangle = 2,
    }

    public enum PoiPriority
    {
        High = 0,
        Medium = 1,
        Low = 2,
    }
}