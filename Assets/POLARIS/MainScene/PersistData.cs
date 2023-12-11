using System.Collections.Generic;
using Unity.Mathematics;
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

        public static List<double2> PathPoints = new();
        public static string RoutingString;

        public static string SrcName = "";
        public static string DestName = "";
        public static float TravelMinutes = 0f;
        public static float TravelMiles = 0f;

        public static int CurrentRequests = 0;
        public static int MAX_REQUESTS = 20;

        public static void ClearStops()
        {
            Routing = false;
            StopLocations.Clear();
            StopNames.Clear();
        }
    }
}
