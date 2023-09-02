using System;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions.Samples.Geospatial;
using UnityEngine;

namespace POLARIS.GeospatialScene
{
    /// <summary>
    /// A serializable struct that stores the basic information of a persistent geospatial anchor.
    /// </summary>
    [Serializable]
    public struct GeospatialAnchorContent
    {
        /// <summary>
        /// The text content of this geospatial anchor.
        /// </summary>
        public string Text;

        /// <summary>
        /// The GeospatialAnchor metadata.
        /// </summary>
        public GeospatialAnchorHistory History;

        /// <summary>
        /// Construct a Geospatial Anchor history.
        /// </summary>
        /// <param name="text">The time this Geospatial Anchor was created.</param>
        /// <param name="history">The anchor data.
        /// </param>
        public GeospatialAnchorContent(string text, GeospatialAnchorHistory history)
        {
            Text = text;
            History = history;
        }
        
        /// <summary>
        /// Overrides ToString() method.
        /// </summary>
        /// <returns>Return the json string of this object.</returns>
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    /// <summary>
    /// A wrapper class for serializing a collection of <see cref="GeospatialAnchorContent"/>.
    /// </summary>
    [Serializable]
    public class GeospatialAnchorContentCollection
    {
        /// <summary>
        /// A list of Geospatial Anchor Content Data.
        /// </summary>
        public List<GeospatialAnchorContent> Collection = new List<GeospatialAnchorContent>();
    }
}
