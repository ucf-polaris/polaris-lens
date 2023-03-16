using System;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace POLARIS
{
	public class MapMovement : MonoBehaviour
	{
		[SerializeField]
		[Range(1, 20)]
		public float PanSpeed;

		[SerializeField]
		private float ZoomSpeed;

		[SerializeField]
		public Camera ReferenceCamera;

		[SerializeField]
		private AbstractMap MapManager;

		private Vector3 _origin;
		private Vector3 _mousePosition;
		private Vector3 _mousePositionPrevious;
		private bool _shouldDrag;
		private bool _isInitialized;
		private bool _dragStartedOnUI;

		private void Awake()
		{
			if (null == ReferenceCamera)
			{
				ReferenceCamera = GetComponent<Camera>();
				if (null == ReferenceCamera) { Debug.LogErrorFormat("{0}: reference camera not set", this.GetType().Name); }
			}
			MapManager.OnInitialized += () =>
			{
				_isInitialized = true;
			};
		}

		public void LateUpdate()
		{
			if (!_isInitialized) return;
			
			if (Input.GetMouseButtonUp(0))
			{
				_dragStartedOnUI = false;
			}
			else if (Input.GetMouseButtonDown(0) && EventSystem.current.IsPointerOverGameObject())
			{
				_dragStartedOnUI = true;
			}
			
			if (_dragStartedOnUI) return;
			
			if (Input.touchSupported && Input.touchCount > 0)
			{
				HandleTouch();
			}
			else
			{
				// Mostly for dev
				HandleMouseAndKeyBoard();
			}
		}
		
		void HandleMouseAndKeyBoard()
		{
			// zoom
			ZoomMap(Input.GetAxis("Mouse ScrollWheel"));

			//pan mouse
			PanMapUsingTouch();
		}

		
		private void HandleTouch()
		{
			//pinch to zoom.
			switch (Input.touchCount)
			{
				case 1:
					PanMapUsingTouch();
					break;
				case 2:
					var zoomFactor = 0.0f;
					// Store both touches.
					Touch touchZero = Input.GetTouch(0);
					Touch touchOne = Input.GetTouch(1);

					// Find the position in the previous frame of each touch.
					Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
					Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

					// Find the magnitude of the vector (the distance) between the touches in each frame.
					float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
					float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

					// Find the difference in the distances between each frame.
					zoomFactor = 0.01f * (touchDeltaMag - prevTouchDeltaMag);
					ZoomMap(zoomFactor);
					break;
			}
		}

		private void ZoomMap(float zoomFactor)
		{
			var zoom = Mathf.Max(0.0f, Mathf.Min(MapManager.Zoom + zoomFactor * ZoomSpeed, 21.0f));
			if (Math.Abs(zoom - MapManager.Zoom) > 0.0f)
			{
				MapManager.UpdateMap(MapManager.CenterLatitudeLongitude, zoom);
			}
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void PanMapUsingTouch()
		{
			// DEV
			if (Input.GetMouseButtonUp(1))
			{
				var mousePosScreen = Input.mousePosition;
				//assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
				//http://answers.unity3d.com/answers/599100/view.html
				mousePosScreen.z = MapManager.transform.position.z;
				var pos = ReferenceCamera.ScreenToWorldPoint(mousePosScreen);

				var latlongDelta = MapManager.WorldToGeoPosition(pos);
				Debug.Log("Latitude: " + latlongDelta.x + " Longitude: " + latlongDelta.y);
			}

			if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
			{
				var mousePosScreen = Input.mousePosition;
				//assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
				//http://answers.unity3d.com/answers/599100/view.html
				mousePosScreen.z = MapManager.transform.position.z;
				_mousePosition = ReferenceCamera.ScreenToWorldPoint(mousePosScreen);
				print(_mousePosition);

				if (_shouldDrag == false)
				{
					_shouldDrag = true;
					_origin = ReferenceCamera.ScreenToWorldPoint(mousePosScreen);
				}
			}
			else
			{
				_shouldDrag = false;
			}

			if (_shouldDrag == false) return;
			
			var changeFromPreviousPosition = _mousePositionPrevious - _mousePosition;

			if (Mathf.Abs(changeFromPreviousPosition.x) > 0.0f || Mathf.Abs(changeFromPreviousPosition.y) > 0.0f)
			{
				_mousePositionPrevious = _mousePosition;
				var offset = _origin - _mousePosition;
				if (Mathf.Abs(offset.x) > 0.0f || Mathf.Abs(offset.y) > 0.0f)
				{
					if (null != MapManager)
					{
						float factor = PanSpeed * Conversions.GetTileScaleInMeters((float)0, MapManager.AbsoluteZoom) / MapManager.UnityTileSize;
						var latlongDelta = Conversions.MetersToLatLon(new Vector2d(offset.x * factor, offset.y * factor));
						var newLatLong = MapManager.CenterLatitudeLongitude + latlongDelta;
						MapManager.UpdateMap(newLatLong, MapManager.Zoom);
					}
				}
				_origin = _mousePosition;
			}
			else
			{
				if (EventSystem.current.IsPointerOverGameObject()) return;

				_mousePositionPrevious = _mousePosition;
				_origin = _mousePosition;
			}
		}
	}
}