using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    public float distanceFromHead = 0.6f; // How far away it floats
    public float smoothSpeed = 5f;
    private Transform head;

    void Start()
    {
        head = Camera.main.transform;
    }

    void LateUpdate()
    {
        // 1. Calculate target position in front of face
        Vector3 targetPosition = head.position + (head.forward * distanceFromHead);

        // 2. Smoothly move there (so it doesn't jitter)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // 3. Face the user
        transform.LookAt(transform.position + head.rotation * Vector3.forward,
                         head.rotation * Vector3.up);
    }
}