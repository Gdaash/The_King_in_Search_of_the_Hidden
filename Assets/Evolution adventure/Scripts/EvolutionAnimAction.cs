using System;
using PrimeTween;
using UnityEngine;

namespace Evolution_adventure.Scripts
{
    public class EvolutionAnimAction : MonoBehaviour
    {
        [SerializeField] private GameObject _old;
        [SerializeField] private GameObject _evolved;
        
        [SerializeField] Animator _animator;
        
        private bool _isAnimPlayed = false;


        public void PlayEvolutionAnim()
        {
            if(_isAnimPlayed)
                return;
            
            
            _isAnimPlayed = true;
            _animator.SetBool("Start", true);
        }

        public void EnableEvolved()
        {
            _old.SetActive(false);
            _evolved.SetActive(true);
        }
        
    }
}