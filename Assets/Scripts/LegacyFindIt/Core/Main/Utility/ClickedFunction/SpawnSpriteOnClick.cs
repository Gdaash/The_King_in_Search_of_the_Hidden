using System.Collections;
using DeskCat.FindIt.Scripts.Core.Main.System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeskCat.FindIt.Scripts.Core.Main.Utility.ClickedFunction
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class SpawnSpriteOnClick : MonoBehaviour, IPointerClickHandler
    {
        public GameObject spawnedPrefab;
        public float lifespan = 0.35f;
        public AudioClip spawnSound;

        private float timer;
        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;
            StartCoroutine(EnableTrigger());
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (CameraView2D.instance.IsPanning) return;
            if (_camera != null && eventData.button == PointerEventData.InputButton.Left)
            {
                Vector2 clickPosition = _camera.ScreenToWorldPoint(eventData.position);
                var hit = Physics2D.Raycast(clickPosition, Vector2.zero);
                Debug.Log("Hit Point: " + eventData.position);

                if (hit.collider == null) return;
                var hitPoint = hit.point;
                Debug.Log("Hit Point: " + hitPoint);

                var spawnedObject = Instantiate(spawnedPrefab);
                spawnedObject.transform.position = hitPoint;
                LevelManager.PlayItemFx(spawnSound);
                Destroy(spawnedObject, lifespan);
            }
        }

        IEnumerator EnableTrigger()
        {
            yield return new WaitForSeconds(2f);
            GetComponent<BoxCollider2D>().enabled = true;
        }
    }
}