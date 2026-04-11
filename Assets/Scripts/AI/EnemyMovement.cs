using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float baseSpeed = 3f;
    [SerializeField] private float speedVariation = 0.7f;
    [SerializeField] private float obstacleDist = 1.2f; 
    [SerializeField] private LayerMask obstacleLayer;

    private Rigidbody2D _rb;
    private EnemyAI _ai;
    private bool _canMove = false;
    private float _initialScaleX;
    private float _finalSpeed;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _ai = GetComponent<EnemyAI>();
        _initialScaleX = Mathf.Abs(transform.localScale.x);
        _finalSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
        _rb.gravityScale = 0;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void SetMove(bool state) => _canMove = state;

    void FixedUpdate()
    {
        if (!_canMove || _ai.GetTarget() == null) { _rb.linearVelocity = Vector2.zero; return; }

        Vector2 targetPos = _ai.GetTarget().position;
        Vector2 dir = CalculateAvoidance(targetPos);
        _rb.linearVelocity = dir * _finalSpeed;
        
        if (Mathf.Abs(dir.x) > 0.1f)
            transform.localScale = new Vector3(dir.x > 0 ? _initialScaleX : -_initialScaleX, transform.localScale.y, 1);
    }

    private Vector2 CalculateAvoidance(Vector2 targetPos)
    {
        Vector2 desiredDir = (targetPos - (Vector2)transform.position).normalized;
        if (!HitWall(desiredDir)) return desiredDir;

        float[] angles = { 45f, -45f, 90f, -90f };
        foreach (float a in angles)
        {
            Vector2 checkDir = Quaternion.Euler(0, 0, a) * desiredDir;
            if (!HitWall(checkDir)) return checkDir;
        }
        return desiredDir;
    }

    private bool HitWall(Vector2 dir) => Physics2D.Raycast(transform.position, dir, obstacleDist, obstacleLayer);
}