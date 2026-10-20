using UnityEngine;

/// <summary>
/// TreeDamageForwarder:
/// يُثبَّت على كل أجزاء الجذع الخشبي المجدول (Colliders)
/// لضمان توجيه أي هجوم أو ضرر فوراً إلى شجرة اللعبة الرئيسية ونقصان صحتها
/// مع حماية ذكية لمنع تضاعف الضرر إذا اخترق مقذوف واحد عدة أجزاء في نفس اللحظة
/// </summary>
public class TreeDamageForwarder : MonoBehaviour
{
    private static int lastDamageFrame = -1;
    private static float frameDamageAccum = 0f;
    private const float MaxDamagePerFrame = 250f;

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (TreeGrowth.Instance == null) return;

        // منع تكرار الضرر الهائل إذا لمس مقذوف عدة كوليدرات للشجرة في نفس الفريم
        if (Time.frameCount != lastDamageFrame)
        {
            lastDamageFrame = Time.frameCount;
            frameDamageAccum = 0f;
        }

        if (frameDamageAccum >= MaxDamagePerFrame)
        {
            return;
        }

        float allowedDamage = Mathf.Min(amount, MaxDamagePerFrame - frameDamageAccum);
        frameDamageAccum += allowedDamage;

        TreeGrowth.Instance.TakeDamage(allowedDamage, attacker);
    }

    public void Heal(float amount)
    {
        if (TreeGrowth.Instance != null)
        {
            TreeGrowth.Instance.Heal(amount);
        }
    }
}
