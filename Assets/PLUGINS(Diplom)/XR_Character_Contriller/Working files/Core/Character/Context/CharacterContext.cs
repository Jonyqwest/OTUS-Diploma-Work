using UnityEngine;

public class CharacterContext
{
    public Transform Ball { get; }
    private Transform _cam;

    public CharacterContext(Transform ball)
    {
        Ball = ball;
        _cam = Camera.main != null ? Camera.main.transform : null;
    }

    public Transform GetHead()
    {
        if (_cam == null) _cam = Camera.main != null ? Camera.main.transform : null;
        return _cam;
    }
}
