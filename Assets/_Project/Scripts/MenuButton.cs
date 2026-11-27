using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MenuButton : Button, IPointerMoveHandler, ICancelHandler
{
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text label;
    [SerializeField] private UnityEvent onCancel = new UnityEvent();

    public void OnPointerMove(PointerEventData eventData)
    {
        if (eventData.IsPointerMoving() && IsInteractable())
            Select();
    }

    public void OnCancel(BaseEventData eventData)
    {
        onCancel.Invoke();
        eventData.Use();
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        if (fill == null || label == null)
            return;

        // The EventSystem owns focus. Pointer highlight alone should not invert a second button.
        bool inverted = state == SelectionState.Selected || state == SelectionState.Pressed;
        fill.color = state == SelectionState.Pressed
            ? new Color32(153, 153, 153, 255)
            : inverted ? Color.white : Color.black;
        label.color = inverted ? Color.black : Color.white;
    }
}
