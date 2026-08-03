using System;
using System.Collections.Generic;

namespace WebApi
{
    [Serializable]
    public class PoiSchema
    {
        public int id;
        public string name;
        public int x;
        public int y;
        public int z;
        public int type;
        public int icon;
        public int priority;
    }

    [Serializable]
    public class PoiTileGroup
    {
        public int x;
        public int y;
        public List<PoiSchema> pois;
    }
}
