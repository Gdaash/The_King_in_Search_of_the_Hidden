using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace DeskCat.FindIt.Scripts.Core.Main.Character
{
    /// <summary>
    /// Состояния скрытого объекта
    /// </summary>
    public class HiddenObjsStates : MonoBehaviour
    {
        [SerializeField] private List<CharacterStateInformation> _characterStateInformations;
        [SerializeField] private Transform _target;
        [SerializeField] private Animator _animator;
        
        private Tween _currentTween;
        private CharacterStateInformation _currentState;
        
        private void Start()
        {
            NextState();
        }
        
        private void NextState()
        {
            if(!_target.gameObject.activeSelf) return; 
            
            if (_characterStateInformations is null || _characterStateInformations.Count == 0) return;

            if (_currentState is null)
            {
                _currentState = _characterStateInformations[0];
            }
            else
            {
                int index = _characterStateInformations.IndexOf(_currentState);
                index++;
                if (index >= _characterStateInformations.Count) index = 0;

                _currentState = _characterStateInformations[index];
            }
            StartHiddenObjsStates(_currentState);

            if (_animator != null)
            {
                if (!string.IsNullOrEmpty(_currentState.Animation))
                {
                    if(!_animator.enabled)
                        _animator.enabled = true;
                
                    _animator?.Play(_currentState.Animation);
                }
                else
                {
                    if(_animator.enabled)
                        _animator.enabled = false;
                }
            }
        }

        private void StartHiddenObjsStates(CharacterStateInformation  characterStateInformation)
        {
            _target.localScale = characterStateInformation.Scale;
            
            switch (characterStateInformation.CharacterState)
            {
                case CharacterState.Idle:
                    _currentTween = Tween.Delay(
                        characterStateInformation.Time,
                        () => { NextState(); });
                    break;
                case CharacterState.Moving:
                    _currentTween = Tween.Position(
                            _target,
                            characterStateInformation.Target.position,
                            characterStateInformation.Time,
                            ease: characterStateInformation.Curve)
                            .OnComplete(() => { NextState(); });
                    break;
                default: break;
            }
        }
    }
}