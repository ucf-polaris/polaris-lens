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
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Esri.ArcGISMapsSDK.Samples.Components
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(HPTransform))]
	[AddComponentMenu("ArcGIS Maps SDK/Samples/ArcGIS Camera Controller")]
	public class OldArcGISCameraControllerComponent : MonoBehaviour
	{
		private ArcGISMapComponent _arcGisMapComponent;
		private HPTransform _hpTransform;

#if ENABLE_INPUT_SYSTEM
		public ArcGISCameraControllerComponentActions CameraActions;
		private InputAction _upControls;
		private InputAction _forwardControls;
		private InputAction _rightControls;
#endif

		private float _translationSpeed = 0.0f;
		private const float RotationSpeed = 100.0f;
		private const double MouseScrollSpeed = 0.1f;

		private const double MaxCameraHeight = 2000.0;
		private const double MinCameraHeight = 1.8;
		private const double MaxCameraLatitude = 85.0;

		private double3 _lastCartesianPoint = double3.zero;
		private ArcGISPoint _lastArcGisPoint = new ArcGISPoint(0, 0, 0, ArcGISSpatialReference.WGS84());
		private double _lastDotVc = 0.0f;
		private bool _firstDragStep = true;

		private Vector3 _lastMouseScreenPosition;
		private bool _firstOnFocus = true;

		public double MaxSpeed = 2000000.0;
		public double MinSpeed = 1000.0;

		private void Awake()
		{
			_lastMouseScreenPosition = GetMousePosition();

			Application.focusChanged += FocusChanged;

#if ENABLE_INPUT_SYSTEM
			CameraActions = new ArcGISCameraControllerComponentActions();
			_upControls = CameraActions.Move.Up;
			_forwardControls = CameraActions.Move.Forward;
			_rightControls = CameraActions.Move.Right;
#endif
		}

		private void OnEnable()
		{
			_arcGisMapComponent = gameObject.GetComponentInParent<ArcGISMapComponent>();
			_hpTransform = GetComponent<HPTransform>();

#if ENABLE_INPUT_SYSTEM
			_upControls.Enable();
			_forwardControls.Enable();
			_rightControls.Enable();
#endif
		}

		private void OnDisable()
		{
#if ENABLE_INPUT_SYSTEM
			_upControls.Disable();
			_forwardControls.Disable();
			_rightControls.Disable();
#endif
		}

		private static Vector3 GetMousePosition()
		{
#if ENABLE_INPUT_SYSTEM
			return Mouse.current.position.ReadValue();
#else
			return Input.mousePosition;
#endif
		}

		private double3 GetTotalTranslation()
		{
			var forward = _hpTransform.Forward.ToDouble3();
			var right = _hpTransform.Right.ToDouble3();
			var up = _hpTransform.Up.ToDouble3();

			var totalTranslation = double3.zero;

#if ENABLE_INPUT_SYSTEM
			up *= _upControls.ReadValue<float>() * _translationSpeed * Time.deltaTime;
			right *= _rightControls.ReadValue<float>() * _translationSpeed * Time.deltaTime;
			forward *= _forwardControls.ReadValue<float>() * _translationSpeed * Time.deltaTime;
			totalTranslation += up + right + forward;
#else

			Action<string, double3> handleAxis = (axis, vector) =>
			{
				if (Input.GetAxis(axis) != 0)
				{
					totalTranslation += vector * Input.GetAxis(axis) * _translationSpeed * Time.deltaTime;
				}
			};

			handleAxis("Vertical", forward);
			handleAxis("Horizontal", right);
			handleAxis("Jump", up);
			handleAxis("Submit", -up);
#endif

			return totalTranslation;
		}

		private static float GetMouseScrollValue()
		{
#if ENABLE_INPUT_SYSTEM
			return Mouse.current.scroll.ReadValue().y;
#else
			return Input.mouseScrollDelta.y;
#endif
		}

		private static bool IsMouseLeftClicked()
		{
#if ENABLE_INPUT_SYSTEM
			return Mouse.current.leftButton.ReadValue() == 1;
#else
			return Input.GetMouseButton(0);
#endif
		}

		private static bool IsMouseRightClicked()
		{
#if ENABLE_INPUT_SYSTEM
			return Mouse.current.rightButton.ReadValue() == 1;
#else
			return Input.GetMouseButton(1);
#endif
		}

		private void Start()
		{
			if (_arcGisMapComponent != null) return;

			Debug.LogError("An ArcGISMapComponent could not be found. Please make sure this GameObject is a child of a GameObject with an ArcGISMapComponent attached");

			enabled = false;
		}

		private void Update()
		{
			if (_arcGisMapComponent == null)
			{
				return;
			}

			if (_arcGisMapComponent.View.SpatialReference == null)
			{
				// Not functional until we have a spatial reference
				return;
			}

			DragMouseEvent();

			UpdateNavigation();
		}

		/// <summary>
		/// Move the camera based on user input
		/// </summary>
		private void UpdateNavigation()
		{
			var altitude = _arcGisMapComponent.View.AltitudeAtCartesianPosition(Position);
			UpdateSpeed(altitude);

			var totalTranslation = GetTotalTranslation();

			var scrollValue = GetMouseScrollValue();
			if (scrollValue != 0.0)
			{
				var towardsMouse = GetMouseRayCastDirection();
				var delta = MouseScrollSpeed * scrollValue;
				totalTranslation += towardsMouse * delta;

				if (altitude + totalTranslation.y < MinCameraHeight
						|| altitude + totalTranslation.y > MaxCameraHeight)
					totalTranslation.y = 0;

				// print("ZOOM DELTA: " + delta);
			}

			if (!totalTranslation.Equals(double3.zero))
			{
				MoveCamera(totalTranslation);
			}
		}

		/// <summary>
		/// Move the camera
		/// </summary>
		private void MoveCamera(double3 movDir)
		{
			var distance = math.length(movDir);
			movDir /= distance;

			var cameraPosition = Position;
			var cameraRotation = Rotation;

			if (_arcGisMapComponent.MapType == GameEngine.Map.ArcGISMapType.Global)
			{
				var spheroidData = _arcGisMapComponent.View.SpatialReference.SpheroidData;
				var nextArcGISPoint = _arcGisMapComponent.View.WorldToGeographic(movDir + cameraPosition);

				if (nextArcGISPoint.Z > MaxCameraHeight)
				{
					var point = new ArcGISPoint(nextArcGISPoint.X, nextArcGISPoint.Y, MaxCameraHeight, nextArcGISPoint.SpatialReference);
					cameraPosition = _arcGisMapComponent.View.GeographicToWorld(point);
				}
				else if (nextArcGISPoint.Z < MinCameraHeight)
				{
					var point = new ArcGISPoint(nextArcGISPoint.X, nextArcGISPoint.Y, MinCameraHeight, nextArcGISPoint.SpatialReference);
					cameraPosition = _arcGisMapComponent.View.GeographicToWorld(point);
				}
				else
				{
					cameraPosition += movDir * distance;
				}

				var newENUReference = _arcGisMapComponent.View.GetENUReference(cameraPosition);
				var oldENUReference = _arcGisMapComponent.View.GetENUReference(Position);

				cameraRotation = math.mul(math.inverse(oldENUReference.GetRotation()), cameraRotation);
				cameraRotation = math.mul(newENUReference.GetRotation(), cameraRotation);
			}
			else
			{
				cameraPosition += movDir * distance;
			}

			Position = cameraPosition;
			Rotation = cameraRotation;
		}

		private void OnTransformParentChanged()
		{
			OnEnable();
		}

		private void DragMouseEvent()
		{
			var cartesianPosition = Position;
			var cartesianRotation = Rotation;

			var deltaMouse = GetMousePosition() - _lastMouseScreenPosition;

			if (!_firstOnFocus)
			{
				if (IsMouseLeftClicked())
				{
					if (deltaMouse != Vector3.zero)
					{
						switch (_arcGisMapComponent.MapType)
						{
							case GameEngine.Map.ArcGISMapType.Global:
								GlobalDragging(ref cartesianPosition, ref cartesianRotation);
								break;
							case GameEngine.Map.ArcGISMapType.Local:
								LocalDragging(ref cartesianPosition);
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}
					}
				}
				else if (IsMouseRightClicked())
				{
					if (!deltaMouse.Equals(Vector3.zero))
					{
						RotateAround(ref cartesianPosition, ref cartesianRotation, deltaMouse);
					}
				}
				else
				{
					_firstDragStep = true;
				}
			}
			else
			{
				_firstOnFocus = false;
			}

			Position = cartesianPosition;
			Rotation = cartesianRotation;

			_lastMouseScreenPosition = GetMousePosition();
		}

		private void LocalDragging(ref double3 cartesianPosition)
		{
			var worldRayDir = GetMouseRayCastDirection();
			var isIntersected = Geometry.RayPlaneIntersection(cartesianPosition, worldRayDir, double3.zero, math.up(), out var intersection);

			if (!isIntersected || !(intersection >= 0)) return;
			
			var cartesianCoord = cartesianPosition + worldRayDir * intersection;

			var delta = _firstDragStep ? double3.zero : _lastCartesianPoint - cartesianCoord;

			_lastCartesianPoint = cartesianCoord + delta;
			cartesianPosition += delta;
			_firstDragStep = false;
		}

		private void GlobalDragging(ref double3 cartesianPosition, ref quaternion cartesianRotation)
		{
			var spheroidData = _arcGisMapComponent.View.SpatialReference.SpheroidData;
			var worldRayDir = GetMouseRayCastDirection();
			var isIntersected = Geometry.RayEllipsoidIntersection(spheroidData, cartesianPosition, worldRayDir, 0, out var intersection);

			if (!isIntersected || !(intersection >= 0)) return;
			
			var oldENUReference = _arcGisMapComponent.View.GetENUReference(cartesianPosition);

			var geoPosition = _arcGisMapComponent.View.WorldToGeographic(cartesianPosition);

			var cartesianCoord = cartesianPosition + worldRayDir * intersection;
			var currentGeoPosition = _arcGisMapComponent.View.WorldToGeographic(cartesianCoord);

			var visibleHemisphereDir = math.normalize(_arcGisMapComponent.View.GeographicToWorld(new ArcGISPoint(geoPosition.X, 0, 0, geoPosition.SpatialReference)));

			var dotVC = math.dot(cartesianCoord, visibleHemisphereDir);
			_lastDotVc = _firstDragStep ? dotVC : _lastDotVc;

			var deltaX = _firstDragStep ? 0 : _lastArcGisPoint.X - currentGeoPosition.X;
			var deltaY = _firstDragStep ? 0 : _lastArcGisPoint.Y - currentGeoPosition.Y;

			deltaY = Math.Sign(dotVC) != Math.Sign(_lastDotVc) ? 0 : deltaY;


			_lastArcGisPoint = new ArcGISPoint(currentGeoPosition.X + deltaX, currentGeoPosition.Y + deltaY, _lastArcGisPoint.Z, _lastArcGisPoint.SpatialReference);


			var YVal = geoPosition.Y + (dotVC <= 0 ? -deltaY : deltaY);
			YVal = Math.Abs(YVal) < MaxCameraLatitude ? YVal : (YVal > 0 ? MaxCameraLatitude : -MaxCameraLatitude);

			geoPosition = new ArcGISPoint(geoPosition.X + deltaX, YVal, geoPosition.Z, geoPosition.SpatialReference);

			cartesianPosition = _arcGisMapComponent.View.GeographicToWorld(geoPosition);

			var newENUReference = _arcGisMapComponent.View.GetENUReference(cartesianPosition);
			cartesianRotation = math.mul(math.inverse(oldENUReference.GetRotation()), cartesianRotation);
			cartesianRotation = math.mul(newENUReference.GetRotation(), cartesianRotation);

			_firstDragStep = false;
			_lastDotVc = dotVC;
		}

		private void RotateAround(ref double3 cartesianPosition, ref quaternion cartesianRotation, Vector3 deltaMouse)
		{
			var ENUReference = _arcGisMapComponent.View.GetENUReference(cartesianPosition).ToMatrix4x4();

			Vector2 angles;

			angles.x = deltaMouse.x / (float)Screen.width * RotationSpeed;
			angles.y = deltaMouse.y / (float)Screen.height * RotationSpeed;

			angles.y = Mathf.Min(Mathf.Max(angles.y, -90.0f), 90.0f);

			var right = Matrix4x4.Rotate(cartesianRotation).GetColumn(0);

			var rotationY = Quaternion.AngleAxis(angles.x, ENUReference.GetColumn(1));
			var rotationX = Quaternion.AngleAxis(-angles.y, right);

			cartesianRotation = rotationY * rotationX * cartesianRotation;
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

			var MousePosition = GetMousePosition();
			double3 ndcCoord = new double3(2.0 * (MousePosition.x / Screen.width) - 1.0, 2.0 * (MousePosition.y / Screen.height) - 1.0, 1);
			double3 viewRayDir = math.normalize(proj.HomogeneousTransformPoint(ndcCoord));
			return view.HomogeneousTransformVector(viewRayDir);
		}

		private void FocusChanged(bool isFocus)
		{
			_firstOnFocus = true;
		}

		private void UpdateSpeed(double height)
		{
			var msMaxSpeed = (MaxSpeed * 1000) / 3600;
			var msMinSpeed = (MinSpeed * 1000) / 3600;
			_translationSpeed = (float)(Math.Pow(Math.Min((height / 100000.0), 1), 2.0) * (msMaxSpeed - msMinSpeed) + msMinSpeed);
		}

		#region Properties
		/// <summary>
		/// Get/set the camera position in world coordinates
		/// </summary>
		private double3 Position
		{
			get
			{
				return _hpTransform.UniversePosition;
			}
			set
			{
				_hpTransform.UniversePosition = value;
			}
		}

		/// <summary>
		/// Get/set the camera rotation
		/// </summary>
		private quaternion Rotation
		{
			get
			{
				return _hpTransform.UniverseRotation;
			}
			set
			{
				_hpTransform.UniverseRotation = value;
			}
		}

		#endregion
	}
}