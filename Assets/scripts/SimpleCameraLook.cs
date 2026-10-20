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

    private static float externalLookX = 0f;
    private static float externalLookY = 0f;

    public static void AddLookDelta(float dx, float dy)
    {
        externalLookX += dx;
        externalLookY += dy;
    }

    void Update()
    {
        // إذا كنا نضغط على UI أو النوافذ المفتوحة لا تدور الكاميرا
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (InGamePauseMenuUI.IsPauseMenuOpen || 
            LanguageSelectionUI.IsLanguageModalOpen || 
            CharacterSelectionUI.IsSelectionOpen || 
            GameIntroBriefingUI.IsBriefingOpen || 
            PlayerProgressionAndShopUI.IsShopOpen)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // دعم تحريك وتوجيه الكاميرا عبر اللمس في النصف الأيمن من الشاشة (مثل فري فاير)
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.position.x > Screen.width * 0.35f)
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                        continue;

                    if (t.phase == TouchPhase.Moved)
                    {
                        float touchSens = mouseSensitivity * 0.40f;
                        mouseX += t.deltaPosition.x * touchSens * Time.deltaTime;
                        mouseY += t.deltaPosition.y * touchSens * Time.deltaTime;
                    }
                }
            }
        }

        if (Mathf.Abs(externalLookX) > 0.0001f || Mathf.Abs(externalLookY) > 0.0001f)
        {
            mouseX += externalLookX;
            mouseY += externalLookY;
            externalLookX = 0f;
            externalLookY = 0f;
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -40f, 60f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}