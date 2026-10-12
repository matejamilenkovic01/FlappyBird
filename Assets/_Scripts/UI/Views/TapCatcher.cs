using UnityEngine;
using UnityEngine.EventSystems;

// Transparent full-screen graphic placed behind the other HUD elements. Taps that no UI element
// above it consumes become flaps, which handles touch-vs-UI without manual raycast checks.
public class TapCatcher : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData) => GameEvents.RaiseFlapRequested();
}
