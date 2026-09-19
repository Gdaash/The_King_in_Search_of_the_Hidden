using UnityEngine;
using System;

namespace DeskCat.FindIt.Scripts.Core.Main.Character
{
    /// <summary>
    /// Информация о состоянии, тут всякая хрень и каша
    /// Но для этого проекта пойдет
    /// </summary>
    [Serializable]
    public class CharacterStateInformation
    {
        /// <summary>
        /// Его состояние, влияет на анимацию
        /// </summary>
        [field: SerializeField] public CharacterState CharacterState { get; private set; }
        /// <summary>
        /// Время этой вакханалии
        /// </summary>
        [field:SerializeField] public float Time{ get; private set; }
        /// <summary>
        /// Ну тут кривая для твинов 
        /// </summary>
        [field:SerializeField] public AnimationCurve Curve{ get; private set; }
        /// <summary>
        /// Целевая точка куда должен упиздохать если должен
        /// </summary>
        [field:SerializeField] public Transform Target{ get; private set; }
        /// <summary>
        /// Скалирование объекта, тут чтоб башка смотрела куда надо
        /// </summary>
        [field:SerializeField] public Vector3 Scale{ get; private set; }
        /// <summary>
        /// Название анимации, если есть и если есть аниматор
        /// </summary>
        [field:SerializeField] public string Animation{ get; private set; }
    }
}