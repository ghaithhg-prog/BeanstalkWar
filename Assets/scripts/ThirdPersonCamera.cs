using UnityEngine;
using Unity.Netcode;

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
        // ابحث فقط عن اللاعب الذي يملكه هذا الجهاز
        if (target == null)
        {
            FindLocalPlayer();
        }
    }

    void FindLocalPlayer()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.IsClient)
            return;

        NetworkObject localPlayer =
            NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

        if (localPlayer != null)
        {
            target = localPlayer.transform;

            Debug.Log(
                "Camera connected to local player: " +
                localPlayer.OwnerClientId
            );
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity *
            Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity *
            Time.deltaTime;

        currentYaw += mouseX;
        currentPitch -= mouseY;

        currentPitch =
            Mathf.Clamp(currentPitch, -30f, 60f);

        Quaternion rotation =
            Quaternion.Euler(
                currentPitch,
                currentYaw,
                0f
            );

        Vector3 position =
            target.position -
            rotation * Vector3.forward * distance +
            Vector3.up * height;

        transform.position = position;

        transform.LookAt(
            target.position +
            Vector3.up * 1.5f
        );
    }
}