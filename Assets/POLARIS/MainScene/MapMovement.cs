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
using Esri.HPFramework;
using System;
using System.Linq;
using Esri.GameEngine;
using Esri.GameEngine.Geometry;
using POLARIS.MainScene;
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
		
		private const double PinchZoomSpeed = 0.5f;
		
		private const double MaxCameraHeight = 1500.0;
		private const double MinCameraHeight = 20;
		
		private const float MinLongitude = -81.215f;
		private const float MaxLongitude = -81.185f;
		private const float MinLatitude = 28.589f;
		private const float MaxLatitude = 28.614f;
		private double3 MinWorldPosition;
		private double3 MaxWorldPosition;
		private bool _hasCalculatedMinMax;

		private double3 _lastCartesianPoint;
		private bool _firstDragStep = true;

		private Vector2 _rotStartPosition;

		private Vector3 _lastMouseScreenPosition;
		private bool _firstOnFocus = true;

		public double MaxSpeed = 2000000.0;
		public double MinSpeed = 1000.0;
		public double VelocityDrag;

		private bool _usingMomentum = false;
		private double3 _velocity = double3.zero;
		
		private HPRoot _root;

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
			if (_arcGisMapComponent == null) 
			{
				Debug.LogError(
					"An ArcGISMapComponent could not be found. Please make sure this GameObject is a child of a GameObject with an ArcGISMapComponent attached");
				enabled = false;
				return;
			}
			
			_root = _arcGisMapComponent.GetComponent<HPRoot>();
		}

		private void Update()
		{
			if (_arcGisMapComponent == null) return;

			if (!_hasCalculatedMinMax &&
			    _arcGisMapComponent.View.Map.LoadStatus == ArcGISLoadStatus.Loaded)
			{
				var minPoint = new ArcGISPoint(MinLongitude, MinLatitude, 0, new ArcGISSpatialReference(4326));
				var maxPoint = new ArcGISPoint(MaxLongitude, MaxLatitude, 0, new ArcGISSpatialReference(4326));
				MinWorldPosition = _arcGisMapComponent.View.GeographicToWorld(minPoint);
				MaxWorldPosition = _arcGisMapComponent.View.GeographicToWorld(maxPoint);
				_hasCalculatedMinMax = true;
			}
			
			// Not functional until we have a spatial reference
			if (_arcGisMapComponent.View.SpatialReference == null) return;
			
			_velocity *= (1 - VelocityDrag);
			if (_velocity.ToVector3().magnitude < 0.01)
			{
				_velocity = double3.zero;
			}
			
			if (!MenUI_Panels.userOnListView) TouchEvent();
		}
		
		private void TouchEvent()
		{
			var cartesianPosition = Position;
			var cartesianRotation = Rotation;

			var deltaTouch = GetTouchDelta();
			_usingMomentum = true;

			if (!_firstOnFocus)
			{
				// Pinch to zoom
				var pinchZoomValue = GetPinchZoomValue();
				var rotationAngle = GetRotationDegrees();

				if (Math.Abs(pinchZoomValue) > float.Epsilon || Math.Abs(rotationAngle) > float.Epsilon)
				{
					var towardsMouse = GetMouseRayCastDirection(true);
					Geometry.RayPlaneIntersection(
						cartesianPosition, towardsMouse, double3.zero, math.up(),
						out var intersection);

					var pinchOffset = towardsMouse * pinchZoomValue * PinchZoomSpeed *
					                  (cartesianPosition.y / 300);

					if (cartesianPosition.y + pinchOffset.y > MinCameraHeight
					    && cartesianPosition.y + pinchOffset.y < MaxCameraHeight)
					{
						cartesianPosition += pinchOffset;
					}
					
					var form = new GameObject().transform;
					form.SetPositionAndRotation(cartesianPosition.ToVector3(), cartesianRotation);
					form.RotateAround((cartesianPosition + towardsMouse * intersection).ToVector3(), Vector3.up, rotationAngle);

					cartesianPosition = form.position.ToDouble3();
					cartesianRotation = form.rotation;
				}
				else
				{
					// Lateral movement
					foreach (var touch in Input.touches)
					{
						_usingMomentum = false;

						if (touch.phase != TouchPhase.Moved)
						{
							_lastCartesianPoint = GetCartesianCoord(cartesianPosition);
							continue;
						}
						
						if (deltaTouch == Vector3.zero) continue;

						var worldRayDir = GetMouseRayCastDirection(false);
						var isIntersected = Geometry.RayPlaneIntersection(
							cartesianPosition, worldRayDir, double3.zero, math.up(),
							out var intersection);

						if (!isIntersected || !(intersection >= 0)) return;

						var cartesianCoord = cartesianPosition + worldRayDir * intersection;

						_velocity = _firstDragStep
							? double3.zero
							: _lastCartesianPoint - cartesianCoord;

						_lastCartesianPoint = cartesianCoord + _velocity;
						cartesianPosition += _velocity;
					}
				}
			}
			else
			{
				_firstOnFocus = false;
			}
			_firstDragStep = false;
			
			if (_usingMomentum)
			{
				cartesianPosition += _velocity;
				
				// Bounce on walls
				if (cartesianPosition.x < MinWorldPosition.x || cartesianPosition.x > MaxWorldPosition.x)
				{
					_velocity.x = -_velocity.x;
				}
				if (cartesianPosition.z < MinWorldPosition.z || cartesianPosition.z > MaxWorldPosition.z)
				{
					_velocity.z = -_velocity.z;
				}
			}

			cartesianPosition.x = Mathf.Clamp((float)cartesianPosition.x, (float)MinWorldPosition.x, (float)MaxWorldPosition.x);
			cartesianPosition.z = Mathf.Clamp((float)cartesianPosition.z, (float)MinWorldPosition.z, (float)MaxWorldPosition.z);

			Position = cartesianPosition;
			Rotation = cartesianRotation;
		}

		private double3 GetCartesianCoord(double3 cartesianPosition)
		{
			var worldRayDir = GetMouseRayCastDirection(false);
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

		private static Vector3 GetTouchPosition(bool average)
		{
			// Handle fallback behavior for no touches (e.g., use the center of the screen)
			if (Input.touchCount <= 0)
			{
				return new Vector3(Screen.width / 2, Screen.height / 2, 0);
			}
			
			if (average)
			{
				return (Input.touches[0].position + Input.touches[1].position) / 2;
			}

			// Use the position of the first touch as the touch position
			return Input.touches[0].position;
		}

		private double3 GetMouseRayCastDirection(bool average)
		{
			var forward = _hpTransform.Forward.ToDouble3();
			var right = _hpTransform.Right.ToDouble3();
			var up = _hpTransform.Up.ToDouble3();

			var mainCamera = gameObject.GetComponent<Camera>();

			var view = new double4x4
			(
				math.double4(right, 0),
				math.double4(up, 0),
				math.double4(forward, 0),
				math.double4(double3.zero, 1)
			);

			var proj = mainCamera.projectionMatrix.inverse.ToDouble4x4();

			proj.c2.w *= -1;
			proj.c3.z *= -1;

			var mousePosition = GetTouchPosition(average);
			var ndcCoord = new double3(2.0 * (mousePosition.x / Screen.width) - 1.0,
			                           2.0 * (mousePosition.y / Screen.height) - 1.0, 1);
			var viewRayDir = math.normalize(proj.HomogeneousTransformPoint(ndcCoord));
			return view.HomogeneousTransformVector(viewRayDir);
		}

		private static float GetPinchZoomValue()
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

		private float GetRotationDegrees()
		{
			if (Input.touchCount != 2) return 0f;
			
			var touchOne = Input.GetTouch(0);
			var touchTwo = Input.GetTouch(1);
 
			if (touchOne.phase == TouchPhase.Began
			    || touchTwo.phase == TouchPhase.Began)
			{
				_rotStartPosition = touchTwo.position - touchOne.position;
			}

			if (touchOne.phase != TouchPhase.Moved
			    && touchTwo.phase != TouchPhase.Moved) return 0f;
				
			var curVector = touchTwo.position - touchOne.position;
			var angle = Vector2.SignedAngle(_rotStartPosition, curVector);
			_rotStartPosition = curVector;
				
			return angle;
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
