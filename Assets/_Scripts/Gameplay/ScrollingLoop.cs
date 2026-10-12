using UnityEngine;

// Endless strip made of identical segments laid side by side; a segment that leaves on the left wraps to the right.
public class ScrollingLoop : MonoBehaviour
{
    [SerializeField] private Transform[] _segments;
    [SerializeField] private float _segmentWidth;
    [SerializeField] private float _speedFactor = 1f;
    [Tooltip("Used when no LevelScroller is assigned (e.g. the menu).")]
    [SerializeField] private float _fallbackSpeed = 2.4f;
    [SerializeField] private LevelScroller _scroller;

    private void Update()
    {
        float speed = (_scroller != null ? _scroller.CurrentSpeed : _fallbackSpeed) * _speedFactor;
        if (speed <= 0f)
            return;

        float delta = speed * Time.deltaTime;
        float wrapThreshold = -_segmentWidth;
        float loopLength = _segmentWidth * _segments.Length;

        foreach (Transform segment in _segments)
        {
            Vector3 position = segment.localPosition;
            position.x -= delta;
            if (position.x <= wrapThreshold)
                position.x += loopLength;
            segment.localPosition = position;
        }
    }
}
