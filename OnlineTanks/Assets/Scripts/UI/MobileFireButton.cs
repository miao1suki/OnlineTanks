using UnityEngine;
using UnityEngine.EventSystems;

public class MobileFireButton : MonoBehaviour, IPointerDownHandler
{
    PlayerInputHandler target;

    void Update()
    {
        if (target == null)
            target = FindLocalInput();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (PlayerInputHandler.Local != null)
            PlayerInputHandler.Local.FirePressed = true;
    }

    PlayerInputHandler FindLocalInput()
    {
        var inputs = Object.FindObjectsByType<PlayerInputHandler>(FindObjectsSortMode.None);
        foreach (var i in inputs)
        {
            var pc = i.GetComponent<PlayerController>();
            if (pc != null && pc.isLocalPlayer)
                return i;
        }
        return null;
    }
}