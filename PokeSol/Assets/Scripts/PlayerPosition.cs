using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class PlayerPosition : MonoBehaviour
{
    public AbstractMap map;
    private SimulatedLocationProvider locationProvider;
    private Vector3 targetPosition;
    private bool isMoving;
    public float moveSpeed = 5f;

    private Vector2d lastLatLon;

    void Start()
    {
        locationProvider = FindObjectOfType<SimulatedLocationProvider>();
        if (locationProvider == null)
        {
            Debug.LogError("SimulatedLocationProvider not found!");
            return;
        }
        map = FindObjectOfType<AbstractMap>();
        if (map == null)
        {
            Debug.LogError("AbstractMap not found!");
            return;
        }
        lastLatLon = new Vector2d(locationProvider.Latitude, locationProvider.Longitude);
        Vector3 worldPos = map.GeoToWorldPosition(lastLatLon, false);
        targetPosition = new Vector3(worldPos.x, transform.position.y, worldPos.z);
        transform.position = targetPosition;
    }

    void Update()
    {
        if (locationProvider == null || map == null) return;

        Vector2d currentLatLon = new Vector2d(locationProvider.Latitude, locationProvider.Longitude);

        if (currentLatLon.x != lastLatLon.x || currentLatLon.y != lastLatLon.y)
        {
            Vector3 worldPos = map.GeoToWorldPosition(currentLatLon, false);
            targetPosition = new Vector3(worldPos.x, transform.position.y, worldPos.z);
            lastLatLon = currentLatLon;
            isMoving = true;
            Debug.Log($"Player moving to: ({currentLatLon.x}, {currentLatLon.y})");
        }

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, 
                targetPosition, 
                moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
                Debug.Log("Player stopped moving.");
            }
        }
    }
}