namespace Mapbox.Unity.Map
{
    using System.Collections;
    using Mapbox.Unity.Location;
    using UnityEngine;

    public class InitializeMapWithLocationProvider : MonoBehaviour
    {
        [SerializeField]
        AbstractMap _map;

        ILocationProvider _locationProvider;

        private void Awake()
        {
            // Prevent double initialization of the map. 
            _map.InitializeOnStart = false;
        }

        protected virtual IEnumerator Start()
        {
            yield return null;
            _locationProvider = LocationProviderFactory.Instance.DefaultLocationProvider;
            if (_locationProvider == null)
            {
                Debug.LogError("LocationProvider is null! Map initialization will fail.");
                yield break;
            }
            Debug.Log($"LocationProvider set: {_locationProvider.GetType().Name}");
            _locationProvider.OnLocationUpdated += LocationProvider_OnLocationUpdated;
        }

        void LocationProvider_OnLocationUpdated(Location location)
        {
            _locationProvider.OnLocationUpdated -= LocationProvider_OnLocationUpdated;
            _map.Initialize(location.LatitudeLongitude, _map.AbsoluteZoom);
            Debug.Log("Map initialized.");

            // Signal to SimulatedLocationProvider that the map is initialized
            var simulatedProvider = _locationProvider as SimulatedLocationProvider;
            if (simulatedProvider != null)
            {
                simulatedProvider.OnMapInitialized();
            }
        }
    }
}