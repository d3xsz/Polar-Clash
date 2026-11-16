using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Fare ile tiklanabilir UI ogesi: uzerine gelince buyur ve cizgisi parlar, basinca icine gocer.
// Sol tik onClick, sag tik onRightClick (yoksa onClick) calistirir.
public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public System.Action onClick;
    public System.Action onRightClick;
    public float hoverScale = 1.05f;

    bool hover, down;
    Vector3 baseScale;
    Image line;
    Color lineBase;

    void Awake()
    {
        baseScale = transform.localScale;
        var l = transform.Find("Cizgi");
        if (l != null) { line = l.GetComponent<Image>(); if (line != null) lineBase = line.color; }
    }

    void OnDisable() { hover = down = false; transform.localScale = baseScale; if (line != null) line.color = lineBase; }

    public void OnPointerEnter(PointerEventData e) { hover = true; SoundFX.Play("hover", 0.5f); }
    public void OnPointerExit(PointerEventData e) { hover = false; down = false; }
    public void OnPointerDown(PointerEventData e) { down = true; }
    public void OnPointerUp(PointerEventData e) { down = false; }

    public void OnPointerClick(PointerEventData e)
    {
        var a = e.button == PointerEventData.InputButton.Right && onRightClick != null ? onRightClick : onClick;
        if (a == null) return;
        SoundFX.Play("click");
        a();
    }

    void Update()
    {
        float target = down ? 0.96f : (hover ? hoverScale : 1f);
        float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f);
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * target, k);
        if (line != null)
        {
            var c = lineBase;
            if (hover) c.a = Mathf.Min(1f, lineBase.a * 2.2f + 0.15f);
            line.color = Color.Lerp(line.color, c, k);
        }
    }
}
