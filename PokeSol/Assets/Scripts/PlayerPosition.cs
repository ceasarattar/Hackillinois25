using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class PlayerPosition : MonoBehaviour
{
    public AbstractMap map;
    private SimulatedLocationProvider locationProvider;
    private Vector3 targetPosition;
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    private Vector2d lastLatLon;
    private Vector3 movementDirection;

    // Variables for on-screen button input
    private Vector3 buttonInputDirection = Vector3.zero;

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

        HandleKeyboardInput();
        MovePlayer();
    }

    void HandleKeyboardInput()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        movementDirection = new Vector3(moveX, 0, moveZ).normalized;

        // If no keyboard input, check for button input
        if (movementDirection.magnitude == 0)
        {
            movementDirection = buttonInputDirection;
        }

        if (movementDirection.magnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void MovePlayer()
    {
        if (movementDirection.magnitude > 0)
        {
            transform.position += movementDirection * moveSpeed * Time.deltaTime;
        }
    }

    // Functions to be called by UI buttons
    public void MoveUp() { buttonInputDirection = Vector3.forward; }
    public void MoveDown() { buttonInputDirection = Vector3.back; }
    public void MoveLeft() { buttonInputDirection = Vector3.left; }
    public void MoveRight() { buttonInputDirection = Vector3.right; }
    public void StopMovement() { buttonInputDirection = Vector3.zero; }
}
