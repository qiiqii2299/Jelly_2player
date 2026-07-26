using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    [SerializeField] private float hoverScale = 1.1f; // Độ phóng to (1.1 = to lên 10%)

    private void Start()
    {
        originalScale = transform.localScale;
    }

    // Khi chuột di chuyển vào
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * hoverScale;
    }

    // Khi chuột rời đi
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }
}