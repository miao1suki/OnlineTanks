using UnityEngine;
using UnityEngine.EventSystems;

public class MobileBoostButton :
    MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    PlayerInputHandler input;

    void Update()
    {
        if (input == null)
            input = PlayerInputHandler.Local;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (input != null)
            input.BoostHeld = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (input != null)
            input.BoostHeld = false;
    }
}