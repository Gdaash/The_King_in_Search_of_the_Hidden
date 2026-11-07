using System.Collections.Generic;
using GameFoundation.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    public sealed partial class WorldFlashlightAvailability
    {
        private readonly Dictionary<Vector2, SpriteRenderer> constructionMarkers = new();
        private readonly List<Vector2> markerSites = new();
        private readonly List<Vector2> staleMarkers = new();
        private readonly List<Vector2> constructionSites = new();
        private GameObject constructionMenu;
        private Vector2 menuSite;
        private bool demolishMenu;
        private readonly List<System.Action> constructionActions = new();
        private const float ConstructionButtonSpacing = 1.08f;
        private float nextConstructionRefresh;
        private Sprite constructionIcon, ballistaIcon;
        private Sprite constructionButton;
        private SpriteRenderer ballistaHead;

        private void RefreshConstructionMarkers()
        {
            if (Time.unscaledTime < nextConstructionRefresh) return;
            nextConstructionRefresh = Time.unscaledTime + .12f;
            if (military == null) military = FindFirstObjectByType<WorldMilitaryDeploymentController>();
            markerSites.Clear();
            if (military != null && !escaped && !ModalBlocksLightInput && !FirstRunLightTutorial.BlocksInput)
            {
                constructionIcon ??= Resources.Load<Sprite>("Engineering/Construction Icon");
                if (ballistaIcon == null)
                {
                    var prefab = Resources.Load<GameObject>("Engineering/Ballista");
                    ballistaIcon = prefab?.transform.Find("Graphics")?.GetComponent<SpriteRenderer>()?.sprite;
                    ballistaHead = prefab?.transform.Find("Rotating Head")?.GetComponent<SpriteRenderer>();
                }
                CollectBallistaSites(constructionSites, military.PortalPosition);
                foreach (var site in constructionSites)
                    if (military.CanOrderDefense(site)) markerSites.Add(site);
                foreach (var order in military.BallistaOrders) markerSites.Add(order);
                foreach (var order in military.DefenseTowers)
                    if (order.Value != null && !order.Value.Built) markerSites.Add(order.Key);
            }
            foreach (var point in markerSites)
            {
                if (!constructionMarkers.TryGetValue(point, out var marker) || marker == null)
                {
                    marker = new GameObject("Construction marker").AddComponent<SpriteRenderer>();
                    marker.transform.SetParent(transform, false);
                    marker.transform.position = point;
                    marker.sortingLayerName = "OverLight";
                    marker.sortingOrder = 120;
                    constructionMarkers[point] = marker;
                }
                bool ordered = military.HasBallistaOrder(point);
                Sprite preview = ordered ? ballistaIcon : null;
                foreach (var tower in military.DefenseTowers)
                    if (tower.Key == point && tower.Value != null)
                    {
                        ordered = true;
                        preview = tower.Value.Preview;
                        break;
                    }
                marker.sprite = preview != null ? preview : constructionIcon;
                marker.color = ordered ? new Color(.45f, .71f, 1f) : Color.white;
                marker.enabled = !IsDragging;
            }
            staleMarkers.Clear();
            foreach (var pair in constructionMarkers)
                if (!markerSites.Contains(pair.Key)) staleMarkers.Add(pair.Key);
            foreach (var point in staleMarkers)
            {
                if (constructionMarkers[point] != null) Destroy(constructionMarkers[point].gameObject);
                constructionMarkers.Remove(point);
            }
            foreach (var light in lights)
                if (light.beam != null && light.beam.Flag != null)
                    light.beam.Flag.SetConstructionMarkerVisible(!IsDragging && markerSites.Contains(light.position));
            if (constructionMenu != null && (ModalBlocksLightInput || military == null ||
                (demolishMenu ? !HasDefenseAt(menuSite) :
                    !military.CanOrderDefense(menuSite))))
                CloseConstructionMenu();
        }

        private bool HandleConstructionMenuInput()
        {
            if (constructionMenu == null) return false;
            if (ModalBlocksLightInput || Input.GetMouseButtonDown(1))
            {
                rightPointerStartedInWorld = false;
                CloseConstructionMenu();
                return true;
            }
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 point = worldCamera != null ? worldCamera.ScreenToWorldPoint(Input.mousePosition) : Vector3.zero;
                Vector2 delta = point - (Vector2)constructionMenu.transform.position;
                int index = Mathf.RoundToInt(delta.x / ConstructionButtonSpacing + (constructionActions.Count - 1) * .5f);
                if (worldCamera != null && worldCamera.pixelRect.Contains(Input.mousePosition) &&
                    !IsPointerOverBlockingUI(Input.mousePosition) && index >= 0 && index < constructionActions.Count &&
                    Mathf.Abs(delta.x - ConstructionButtonX(index, constructionActions.Count)) <= .5f && Mathf.Abs(delta.y) <= .5f)
                {
                    constructionActions[index]();
                    nextConstructionRefresh = 0;
                }
                CloseConstructionMenu();
                pointerStartedInWorld = false;
            }
            return true;
        }

        private bool TryOpenConstructionMenu(Vector2 point)
        {
            if (military == null || FirstRunLightTutorial.BlocksInput) return false;
            foreach (var tower in military.DefenseTowers)
                if ((tower.Key - point).sqrMagnitude < .48f * .48f)
                {
                    OpenConstructionMenu(tower.Key, true);
                    return true;
                }
            foreach (var ballista in military.BuiltBallistas)
                if (ballista != null && ballista.IsAlive &&
                    ((Vector2)ballista.transform.position - point).sqrMagnitude < .48f * .48f)
                {
                    OpenConstructionMenu(ballista.transform.position, true);
                    return true;
                }
            foreach (var pair in constructionMarkers)
                if (pair.Value != null && pair.Value.enabled && pair.Value.sprite != null &&
                    pair.Value.bounds.Contains(new Vector3(point.x, point.y, pair.Value.bounds.center.z)))
                {
                    bool ordered = military.HasBallistaOrder(pair.Key);
                    if (!ordered && !military.CanOrderDefense(pair.Key)) continue;
                    OpenConstructionMenu(pair.Key, ordered);
                    return true;
                }
            return false;
        }

        private bool HasDefenseAt(Vector2 site)
        {
            if (military.HasBallistaOrder(site) || military.BallistaAt(site) != null) return true;
            foreach (var tower in military.DefenseTowers)
                if (tower.Key == site) return true;
            return false;
        }

        private static float ConstructionButtonX(int index, int count) =>
            (index - (count - 1) * .5f) * ConstructionButtonSpacing;

        private void OpenConstructionMenu(Vector2 site, bool demolish)
        {
            CloseConstructionMenu();
            menuSite = site;
            demolishMenu = demolish;
            constructionMenu = new GameObject(demolish ? "Demolish defense" : "Build defense");
            constructionMenu.transform.SetParent(transform, false);
            constructionMenu.transform.position = site + Vector2.up * .92f;

            if (demolish)
            {
                constructionActions.Add(() => military.DemolishBallista(site));
                AddConstructionButton(0, PortalPopupView.Tr("world.construction.demolish", "Снести"), null);
                return;
            }

            bool canBuildBallista = military.HasLivingEngineer;
            int count = canBuildBallista ? 4 : 3;
            if (canBuildBallista)
            {
                constructionActions.Add(() => military.OrderBallista(site));
                AddConstructionButton(ConstructionButtonX(0, count),
                    PortalPopupView.Tr("world.construction.ballista", "Баллиста"), ballistaIcon, true);
            }

            AddTowerButton(DefenseTowerKind.Arrow, "world.construction.arrowTower", "Деревянная башня");
            AddTowerButton(DefenseTowerKind.Stone, "world.construction.stoneTower", "Каменная башня");
            AddTowerButton(DefenseTowerKind.Magic, "world.construction.magicTower", "Магическая башня");

            void AddTowerButton(DefenseTowerKind kind, string key, string fallback)
            {
                var sprite = DefenseTowerRequest.LoadPrefab(kind)?.GetComponentInChildren<SpriteRenderer>()?.sprite;
                AddConstructionButton(ConstructionButtonX(constructionActions.Count, count), PortalPopupView.Tr(key, fallback), sprite);
                constructionActions.Add(() => military.OrderDefenseTower(site, kind));
            }
        }

        private void AddConstructionButton(float x, string title, Sprite icon, bool ballista = false)
        {
            var button = new GameObject(title).transform;
            button.SetParent(constructionMenu.transform, false);
            button.localPosition = Vector3.right * x;
            constructionButton ??= Resources.Load<Sprite>("Engineering/Construction Button");
            var background = new GameObject("Button background", typeof(RectTransform), typeof(Canvas));
            background.transform.SetParent(button, false);
            background.transform.localScale = Vector3.one / 64f;
            var canvas = background.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "OverLight";
            canvas.sortingOrder = 121;
            var rect = (RectTransform)background.transform;
            rect.sizeDelta = new Vector2(64, 64);
            var frame = new GameObject("Standard button frame", typeof(RectTransform)).AddComponent<Image>();
            frame.transform.SetParent(rect, false);
            frame.rectTransform.anchorMin = Vector2.zero;
            frame.rectTransform.anchorMax = Vector2.one;
            frame.rectTransform.offsetMin = frame.rectTransform.offsetMax = Vector2.zero;
            frame.sprite = constructionButton;
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = false;
            if (icon != null) AddConstructionButtonSprite(button, icon, .08f, 122, ballista);
            if (ballista && ballistaHead != null)
                AddConstructionButtonSprite(button, ballistaHead.sprite, .08f + ballistaHead.transform.localPosition.y * .6f, 123, true);
            var label = new GameObject("Action label").AddComponent<TextMeshPro>();
            label.transform.SetParent(button, false);
            label.transform.localPosition = new Vector3(0, icon == null ? 0 : -.28f, 0);
            var font = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
            if (font != null) label.font = font;
            label.text = title;
            label.fontSize = 1.5f;
            label.enableAutoSizing = true;
            label.fontSizeMin = .8f;
            label.fontSizeMax = 1.5f;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(.94f, .35f);
            label.GetComponent<MeshRenderer>().sortingLayerName = "OverLight";
            label.GetComponent<MeshRenderer>().sortingOrder = 124;
        }

        private void AddConstructionButtonSprite(Transform button, Sprite sprite, float height, int sortingOrder, bool ballista)
        {
            if (sprite == null) return;
            var icon = new GameObject("Defense icon").AddComponent<SpriteRenderer>();
            icon.transform.SetParent(button, false);
            icon.transform.localPosition = new Vector3(0, height, 0);
            float scale = ballista ? .6f : .51f / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            icon.transform.localScale = new Vector3(ballista ? -scale : scale, scale, scale);
            icon.sprite = sprite;
            icon.sortingLayerName = "OverLight";
            icon.sortingOrder = sortingOrder;
        }

        private void CloseConstructionMenu()
        {
            if (constructionMenu != null) Destroy(constructionMenu);
            constructionMenu = null;
            constructionActions.Clear();
        }

        private void ClearConstructionInterface()
        {
            CloseConstructionMenu();
            foreach (var marker in constructionMarkers.Values)
                if (marker != null) Destroy(marker.gameObject);
            constructionMarkers.Clear();
        }
    }
}
