using UnityEngine;

namespace CodeBase
{
    public static class BraydenVisualizerConstants
    {
        public static class CompressionVisualizerConstants
        {
            public const int FREQUENCY_TYPES_COUNT = 3;
            
            public const float DEPTH_GRAPH_MINIMUM = 0f;
            public const float DEPTH_GRAPH_MAXIMUM = 7f;
        }

        public static readonly Color UnderNormaColor = new(246 / 255f, 226 / 255f, 42 / 255f);
        public static readonly Color NormaColor = new(30 / 255f, 215f / 255f, 126 / 255f);
        public static readonly Color OverNormaColor = new(249f / 255f, 84f / 255f, 74 / 255f);
        
        public static int GraphSizeX;
        public static int GraphSizeY;
    }
}