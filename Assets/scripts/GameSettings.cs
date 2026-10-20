using UnityEngine;

public class GameSettings : MonoBehaviour
{
    void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    void Start()
    {
        ApplyAtmosphericVisuals();
    }

    void ApplyAtmosphericVisuals()
    {
        // Clear visibility without blinding or obscuring fog
        RenderSettings.fog = false;

        // 2. تحسين الإضاءة المحيطة لتكون دافئة ومليئة بالحياة
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.6f, 0.75f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.6f, 0.45f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.3f, 0.2f);

        // 3. ضبط اتجاه وسطوع ضوء الشمس الرئيسي
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sun = l;
                    break;
                }
            }
        }

        if (sun != null)
        {
            sun.color = new Color(1.0f, 0.96f, 0.88f); // ضوء شمس دافئ ذهبي
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.65f;
        }
    }
}