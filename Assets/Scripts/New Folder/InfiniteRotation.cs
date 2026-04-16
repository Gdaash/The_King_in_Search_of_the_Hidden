using UnityEngine;

public class InfiniteRotation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 100f; // Скорость вращения
    [SerializeField] private Vector3 rotationAxis = Vector3.forward; // Ось (Z для 2D)

    void Update()
    {
        // Вращение: скорость * время между кадрами * ось
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}