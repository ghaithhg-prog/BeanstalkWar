using UnityEngine;
using UnityEngine.UI;

public class MobileUI : MonoBehaviour
{
    public Button attackButton;
    public Button jumpButton;
    public SimpleJoystick joystick;
    public GameObject joystickBackground;

    private CharacterCombat combat;
    private PlayerMovement movement;
    private bool connected = false;

    void Start()
    {
        bool isMobile = Application.isMobilePlatform;

        if (!isMobile)
        {
            // ✅ إخفاء كل عناصر الموبايل على PC
            if (attackButton != null)
                attackButton.gameObject.SetActive(false);

            if (jumpButton != null)
                jumpButton.gameObject.SetActive(false);

            if (joystickBackground != null)
                joystickBackground.SetActive(false);

            if (joystick != null)
                joystick.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (connected) return;

        // ✅ على PC لا نربط شيئاً
        if (!Application.isMobilePlatform) 
        {
            connected = true;
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            combat = player.GetComponent<CharacterCombat>();
            movement = player.GetComponent<PlayerMovement>();

            if (joystick != null && movement != null)
                movement.joystick = joystick;

            if (attackButton != null && combat != null)
            {
                attackButton.onClick.RemoveAllListeners();
                attackButton.onClick.AddListener(
                    combat.OnAttackButtonPressed);
            }

            if (jumpButton != null && movement != null)
            {
                jumpButton.onClick.RemoveAllListeners();
                jumpButton.onClick.AddListener(
                    movement.OnJumpButtonPressed);
            }

            connected = true;
        }
    }
}