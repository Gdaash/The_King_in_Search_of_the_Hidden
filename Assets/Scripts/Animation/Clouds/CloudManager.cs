using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using ObjectPools;
using Random = UnityEngine.Random;

namespace Animation.Clouds
{
    /// <summary> Push to hint
    /// Менеджер облаков, генерирует, двигает и тп
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(ObjectPool))]
    public class CloudManager : MonoBehaviour
    {
        /// <summary>
        /// Спрайты облаков
        /// </summary>
        [SerializeField] private List<Sprite>  _spritesClouds;
        
        /// <summary>
        /// Минимальный интервал с которым появляются облака
        /// </summary>
        [SerializeField] 
        private float _minInterval;
        
        /// <summary>
        /// Максимальный интервал с которым появляются облака
        /// </summary>
        [SerializeField] 
        private float _maxInterval;
        
        /// <summary>
        /// Минимальная скорость облака
        /// </summary>
        [SerializeField] 
        private float _minSpeed;
        
        /// <summary>
        /// Максимальная скорость 
        /// </summary>
        [SerializeField] 
        private float _maxSpeed;
        
        /// <summary>
        /// Минимальный размер
        /// </summary>
        [SerializeField] 
        private float _minScale;
        
        /// <summary>
        /// Направление движения
        /// </summary>
        [SerializeField] 
        private Forward _forward;
        
        /// <summary>
        /// Кривая движения облака
        /// </summary>
        [SerializeField] 
        private AnimationCurve _curve;
        
        private BoxCollider2D _boxCollider2D;
        private ObjectPool _pool;
        
        private CancellationTokenSource _cancellationToken;
        
        private void Awake()
        {
            transform.position = new Vector3(0, 0, 1);
            
            TryGetComponent(out _boxCollider2D);
            TryGetComponent(out _pool);
            _boxCollider2D.isTrigger = true;            
        }

        async UniTaskVoid Start()
        {
            print("UniTaskVoid start");
            await RunClouds();
            //RunClouds().Forget();
            print("UniTaskVoid end");
        }

        private async UniTask RunClouds()
        {
            _cancellationToken?.Dispose();
            _cancellationToken = new CancellationTokenSource();

            while (!_cancellationToken.IsCancellationRequested)
            {
                GameObject cloud = GetCloud();
                
                Vector2 startPoint = GetStartPoint();
                Vector2 endPoint = GetEndPoint(startPoint.y);
                
                
                float time = Vector2.Distance(startPoint, endPoint) / Random.Range(_minSpeed, _maxSpeed);
                
                cloud.transform.position = startPoint;
                cloud.SetActive(true);
                
                PrimeTween.Tween.Position(cloud.transform, endPoint, time, _curve)
                    .OnComplete(() => ReturnCloud(cloud));
                
                await UniTask.WaitForSeconds(
                    Random.Range(_minInterval, _maxInterval), 
                    cancellationToken: _cancellationToken.Token);
            }
        }

        private void OnDestroy()
        {
            _cancellationToken?.Cancel();
        }

        private Vector2 GetStartPoint()
        {
            Vector2 position = _boxCollider2D.offset + new Vector2
                (_forward == Forward.LeftToRight ? _boxCollider2D.bounds.min.x : _boxCollider2D.bounds.max.x, 
                 UnityEngine.Random.Range(_boxCollider2D.bounds.min.y, _boxCollider2D.bounds.max.y));
            
            return transform.TransformPoint(position);
        }

        private Vector2 GetEndPoint(float y)
        {
            Vector2 position = new Vector2
                (_boxCollider2D.offset.x + (_forward == Forward.LeftToRight ? _boxCollider2D.bounds.max.x : _boxCollider2D.bounds.min.x), y);
            
            return transform.TransformPoint(position);
        }

        private GameObject GetCloud()
        {
            GameObject cloud = _pool.Get();
            if (!cloud.TryGetComponent(out SpriteRenderer sprite))
            {
                sprite = cloud.AddComponent<SpriteRenderer>();
            }
            
            sprite.sprite = _spritesClouds[Random.Range(0, _spritesClouds.Count - 1 )];
            sprite.sortingOrder = 99;

            float scale = Random.Range(_minScale, 1);

            cloud.transform.localScale = new Vector3(scale, scale, 1);
            
            return cloud;
        }

        private void ReturnCloud(GameObject cloud)
        {
            _pool.Return(cloud);
        }
    }
}