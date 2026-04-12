using UnityEngine;

public class PlacementValidator : MonoBehaviour
{
    private int _collidersCount = 0;
    private SpriteRenderer _sr;
    
    public bool IsValid => _collidersCount == 0;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    // Если что-то входит в зону чертежа
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.isTrigger) _collidersCount++;
        UpdateVisual();
    }

    // Если что-то выходит
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.isTrigger) _collidersCount--;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_sr == null) return;
        // Подсвечиваем красным, если строить нельзя
        _sr.color = IsValid ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
    }
}