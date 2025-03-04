namespace Mapbox.Examples
{
	using Mapbox.Unity.Location;
	using Mapbox.Unity.Utilities;
	using Mapbox.Unity.Map;
	using UnityEngine;

	public class PositionWithLocationProvider : MonoBehaviour
	{
		[SerializeField]
		private AbstractMap _map;

		[SerializeField]
		float _positionFollowFactor;

		[SerializeField]
		bool _useTransformLocationProvider;

		bool _isInitialized;

		ILocationProvider _locationProvider;
		public ILocationProvider LocationProvider
		{
			private get
			{
				if (_locationProvider == null)
				{
					_locationProvider = _useTransformLocationProvider ?
						LocationProviderFactory.Instance.TransformLocationProvider : LocationProviderFactory.Instance.DefaultLocationProvider;
				}

				return _locationProvider;
			}
			set
			{
				if (_locationProvider != null)
				{
					_locationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
				}
				_locationProvider = value;
				_locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
			}
		}

		Vector3 _targetPosition;

		void Start()
		{
			LocationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
			_map.OnInitialized += () => _isInitialized = true;
			Debug.Log("PositionWithLocationProvider started.");
		}

		void OnDestroy()
		{
			if (LocationProvider != null)
			{
				LocationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
			}
		}

		void LocationProvider_OnLocationUpdated(Location location)
		{
			if (_isInitialized && location.IsLocationUpdated)
			{
				Debug.Log($"Location received: {location.LatitudeLongitude}");
				_targetPosition = _map.GeoToWorldPosition(location.LatitudeLongitude);
				Debug.Log($"Location updated: {_targetPosition}");
			}
		}

		void Update()
		{
			transform.localPosition = Vector3.Lerp(transform.localPosition, _targetPosition, Time.deltaTime * _positionFollowFactor);
		}
	}
}