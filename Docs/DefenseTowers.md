# Башни обороны

Спрайты извлечены из `hex1 (10).psd` без изменения пикселей. Источник и индексы слоёв: `Assets/Sprites/World/Defense Towers/Source.json`. Повторное извлечение: `python Tools/extract_defense_towers_psd.py`.

Префабы: `Assets/Prefabs/Buildings/Defense Towers/`.
Статы: `Assets/Resources/Stats/Defense Towers/`.
Снаряды: `Assets/Prefabs/Projectiles/{Magic,Arrow,Stone}TowerProjectile.prefab`.

| Башня | Урон | Тип | Дальность | Интервал |
|---|---:|---|---:|---:|
| Magic Tower | 12 | Magic | 8 | 1,2 с |
| Arrow Tower | 3 | Physical | 9 | 0,22 с |
| Stone Tower | 45 | Physical | 4 | 3,5 с |

Здоровье каждой — 200. Эти значения являются начальной настройкой баланса и редактируются в отдельных GlobalStats. Стрельба использует ArcherTower, TowerVisuals и EnemyProjectile, как у PortalTower. Башни дружеские (Player), атакуют Enemy1. Не зависят от исследования магических стрел портала. В префабе TowerVisuals можно менять точку выстрела, подготовку выстрела и отдачу; у снаряда EnemyProjectile — скорость и время жизни.

Графика: 32 PPU, Point, без сжатия; масштаб SpriteRenderer = 1. У магической башни отдельный исходный магический снаряд. Камень вырезан из слоя камней камнеметательной башни, стрела взята из существующего спрайта Arrow.

Префабы можно поставить на сцену или добавить в Prefabs For Group у HexManager. Стартовая последовательность гексов и цены строительства не изменены.

Проверка: DefenseTowersValidation.Run() через Unity Editor. Проверяет реальные попадания и урон, частоту, дальность, очистку цели и однократное попадание при перекрывающихся коллайдерах. Использует отдельную временную сцену и не меняет сохранения игрока.
