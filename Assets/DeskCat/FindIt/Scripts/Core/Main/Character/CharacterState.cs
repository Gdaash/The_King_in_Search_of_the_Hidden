using UnityEngine;

namespace DeskCat.FindIt.Scripts.Core.Main.Character
{
    /// <summary>
    /// Состояние игрока
    /// От этой мутатени зависит название анимации, точнее ее состояния
    /// в машине состояний аниматора
    /// </summary>
    public enum CharacterState
    {
        /// <summary>
        /// Стоит смиренно ждет
        /// </summary>
        Idle,
        /// <summary>
        /// Пиздует нахрен куда то!
        /// </summary>
        Moving,
    }
}