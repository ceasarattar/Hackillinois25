using UnityEngine;
using Mapbox.Unity.Location;
using Mapbox.Utils;

public class SimulatedLocationProvider : AbstractLocationProvider
{
    [SerializeField]
    private float _latitude = 40.7128f;
    [SerializeField]
    private float _longitude = -74.0060f;

    private float _lastLatitude;
    private float _lastLongitude;
    protected Location _location;
    private bool _mapInitialized = false;

    // Public getters for latitude and longitude
    public float Latitude => _latitude;
    public float Longitude => _longitude;

    void Start()
    {
        _lastLatitude = _latitude;
        _lastLongitude = _longitude;
        UpdateLocation();
    }

    void Update()
    {
        if (!_mapInitialized)
        {
            UpdateLocation();
        }
        else if (_latitude != _lastLatitude || _longitude != _lastLongitude)
        {
            Debug.Log($"Location changed to: ({_latitude}, {_longitude})");
            UpdateLocation();
            _lastLatitude = _latitude;
            _lastLongitude = _longitude;
        }
    }

    public void SetLocation(float lat, float lon)
    {
        _latitude = lat;
        _longitude = lon;
        UpdateLocation(); // Call UpdateLocation to send the update
        Debug.Log($"Setting location to: ({lat}, {lon}) and triggering update.");
    }

    public void OnMapInitialized()
    {
        _mapInitialized = true;
        Debug.Log("Map initialized, stopping continuous location updates.");
    }

    private void UpdateLocation()
    {
        _location = new Location
        {
            LatitudeLongitude = new Vector2d(_latitude, _longitude),
            IsLocationUpdated = true
        };
        Debug.Log($"Sending location update: ({_location.LatitudeLongitude.x}, {_location.LatitudeLongitude.y})");
        SendLocation(_location);
    }
}