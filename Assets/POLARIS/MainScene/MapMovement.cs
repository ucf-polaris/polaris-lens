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
using Esri.ArcGISMapsSDK.Utils.Math;
using Esri.GameEngine.Geometry;
using Esri.GameEngine.View;
using Esri.HPFramework;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Esri.ArcGISMapsSDK.Samples.Components
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(HPTransform))]
	[AddComponentMenu("ArcGIS Maps SDK/Samples/ArcGIS Camera Controller")]
	public class MapMovement : MonoBehaviour
	{
		private ArcGISMapComponent _arcGisMapComponent;
		private HPTransform _hpTransform;

		private float _translationSpeed = 0.0f;
		private const float RotationSpeed = 100.0f;
		private const double PinchZoomSpeed = 0.1f;
		
		private const double MaxCameraHeight = 2000.0;
		private const double MinCameraHeight = 1.8;
		private const double MaxCameraLatitude = 85.0;

		private double3 _lastCartesianPoint;
		private double _lastDotVc = 0.0f;
		private bool _firstDragStep = true;
		private bool _lastDragStep = false;

		private Vector3 _lastMouseScreenPosition;
		private bool _firstOnFocus = true;

		public double MaxSpeed = 2000000.0;
		public double MinSpeed = 1000.0;

		private void Awake()
		{
			Application.focusChanged += FocusChanged;
		}
		
		private void OnTransformParentChanged()
		{
			OnEnable();
		}

		private void OnEnable()
		{
			_arcGisMapComponent = gameObject.GetComponentInParent<ArcGISMapComponent>();
			_hpTransform = GetComponent<HPTransform>();
		}
		
		private void Start()
		{
			if (_arcGisMapComponent != null) return;

			Debug.LogError("An ArcGISMapComponent could not be found. Please make sure this GameObject is a child of a GameObject with an ArcGISMapComponent attached");
			enabled = false;
		}

		private void Update()
		{
			if (_arcGisMapComponent == null) return;

			// Not functional until we have a spatial reference
			if (_arcGisMapComponent.View.SpatialReference == null) return;

			// Side-to-side
			DragTouchEvent();
			
			// Altitude
			UpdateNavigation();
		}
		
		private void DragTouchEvent()
		{
			var cartesianPosition = Position;
			// var cartesianRotation = Rotation;
			
			var deltaTouch =  GetTouchDelta();

			if (!_firstOnFocus)
			{
				foreach (var touch in Input.touches)
				{
					if (touch.phase != TouchPhase.Moved)
					{
						_lastCartesianPoint = GetCartesianCoord(cartesianPosition);
						continue;
					};
					if (deltaTouch == Vector3.zero) continue;
					
					var worldRayDir = GetMouseRayCastDirection();
					var isIntersected = Geometry.RayPlaneIntersection(cartesianPosition, worldRayDir, double3.zero, math.up(), out var intersection);
					
					if (!isIntersected || !(intersection >= 0)) return;
			
					var cartesianCoord = cartesianPosition + worldRayDir * intersection;

					var delta = _firstDragStep ? double3.zero : _lastCartesianPoint - cartesianCoord;

					_lastCartesianPoint = cartesianCoord + delta;
					cartesianPosition += delta;
					_firstDragStep = false;
				}
			}
			else
			{
				_firstOnFocus = false;
			}

			Position = cartesianPosition;
			// Rotation = cartesianRotation;
		}

		private double3 GetCartesianCoord(double3 cartesianPosition)
		{
			var worldRayDir = GetMouseRayCastDirection();
			var isIntersected = Geometry.RayPlaneIntersection(cartesianPosition, worldRayDir, double3.zero, math.up(), out var intersection);
			if (!isIntersected || !(intersection >= 0)) return double3.zero;
			return cartesianPosition + worldRayDir * intersection;
		}
		
		private static Vector3 GetTouchDelta()
		{
			if (Input.touchCount > 0)
			{
				// Use the position of the first touch as the touch position
				return Input.touches[0].deltaPosition;
			}
			else
			{
				// Handle fallback behavior for no delta
				return new Vector3(0, 0, 0);
			}
		}

		private static Vector3 GetTouchPosition()
		{
			if (Input.touchCount > 0)
			{
				// Use the position of the first touch as the touch position
				return Input.touches[0].position;
			}
			else
			{
				// Handle fallback behavior for no touches (e.g., use the center of the screen)
				return new Vector3(Screen.width / 2, Screen.height / 2, 0);
			}
		}
		
		private double3 GetMouseRayCastDirection()
		{
			var forward = _hpTransform.Forward.ToDouble3();
			var right = _hpTransform.Right.ToDouble3();
			var up = _hpTransform.Up.ToDouble3();
		
			var camera = gameObject.GetComponent<Camera>();
		
			var view = new double4x4
			(
				math.double4(right, 0),
				math.double4(up, 0),
				math.double4(forward, 0),
				math.double4(double3.zero, 1)
			);
		
			var proj = camera.projectionMatrix.inverse.ToDouble4x4();
		
			proj.c2.w *= -1;
			proj.c3.z *= -1;
		
			var mousePosition = GetTouchPosition();
			var ndcCoord = new double3(2.0 * (mousePosition.x / Screen.width) - 1.0, 2.0 * (mousePosition.y / Screen.height) - 1.0, 1);
			var viewRayDir = math.normalize(proj.HomogeneousTransformPoint(ndcCoord));
			return view.HomogeneousTransformVector(viewRayDir);
		}

		// private double3 GetTotalTranslation()
		// {
		// 	var forward = _hpTransform.Forward.ToDouble3();
		// 	var right = _hpTransform.Right.ToDouble3();
		// 	var up = _hpTransform.Up.ToDouble3();
		//
		// 	var totalTranslation = double3.zero;
		// 	
		// 	foreach (var touch in Input.touches)
		// 	{
		// 		if (touch.phase != TouchPhase.Moved) continue;
		// 		
		// 		var touchDelta = touch.deltaPosition;
		//
		// 		// Adjust translation speed based on touch sensitivity
		// 		totalTranslation += (right * touchDelta.x + up * touchDelta.y) * _translationSpeed * Time.deltaTime;
		// 	}
		//
		// 	return totalTranslation;
		// }
		
		private float GetPinchZoomValue()
		{
			// Handle mobile pinch zoom
			if (Input.touchCount != 2) return 0.0f;
			
			var touch1 = Input.touches[0];
			var touch2 = Input.touches[1];

			var touch1PrevPos = touch1.position - touch1.deltaPosition;
			var touch2PrevPos = touch2.position - touch2.deltaPosition;

			var prevTouchDeltaMag = (touch1PrevPos - touch2PrevPos).magnitude;
			var touchDeltaMag = (touch1.position - touch2.position).magnitude;

			return (touchDeltaMag - prevTouchDeltaMag) * (float)PinchZoomSpeed;
		}


		/// <summary>
		/// Move the camera based on user input
		/// </summary>
		private void UpdateNavigation()
		{
			var altitude = _arcGisMapComponent.View.AltitudeAtCartesianPosition(Position);
			UpdateSpeed(altitude);

			// var totalTranslation = GetTotalTranslation();
			//
			// var pinchZoomValue = GetPinchZoomValue();
			// var pinchZoomValue = 0f;
			// if (Math.Abs(pinchZoomValue) > float.Epsilon)
			{
				// var towardsMouse = GetMouseRayCastDirection();
				// totalTranslation += towardsMouse * pinchZoomValue;

				// 	if (altitude + totalTranslation.y < MinCameraHeight
				// 			|| altitude + totalTranslation.y > MaxCameraHeight)
				// 		totalTranslation.y = 0;
				//
				// 	// print("ZOOM DELTA: " + delta);
				// }
				//
				// if (!totalTranslation.Equals(double3.zero))
				// {
				// 	MoveCamera(totalTranslation);
				// }
			}
		}


		/// <summary>
		/// Move the camera
		/// </summary>
		// private void MoveCamera(double3 movDir)
		// {
		// 	var distance = math.length(movDir);
		// 	movDir /= distance;
		//
		// 	var cameraPosition = Position;
		// 	var cameraRotation = Rotation;
		//
		// 	if (_arcGisMapComponent.MapType == GameEngine.Map.ArcGISMapType.Global)
		// 	{
		// 		var spheroidData = _arcGisMapComponent.View.SpatialReference.SpheroidData;
		// 		var nextArcGISPoint = _arcGisMapComponent.View.WorldToGeographic(movDir + cameraPosition);
		//
		// 		if (nextArcGISPoint.Z > MaxCameraHeight)
		// 		{
		// 			var point = new ArcGISPoint(nextArcGISPoint.X, nextArcGISPoint.Y, MaxCameraHeight, nextArcGISPoint.SpatialReference);
		// 			cameraPosition = _arcGisMapComponent.View.GeographicToWorld(point);
		// 		}
		// 		else if (nextArcGISPoint.Z < MinCameraHeight)
		// 		{
		// 			var point = new ArcGISPoint(nextArcGISPoint.X, nextArcGISPoint.Y, MinCameraHeight, nextArcGISPoint.SpatialReference);
		// 			cameraPosition = _arcGisMapComponent.View.GeographicToWorld(point);
		// 		}
		// 		else
		// 		{
		// 			cameraPosition += movDir * distance;
		// 		}
		//
		// 		var newENUReference = _arcGisMapComponent.View.GetENUReference(cameraPosition);
		// 		var oldENUReference = _arcGisMapComponent.View.GetENUReference(Position);
		//
		// 		cameraRotation = math.mul(math.inverse(oldENUReference.GetRotation()), cameraRotation);
		// 		cameraRotation = math.mul(newENUReference.GetRotation(), cameraRotation);
		// 	}
		// 	else
		// 	{
		// 		cameraPosition += movDir * distance;
		// 	}
		//
		// 	Position = cameraPosition;
		// 	Rotation = cameraRotation;
		// }

		// private void RotateAround(ref double3 cartesianPosition, ref quaternion cartesianRotation, Vector3 deltaMouse)
		// {
		// 	var ENUReference = _arcGisMapComponent.View.GetENUReference(cartesianPosition).ToMatrix4x4();
		//
		// 	Vector2 angles;
		//
		// 	angles.x = deltaMouse.x / (float)Screen.width * RotationSpeed;
		// 	angles.y = deltaMouse.y / (float)Screen.height * RotationSpeed;
		//
		// 	angles.y = Mathf.Min(Mathf.Max(angles.y, -90.0f), 90.0f);
		//
		// 	var right = Matrix4x4.Rotate(cartesianRotation).GetColumn(0);
		//
		// 	var rotationY = Quaternion.AngleAxis(angles.x, ENUReference.GetColumn(1));
		// 	var rotationX = Quaternion.AngleAxis(-angles.y, right);
		//
		// 	cartesianRotation = rotationY * rotationX * cartesianRotation;
		// }

		private void UpdateSpeed(double height)
		{
			var msMaxSpeed = (MaxSpeed * 1000) / 3600;
			var msMinSpeed = (MinSpeed * 1000) / 3600;
			_translationSpeed = (float)(Math.Pow(Math.Min((height / 100000.0), 1), 2.0) * (msMaxSpeed - msMinSpeed) + msMinSpeed);
		}
		
		private void FocusChanged(bool isFocus)
		{
			_firstOnFocus = true;
		}

		#region Properties
		/// <summary>
		/// Get/set the camera position in world coordinates
		/// </summary>
		private double3 Position
		{
			get => _hpTransform.UniversePosition;
			set => _hpTransform.UniversePosition = value;
		}

		/// <summary>
		/// Get/set the camera rotation
		/// </summary>
		private quaternion Rotation
		{
			get => _hpTransform.UniverseRotation;
			set => _hpTransform.UniverseRotation = value;
		}

		#endregion
	}
}
