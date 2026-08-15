using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public RectTransform handle;
    public float moveRange = 100f;

    private Vector2 startPos;
    private Vector2 inputVector = Vector2.zero;

    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;

    void Start()
    {
        startPos = handle.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        
        // نحصل على موضع الماوس بالنسبة للخلفية
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(),
            eventData.position,
            null, // مهم جدًا: null للـ Overlay
            out localPoint
        );

        // نحسب المسافة من المركز
        Vector2 center = GetComponent<RectTransform>().rect.center;
        Vector2 delta = localPoint - center;

        // نحدد المسافة القصوى
        if (delta.magnitude > moveRange)
        {
            delta = delta.normalized * moveRange;
        }

        // نحرك الـ Handle
        handle.anchoredPosition = startPos + delta;

        // نحسب القيمة النهائية (-1 إلى 1)
        inputVector = delta / moveRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        handle.anchoredPosition = startPos;
        inputVector = Vector2.zero;
    }
}