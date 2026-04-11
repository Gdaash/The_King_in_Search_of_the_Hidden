using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ObjectPools
{
    /// <summary>
    /// Простой пул объектов
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 10;
        [SerializeField] private List<GameObject> _startList;
    
        private readonly Queue<GameObject> _pool = new();
        private readonly HashSet<GameObject> _active = new();
        
        private void Awake()
        {
            // Создаём начальные объекты
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = Create();
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
            
            foreach (GameObject obj in _startList)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    _pool.Enqueue(obj);
                }
            }
        }
    
        public GameObject Get()
        {
            GameObject obj;
        
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                // Пул пуст — создаём новый
                obj = Create();
            }
        
            obj.SetActive(true);
            _active.Add(obj);
        
            return obj;
        }
    
        public void Return(GameObject obj)
        {
            if (!_active.Contains(obj)) return;
        
            obj.SetActive(false);
            _pool.Enqueue(obj);
            _active.Remove(obj);
        }
    
        public void ReturnAll()
        {
            foreach (var obj in _active)
            {
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
            _active.Clear();
        }
    
        private void OnDestroy()
        {
            // Очищаем все объекты при уничтожении пула
            foreach (var obj in _pool)
                Destroy(obj);
            foreach (var obj in _active)
                Destroy(obj);
        
            _pool.Clear();
            _active.Clear();
        }

        private GameObject Create()
        {
            if (prefab == null)
            {
                GameObject obj = new GameObject();
                obj.transform.parent = transform;
                return obj;
            }
            else
            {
                return Instantiate(prefab, transform);
            }
        }
    }
}