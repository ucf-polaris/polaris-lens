// COPYRIGHT 1995-2022 ESRI
// TRADE SECRETS: ESRI PROPRIETARY AND CONFIDENTIAL
// Unpublished material - all rights reserved under the
// Copyright Laws of the United States and applicable international
// laws, treaties, and conventions.
//
// For additional information, contact:
// Attn: Contracts and Legal Department
// Environmental Systems Research Institute, Inc.
// 380 New York Street
// Redlands, California 92373
// USA
//
// email: legal@esri.com
using Esri.ArcGISMapsSDK.Components;
using Esri.GameEngine.View.State;
using UnityEngine;


namespace Esri.ArcGISMapsSDK.Samples.Components
{
	[DisallowMultipleComponent]
	[ExecuteAlways]
	[AddComponentMenu("ArcGIS Maps SDK/Samples/ArcGIS View State Logging")]
	public class ArcGISViewStateLoggingComponent : MonoBehaviour
	{
		public bool enableLogging = true;

		private ArcGISMapComponent arcGISMapComponent;

		private void OnEnable()
		{
			arcGISMapComponent = GetComponentInParent<ArcGISMapComponent>();

			if (arcGISMapComponent == null)
			{
				Debug.LogError("Unable to find a parent ArcGISMapComponent.");

				enabled = false;
				return;
			}

			SubscribeToViewStateEvents();
		}

		// Remove subscribers to events so messages are no longer logged when the component is removed
		private void OnDisable()
		{
			arcGISMapComponent.View.ViewStateChanged = null;
			arcGISMapComponent.View.ElevationSourceViewStateChanged = null;
			arcGISMapComponent.View.LayerViewStateChanged = null;
		}

		// You can subscribe to these events to show information about the view state and log warnings in the console
		// Logs usually describe events such as if the data is loading, if the data's state is changed, or if there's an error processing the data
		// You only need to subscribe to them once as long as you don't unsubscribe
		private void SubscribeToViewStateEvents()
		{
			if (arcGISMapComponent == null || arcGISMapComponent.View == null)
			{
				return;
			}

			// This event logs updates on the Elevation source data
			arcGISMapComponent.View.ElevationSourceViewStateChanged += (Esri.GameEngine.Elevation.Base.ArcGISElevationSource layer, ArcGISElevationSourceViewState arcGISElevationSourceViewState) =>
			{
				var message = arcGISElevationSourceViewState.Message?.GetMessage();
				var status = arcGISElevationSourceViewState.Status;

				var statusString = "ArcGISElevationSourceViewState " + layer.Name + " changed to : " + status.ToString();

				if ((status.HasFlag(ArcGISElevationSourceViewStatus.Error) || status.HasFlag(ArcGISElevationSourceViewStatus.Warning)) && message != null)
				{
					statusString += " (" + message + ")";

					var additionalInfo = arcGISElevationSourceViewState.Message.GetAdditionalInformation();
					string additionalMessage = "";
					additionalInfo.TryGetValue("Additional Message", out additionalMessage);

					if (additionalMessage != null && additionalMessage != "")
					{
						statusString += "\nAdditional info: " + additionalMessage;
					}
				}

				if (enableLogging)
				{
					Debug.Log(statusString);
				}
			};

			// This event logs changes to the layers' statuses
			arcGISMapComponent.View.LayerViewStateChanged += (Esri.GameEngine.Layers.Base.ArcGISLayer layer, ArcGISLayerViewState arcGISLayerViewState) =>
			{
				var message = arcGISLayerViewState.Message?.GetMessage();
				var status = arcGISLayerViewState.Status;

				var statusString = "ArcGISLayerViewState " + layer.Name + " changed to : " + status.ToString();

				if ((status.HasFlag(ArcGISLayerViewStatus.Error) || status.HasFlag(ArcGISLayerViewStatus.Warning)) && message != null)
				{
					statusString += " (" + message + ")";

					var additionalInfo = arcGISLayerViewState.Message.GetAdditionalInformation();
					string additionalMessage = "";
					additionalInfo.TryGetValue("Additional Message", out additionalMessage);

					if (additionalMessage != null && additionalMessage != "")
					{
						statusString += "\nAdditional info: " + additionalMessage;
					}
				}

				if (enableLogging)
				{
					Debug.Log(statusString);
				}
			};

			// This event logs the View's overall status
			arcGISMapComponent.View.ViewStateChanged += (ArcGISViewState arcGISViewState) =>
			{
				var message = arcGISViewState.Message?.GetMessage();
				var status = arcGISViewState.Status;

				var statusString = "ArcGISViewState changed to : " + status.ToString();

				if ((status.HasFlag(ArcGISViewStatus.Error) || status.HasFlag(ArcGISViewStatus.Warning)) && message != null)
				{
					statusString += " (" + message + ")";

					var additionalInfo = arcGISViewState.Message.GetAdditionalInformation();
					string additionalMessage = "";
					additionalInfo.TryGetValue("Additional Message", out additionalMessage);

					if (additionalMessage != null && additionalMessage != "")
					{
						statusString += "\nAdditional info: " + additionalMessage;
					}
				}

				if (enableLogging)
				{
					Debug.Log(statusString);
				}
			};

			arcGISMapComponent.View.SpatialReferenceChanged += () =>
			{
				var spatialReference = arcGISMapComponent.View.SpatialReference;

				if (spatialReference != null)
				{
					if (enableLogging)
					{
						Debug.Log("SpatialReference changed to : " + spatialReference.WKID.ToString());
					}
				}
			};
		}

	}
}
