using UnityEngine;

public interface IEnemyAI 
{ 
    // Получить текущую цель (игрок или база)
    Transform GetTarget(); 

    // Завершить фазу атаки и сбросить кулдаун
    void FinishAttack(); 

    // Реакция на получение урона
    void OnTakeDamage(Transform attacker);
}