using System;
using System.Collections;
using System.Collections.Generic;
using DeskCat.FindIt.Scripts.Core.Main.Utility.ClickedFunction;
using DeskCat.FindIt.Scripts.Core.Model;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeskCat.FindIt.Scripts.Core.Main.System
{
    public class HiddenObj : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Decide This Object Is Click To Found Or Drag To Specified Region")]
        public HiddenObjFoundType hiddenObjFoundType = HiddenObjFoundType.Click;

        [Tooltip("Sprite To Display In UI Panel, Leave Empty To Use The Current Game Object Sprite")]
        public Sprite UISprite;

        [Tooltip("Game Object Will Be Invisible When Start Scene")]
        public bool HideOnStart;

        [Tooltip("Tooltips or Hint Display When Click On The UI Object")]
        public bool EnableTooltip;

        public TooltipsType TooltipsType;

        [NonReorderable] public List<MultiLanguageTextListModel> Tooltips;

        [Tooltip("Use Background Animation When Clicked")]
        public bool EnableBGAnimation;

        public GameObject BGAnimationPrefab;
        public Transform BgAnimationTransform;

        [Tooltip("Use Particle Fx When Clicked")]
        public bool EnableParticleFx;
        public GameObject ParticleFxPrefab;

        [Tooltip("Lifespan Of Particle Fx, -1 For Infinite")]
        public float ParticleFxLifespan = 2f;

        [Tooltip("Hide The Object When Found")]
        public bool HideWhenFound = true;

        [Tooltip("Play Sound Effect When Found")]
        public bool PlaySoundWhenFound = true;

        public AudioClip AudioWhenClick;

        [Tooltip("Action When Target Is Clicked")]
        public Action TargetClickAction;

        [Tooltip("Action When Target Is Drag To The Specified Region")]
        public Action DragRegionAction;

        [HideInInspector] public bool IsFound;

        public bool baseInfoBool = true;
        public bool tooltipsBool = true;
        public bool bgAnimBool = true;
        public bool particleFxBool = true;
        public bool actionFoldoutBool = true;
        public bool isClickActionTrigger = true;
        
        [HideInInspector] 
        public bool isAbleToClick = true;
        private void Awake()
        {
            if (UISprite == null)
            {
                if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                {
                    UISprite = spriteRenderer.sprite;
                }
            }

            if (HideOnStart)
            {
                gameObject.SetActive(false);
            }
        }

        // Handle mouse button down events and avoid processing clicks on UI
        public void OnMouseDown()
        {
            if (!isAbleToClick) return;
            if (hiddenObjFoundType != HiddenObjFoundType.Click) return; // Only allow click for Click type
            HitHiddenObject();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isAbleToClick) return;
            if (hiddenObjFoundType != HiddenObjFoundType.Click) return; // Only allow click for Click type
            HitHiddenObject();
        }

        public void SetItemVisibilityWhenRandom(bool isShow)
        {
            if (HideOnStart)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(isShow);
        }

        // Toggle the visibility of the object
        public void ToggleItem()
        {
            if (IsFound)
            {
                return;
            }

            gameObject.SetActive(!gameObject.activeSelf);
        }

        // Use this function for custom click events
        public void HitHiddenObj()
        {
            isClickActionTrigger = true;
            HitHiddenObject();
        }

        // Core logic for handling "hidden objects"
        private void HitHiddenObject()
        {
            if (!isClickActionTrigger) return;

            if (AudioWhenClick != null)
            {
                LevelManager.PlayItemFx(AudioWhenClick);
            }

            if (EnableBGAnimation)
            {
                BgAnimationTransform.gameObject.SetActive(true);
                IsFound = true;
            }

            if (EnableParticleFx && ParticleFxPrefab != null)
            {
                var instance = Instantiate(ParticleFxPrefab, transform.position, Quaternion.identity, transform.parent);
                if (ParticleFxLifespan >= 0f)
                {
                    Destroy(instance, ParticleFxLifespan);
                }
                IsFound = true;
            }

            if (HideWhenFound)
            {
                gameObject.SetActive(false);
                IsFound = true;
            }

            TargetClickAction?.Invoke();
        }

        
    }
}