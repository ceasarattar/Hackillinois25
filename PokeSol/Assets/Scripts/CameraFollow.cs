using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // The transform to follow (PlayerTarget)
    public float followSpeed = 5f; // Speed at which the camera follows
    public float rotationSpeed = 2f; // Reduced speed at which the camera rotates (was 5f)
    public Vector3 offset = new Vector3(0, 10, -10); // Base offset from the target

    void LateUpdate()
    {
        if (target == null) return;

        // Get the player's forward direction (based on their rotation)
        Vector3 playerForward = target.forward;

        // Calculate a slightly rotated offset based on the player's facing direction
        // Blend the base offset with a small rotation based on the player's direction
        Vector3 rotatedOffset = Quaternion.Euler(0, target.eulerAngles.y * 0.3f, 0) * offset; // Reduced rotation influence (0.3f)

        // Calculate the desired position based on the target's position and rotated offset
        Vector3 desiredPosition = target.position + rotatedOffset;

        // Smoothly move the camera to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Smoothly rotate the camera to look at the player
        Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}