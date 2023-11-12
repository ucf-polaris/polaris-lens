using System.Collections.Generic;
using UnityEngine;

namespace POLARIS.MainScene
{
    public static class PersistData
    {
        public static bool Routing = false;
        public static bool UsingCurrent = false;
        
        public static Vector3 DestPoint = Vector3.zero;
        public static readonly Stack<Vector3> StopLocations = new();
        public static readonly Stack<string> StopNames = new();

        public static List<double[]> PathPoints = new();
        // {
        //     new[]{28.614402, -81.195860},
        //     new[]{28.614469, -81.195702},
        //     new[]{28.614369, -81.195760}
        // };
        public static string RoutingString;

        public static string SrcName = "";
        public static string DestName = "";
        public static float TravelMinutes = 0f;
        public static float TravelMiles = 0f;

        public static void ClearStops()
        {
            Routing = false;
            StopLocations.Clear();
            StopNames.Clear();
        }
    }
}
