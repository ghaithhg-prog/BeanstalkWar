using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 6f;
    public float height = 2f;
    public float mouseSensitivity = 200f;

    float currentYaw = 0f;
    float currentPitch = 15f;

    void Update()
    {
        // ✅ إذا لم يوجد target ابحث عن Player تلقائياً
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                target = player.transform;
            return;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        currentYaw += mouseX;
        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(currentPitch, -30f, 60f);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 position = target.position - rotation * Vector3.forward * distance + Vector3.up * height;

        transform.position = position;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}