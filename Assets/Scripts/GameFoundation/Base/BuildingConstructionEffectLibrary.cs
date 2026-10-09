using System;
using UnityEngine;

namespace GameFoundation.Base
{
    [CreateAssetMenu(menuName = "Those UnderHex/Building Construction Effect Library")]
    public sealed class BuildingConstructionEffectLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class MaterialFragments
        {
            public ResourceType resource;
            public Sprite[] fragments;
        }

        [SerializeField] private MaterialFragments[] materials;
        [SerializeField] private Sprite[] dustSprites;
        [Header("Эффект стройки")]
        [SerializeField, Min(0.05f)] private float shakeDuration = 0.5f;
        [SerializeField, Min(0f)] private float shakeDistance = 0.11f;
        [SerializeField, Min(0f)] private float shakeAngle = 1.5f;
        [SerializeField, Min(0)] private int fragmentsPerMaterial = 6;
        [SerializeField, Min(0)] private int dustParticleCount = 5;
        [SerializeField] private Vector2 fragmentLifetime = new Vector2(0.65f, 0.95f);
        [SerializeField] private Vector2 fragmentUpSpeed = new Vector2(0.75f, 1.25f);
        [SerializeField, Min(0f)] private float sidewaysSpeed = 0.65f;
        [SerializeField] private Vector2 dustLifetime = new Vector2(0.45f, 0.75f);
        [SerializeField, Min(0f)] private float dustDrag = 2.3f;
        [SerializeField, Min(0f)] private float gravity = 1.1f;
        [SerializeField, Min(0)] private int sortingOrderOffset = 2;

        public float ShakeDuration => shakeDuration;
        public float ShakeDistance => shakeDistance;
        public float ShakeAngle => shakeAngle;
        public int FragmentsPerMaterial => fragmentsPerMaterial;
        public int DustParticleCount => dustParticleCount;
        public Vector2 FragmentLifetime => fragmentLifetime;
        public Vector2 FragmentUpSpeed => fragmentUpSpeed;
        public float SidewaysSpeed => sidewaysSpeed;
        public Vector2 DustLifetime => dustLifetime;
        public float DustDrag => dustDrag;
        public float Gravity => gravity;
        public int SortingOrderOffset => sortingOrderOffset;

        public Sprite[] FragmentsFor(ResourceType resource)
        {
            if (resource == null || materials == null) return null;
            foreach (var material in materials)
                if (material != null && material.resource == resource)
                    return material.fragments;
            return null;
        }

        public Sprite RandomDust()
        {
            if (dustSprites == null || dustSprites.Length == 0) return null;
            return dustSprites[UnityEngine.Random.Range(0, dustSprites.Length)];
        }
    }
}
