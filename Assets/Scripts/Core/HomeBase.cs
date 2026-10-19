using UnityEngine;

public class HomeBase : MonoBehaviour
{
    private void OnEnable() => CombatTargetRegistry.Register(this);
    private void OnDisable() => CombatTargetRegistry.Unregister(this);
}
