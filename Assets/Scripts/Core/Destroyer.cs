using UnityEngine;

public class Destroyer : MonoBehaviour
{
    // Этот метод мы будем вызывать через Unity Event
    public void SelfDestruct()
    {
        Destroy(gameObject);
    }
}