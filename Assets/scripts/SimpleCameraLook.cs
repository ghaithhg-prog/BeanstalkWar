using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleCameraLook : MonoBehaviour
{
    public Transform cameraTransform;
    public float mouseSensitivity = 200f;
    float xRotation = 0f;

    void Start()
    {
      //  Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
{
    // إذا كنا نضغط على UI (مثل الجويستيك) لا تدور الكاميرا
    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        return;

    float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
    float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
    // إذا كان الجويستيك يتحرك نستخدمه
    xRotation -= mouseY;
    xRotation = Mathf.Clamp(xRotation, -40f, 60f);

    cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    transform.Rotate(Vector3.up * mouseX);
}
}