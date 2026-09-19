using System;
using UnityEngine;
using UnityEngine.Events;

namespace DeskCat.FindIt.Scripts.Core.Model
{
    [Serializable]
    public class CustomUnityEvent
    {
        [Tooltip("Waiting Time Before Click Event Happen")]
        public float WaitTimer = 0f;

        [Tooltip("Waiting Time After Click Event Happen")]
        public float DelayTimer = 0f;

        [SerializeField] public UnityEvent OnClickEventList;
    }
}