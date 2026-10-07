using UnityEngine;

/// <summary>Visual wildlife: local hex wandering, independent of combat and logistics.</summary>
public sealed class HexAnimalWander : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [Header("Прогулка внутри гекса")]
    [SerializeField] private Vector2 areaHalfSize = new Vector2(1.1f, .9f);
    [SerializeField] private Vector2 speedRange = new Vector2(.18f, .35f);
    [SerializeField] private Vector2 pauseRange = new Vector2(.7f, 3f);
    [SerializeField] private bool spriteFacesLeft = true;
    [Tooltip("Минимальный порядок отрисовки. Должен быть выше подложки гекса.")]
    [SerializeField] private int baseSortingOrder = 10;
    private Vector3 target;
    private float pause;
    private float speed;

    private void OnEnable()
    {
        // State groups can be switched off and back on by their production counter.
        transform.localPosition = RandomPointInArea();
        target = transform.localPosition;
        pause = Random.Range(Mathf.Max(0, pauseRange.x), Mathf.Max(pauseRange.x, pauseRange.y));
        UpdateVisual();
    }

    private void Update() => Advance(Time.deltaTime);

    public void Advance(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        if (pause > 0f)
        {
            pause -= deltaTime;
            if (pause <= 0f) PickTarget();
            return;
        }
        var position = transform.localPosition;
        var dx = target.x - position.x;
        if (visual != null && Mathf.Abs(dx) > .001f)
            visual.flipX = spriteFacesLeft ? dx > 0f : dx < 0f;
        transform.localPosition = Vector3.MoveTowards(position, target, speed * deltaTime);
        UpdateVisual();
        if ((transform.localPosition - target).sqrMagnitude < .000001f)
            pause = Random.Range(Mathf.Max(.05f, pauseRange.x), Mathf.Max(.05f, Mathf.Max(pauseRange.x, pauseRange.y)));
    }

    private void PickTarget()
    {
        target = RandomPointInArea();
        speed = Random.Range(Mathf.Max(.01f, speedRange.x), Mathf.Max(.01f, Mathf.Max(speedRange.x, speedRange.y)));
    }

    private Vector3 RandomPointInArea()
    {
        var half = new Vector2(Mathf.Max(.05f, areaHalfSize.x), Mathf.Max(.05f, areaHalfSize.y));
        // Convex flat-top hex. Both endpoints and the entire connecting segment stay inside.
        float y = Random.Range(-half.y, half.y);
        float width = half.x * (1f - .5f * Mathf.Abs(y) / half.y);
        return new Vector3(Random.Range(-width, width), y, transform.localPosition.z);
    }

    private void UpdateVisual()
    {
        if (visual != null)
            visual.sortingOrder = baseSortingOrder + Mathf.RoundToInt(
                Mathf.Max(0f, areaHalfSize.y - transform.localPosition.y) * 10f);
    }

    private void OnDrawGizmosSelected()
    {
        if (transform.parent == null) return;
        Gizmos.color = new Color(.45f, .9f, .45f, .7f);
        var h = areaHalfSize;
        Vector3[] corners = { new(h.x,0), new(h.x*.5f,h.y), new(-h.x*.5f,h.y), new(-h.x,0), new(-h.x*.5f,-h.y), new(h.x*.5f,-h.y) };
        for (int i=0;i<6;i++) Gizmos.DrawLine(transform.parent.TransformPoint(corners[i]),transform.parent.TransformPoint(corners[(i+1)%6]));
    }
}
