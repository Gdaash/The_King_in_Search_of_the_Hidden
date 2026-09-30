# Магическая библиотека и бестиарий

## Здание

- Кнопка `Magic Library` находится на `Base Panel` и использует обычный `BaseBuildingConstruction`.
- Идентификатор строительства: `magic_library`.
- Базовая цена: 1 дерево и 1 камень.
- Настраиваемые префабы: `Assets/Prefabs/Base/Magic Library Popup.prefab` и `Assets/Prefabs/Base/Bestiary Entry.prefab`.

## Открытие монстров

`BestiaryService` хранит в сохранении ячейки список имён вражеских префабов под ключом `bestiary.encountered`.

Монстр записывается при фактическом создании на World через `BestiaryService.RegisterSpawn`. Этот вызов подключён к волнам тревоги, `ObjectSpawner` и раскрытию гекса. Метод принимает только объекты с тегом `Enemy1` и компонентом `Health`, поэтому ресурсы и мирные юниты в бестиарий не попадут. Повторные появления не создают дубликаты.

## Данные и интерфейс

Описание, иконка и характеристики берутся из `Assets/Resources/UI/Unit Details/Enemy Description Catalog.asset`. Карточка при наведении использует тот же `Unit Description Tooltip`, что панель врагов на World. Для нового вражеского префаба запустите `Tools / Game Foundation / UI / Setup Enemy Roster`: инструмент создаст описание и UI-иконку, сохранив исходный боевой спрайт без изменений.

`Tools / Game Foundation / Base / Setup Magic Library` создаёт и обновляет префабы библиотеки и связывает её с `Base Screen HUD`.
