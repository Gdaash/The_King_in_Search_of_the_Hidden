using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Runs real logistics components in an isolated Play Mode scene, restoring save envelopes afterwards.
[InitializeOnLoad]
public static class LogisticsCancellationAudit
{
    const string Key = "LogisticsCancellationAudit";
    const string ScenePath = "Assets/LogisticsAudit.unity";
    static readonly string Report = "Temp/LogisticsCancellationAudit.txt";
    static GlobalResourceManager resources;
    static Warehouse warehouse;
    static OrderManager orders;
    static ResourceType human, cart, berry;
    static LogisticFlag flag;
    static int checks;

    static LogisticsCancellationAudit() { EditorApplication.playModeStateChanged += Changed; }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first.");
        Directory.CreateDirectory("Temp"); File.WriteAllText(Report, "Logistics audit started\n");
        for (int i = 1; i <= 3; i++)
        {
            string k = "foundation.slot." + i + ".saveData";
            SessionState.SetBool(Key + k, PlayerPrefs.HasKey(k));
            SessionState.SetString(Key + k, PlayerPrefs.GetString(k, ""));
        }
        SessionState.SetString(Key + "startScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        var active = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(active);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
    }

    public static void RunForaging() { SessionState.SetBool(Key + "foraging", true); Run(); }
    public static void RunAnimals()
    {
        SessionState.SetBool(Key + "animals", true);
        Run();
    }

    public static void RunPortal()
    {
        SessionState.SetBool(Key + "portal", true);
        Run();
    }

    public static void RunResourceVisuals()
    {
        SessionState.SetBool(Key + "resourceVisuals", true);
        Run();
    }

    public static void RunRecoil()
    {
        SessionState.SetBool(Key + "recoil", true);
        Run();
    }
    public static void RunQuests()
    {
        SessionState.SetBool(Key + "quests", true);
        Run();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) RunChecks();
        if (state != PlayModeStateChange.EnteredEditMode) return;
        for (int i = 1; i <= 3; i++)
        {
            string k = "foundation.slot." + i + ".saveData";
            if (SessionState.GetBool(Key + k, false)) PlayerPrefs.SetString(k, SessionState.GetString(Key + k, ""));
            else PlayerPrefs.DeleteKey(k);
        }
        PlayerPrefs.Save();
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key + "startScene", ""));
        AssetDatabase.DeleteAsset(ScenePath); SessionState.SetBool(Key, false);
        File.AppendAllText(Report, "Save envelopes restored; original editor scenes preserved.\n");
    }

    static void Set(Object target, string field, Object value)
    { var s = new SerializedObject(target); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    static int Amount(ResourceType t) => resources.GetResourceAmount(t);
    static void Check(bool ok, string label)
    { if (!ok) throw new Exception(label); checks++; File.AppendAllText(Report, "PASS " + label + "\n"); }
    static async Task Until(Func<bool> condition, string label)
    {
        double end = EditorApplication.timeSinceStartup + 12;
        while (!condition()) { if (EditorApplication.timeSinceStartup > end) throw new Exception("Timeout: " + label); await Task.Delay(30); }
    }
    static ResourceRequester Job(int berries, int people = 0)
    {
        var go = new GameObject("Audit building"); go.transform.position = new Vector3(5, 0, 0);
        var job = go.AddComponent<ResourceRequester>();
        job.requirements.Add(new ResourceRequirement { resourceType = berry, requiredAmount = berries });
        if (people > 0) job.requirements.Add(new ResourceRequirement { resourceType = human, requiredAmount = people });
        flag.SetCrystalTarget(job); job.SetCrystalFlag(flag); return job;
    }
    static async void RunChecks()
    {
        try
        {
            if (SessionState.GetBool(Key + "foraging", false)) { SessionState.SetBool(Key + "foraging", false); await ForestForagingAudit.RunPlay(); return; }
            if (SessionState.GetBool(Key + "animals", false))
            {
                SessionState.SetBool(Key + "animals", false);
                await AnimalFoodAudit.RunPlay();
                return;
            }
            if (SessionState.GetBool(Key + "portal", false))
            {
                SessionState.SetBool(Key + "portal", false);
                await CheckPortal();
                return;
            }
            if (SessionState.GetBool(Key + "quests", false))
            {
                SessionState.SetBool(Key + "quests", false);
                await CheckQuests();
                File.AppendAllText(Report, "QUESTS PASSED: " + checks + " checks\n");
                return;
            }
            if (SessionState.GetBool(Key + "recoil", false))
            {
                SessionState.SetBool(Key + "recoil", false);
                await CheckCombatRecoil();
                File.AppendAllText(Report, "COMBAT RECOIL PASSED: " + checks + " checks\n");
                return;
            }
            human = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Human.asset");
            cart = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Cart.asset");
            berry = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Berry.asset");
            resources = new GameObject("Audit inventory").AddComponent<GlobalResourceManager>();
            resources.SetResourceAmount(human, 10); resources.SetResourceAmount(cart, 10); resources.SetResourceAmount(berry, 20);
            warehouse = new GameObject("Audit portal").AddComponent<Warehouse>();
            Set(warehouse, "humanResourceType", human); Set(warehouse, "cartResourceType", cart);
            Set(warehouse, "humanPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/Human.prefab"));
            Set(warehouse, "porterPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/Porter.prefab"));
            orders = new GameObject("Audit orders").AddComponent<OrderManager>(); orders.enabled = false;
            flag = new GameObject("Audit flag").AddComponent<LogisticFlag>();
            await Task.Delay(100);
            if (SessionState.GetBool(Key + "resourceVisuals", false))
            {
                SessionState.SetBool(Key + "resourceVisuals", false);
                await CheckResourceVisuals();
                File.AppendAllText(Report, "RESOURCE VISUALS PASSED: " + checks + " checks\n");
                return;
            }
            await CheckNotifications();
            for (int cycle = 0; cycle < 5; cycle++)
            {
                var job = Job(2);
                var p = warehouse.SpawnPorter(); p.AssignWarehouseTask(job, berry);
                if (cycle == 0) job.SetCrystalFlag(null); // before pickup
                else { await Until(() => p.IsCarryingResource(), "pickup"); await Task.Delay(150); job.SetCrystalFlag(null); }
                job.SetCrystalFlag(null); // repeated cancel
                Check(p.GetCurrentJob() == null, "cancel detaches porter " + cycle);
                await Until(() => p == null, "porter return");
                Check(Amount(human) == 10 && Amount(cart) == 10 && Amount(berry) == 20, "stock conserved after cancel " + cycle);
                Check(job.requirements[0].currentAmount == 0 && job.requirements[0].reservedAmount == 0, "cancelled building received nothing " + cycle);
                Object.Destroy(job.gameObject); await Task.Delay(50);
            }
            // Successful delivery after cancellation, and cancel after the delivery.
            var delivered = Job(2); var porter = warehouse.SpawnPorter(); porter.AssignWarehouseTask(delivered, berry);
            await Until(() => delivered.requirements[0].currentAmount == 1, "delivery");
            delivered.SetCrystalFlag(null); await Until(() => porter == null, "empty porter return");
            Check(Amount(berry) == 19 && delivered.requirements[0].currentAmount == 1, "delivered resource counted exactly once");
            Check(Amount(human) == 10 && Amount(cart) == 10, "successful delivery returns human and cart");
            Object.Destroy(delivered.gameObject); resources.SetResourceAmount(berry, 20);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                var job = Job(1, 1); var person = warehouse.SpawnHumanForJob(job, human);
                await Task.Delay(200); job.SetCrystalFlag(null);
                await Until(() => person == null, "human return");
                Check(Amount(human) == 10 && job.requirements[1].currentAmount == 0, "human cancellation conserved " + cycle);
                Object.Destroy(job.gameObject); await Task.Delay(50);
            }
            var occupied = Job(1, 1); var worker = warehouse.SpawnHumanForJob(occupied, human);
            await Until(() => worker == null, "worker reaches building");
            Check(occupied.requirements[1].currentAmount == 1 && Amount(human) == 9, "worker accounted inside building");
            occupied.SetCrystalFlag(null); Check(occupied.RecallIdleHumans() == 1 && occupied.RecallIdleHumans() == 0, "repeat recall cannot duplicate worker");
            await Until(() => Amount(human) == 10, "recalled worker arrives");
            Object.Destroy(occupied.gameObject);
            // Real scheduler must reserve only once per assigned porter.
            var scheduled = Job(2); orders.SendMessage("DistributeOrders");
            int assigned = 0; foreach(var p in Object.FindObjectsByType<Porter>(FindObjectsSortMode.None)) if(p.GetCurrentJob()==scheduled) assigned++;
            Check(scheduled.requirements[0].reservedAmount == assigned, "scheduler reservation equals assigned porters");
            scheduled.SetCrystalFlag(null);
            await Until(() => Object.FindObjectsByType<Porter>(FindObjectsSortMode.None).Length == 0, "scheduled porters return");
            Check(Amount(human)==10 && Amount(cart)==10 && Amount(berry)==20, "scheduler cancellation conserves stocks");
            Object.Destroy(scheduled.gameObject); await Task.Delay(50);
            var duplicate = warehouse.SpawnPorter();
            warehouse.DespawnPorter(duplicate); warehouse.DespawnPorter(duplicate);
            Check(Amount(human)==10 && Amount(cart)==10, "same-frame duplicate return cannot credit twice");
            await Task.Delay(50);
            var restart = Job(2, 1); var restartingHuman = warehouse.SpawnHumanForJob(restart, human);
            restart.SetCrystalFlag(null); restart.SetCrystalFlag(flag);
            await Task.Delay(100);
            Check(restartingHuman==null || (restartingHuman.GetCurrentJob()==null && restartingHuman.IsReturningToWarehouse()), "same-frame cancel and restart detaches old human");
            await Until(()=>restartingHuman==null, "restart human returns");
            Object.Destroy(restart.gameObject);
            await Task.Delay(50);
            // Two porters contend for the last berry: only one may withdraw it.
            resources.SetResourceAmount(berry, 1);
            var scarce = Job(3); var first = warehouse.SpawnPorter(); var second = warehouse.SpawnPorter();
            first.AssignWarehouseTask(scarce, berry); second.AssignWarehouseTask(scarce, berry);
            await Until(()=>first==null && second==null, "last-resource contenders return");
            Check(Amount(berry)==0 && scarce.requirements[0].currentAmount==1, "last resource spent exactly once across two porters");
            Check(Amount(human)==10 && Amount(cart)==10, "failed pickup returns person and cart");
            Object.Destroy(scarce.gameObject); resources.SetResourceAmount(berry,20); await Task.Delay(50);
            // Cancel a cargo collected on the map rather than withdrawn from inventory.
            var mapJob=Job(2); var itemGo=new GameObject("Audit loose berry"); itemGo.transform.position=new Vector3(2,0,0);
            var item=itemGo.AddComponent<ResourceItem>(); item.type=berry;
            var gatherer=warehouse.SpawnPorter(); gatherer.AssignTask(mapJob,item); item.isReserved=true;
            await Until(()=>gatherer.IsCarryingResource(),"map pickup"); mapJob.SetCrystalFlag(null);
            await Until(()=>gatherer==null,"map cargo return");
            Check(Amount(berry)==21 && item==null && mapJob.requirements[0].currentAmount==0,"map cargo returned once and original item removed");
            Check(Amount(human)==10 && Amount(cart)==10,"map gathering returns people and carts");
            Object.Destroy(mapJob.gameObject); resources.SetResourceAmount(berry,20);
            // A second return callback must not credit the same human twice.
            var duplicateJob=Job(2,1); var duplicateHuman=warehouse.SpawnHumanForJob(duplicateJob,human);
            duplicateHuman.ReturnToPortal(); warehouse.ReturnHuman(duplicateHuman); warehouse.ReturnHuman(duplicateHuman);
            Check(Amount(human)==10,"duplicate human callback credits once");
            await Until(()=>duplicateHuman==null,"duplicate human cleanup");
            Check(Amount(human)==10,"arrival after duplicate callback does not credit again");
            Object.Destroy(duplicateJob.gameObject);
            File.AppendAllText(Report, "ALL PASSED: " + checks + " checks\n");
        }
        catch(Exception e) { File.AppendAllText(Report, "FAIL " + e + "\n"); }
        finally { EditorApplication.ExitPlaymode(); }
    }

    static async Task CheckPortal()
    {
        // Reproduce a cold catalog: preloaded assets are not guaranteed to be loaded in Editor Play Mode.
        var oldCatalog = ProjectReferences.Instance;
        if (oldCatalog != null) Resources.UnloadAsset(oldCatalog);
        Check(ProjectReferences.Instance == null, "catalog absent before scene load");
        GameFoundation.Saves.SaveSlotPrefs.DeleteKey("foundation.daycycle");
        SceneManager.LoadScene("Base");
        await Task.Delay(1200);
        Check(ProjectReferences.Instance != null, "scene loads its catalog dependency");
        var unload = Resources.UnloadUnusedAssets();
        await Until(() => unload.isDone, "unused asset cleanup");
        Check(ProjectReferences.Instance != null, "catalog survives unused asset cleanup");
        var day=GameFoundation.MetaProgression.DayCycleService.Instance;
        var router=Object.FindFirstObjectByType<GameFoundation.MetaProgression.RunSceneRouter>();
        File.AppendAllText(Report,"BASE refs="+(ProjectReferences.Instance!=null)+" sites="+day.Portals.Count+" router="+(router!=null)+" entered="+day.EnteredToday+"\n");
        var views=Object.FindObjectsByType<GameFoundation.Base.PortalPopupView>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        GameFoundation.Base.PortalPopupView view=null;
        foreach(var v in views)if(v.transform.parent.gameObject.activeInHierarchy){view=v;break;}
        Check(view!=null,"portal popup in active canvas");
        view.gameObject.SetActive(true);await Task.Delay(100);
        view.Select(view.locations[0].location);
        File.AppendAllText(Report,"SELECTED="+view.Selected.LocationId+" enabled="+view.travelButton.interactable+"\n");
        Check(view.travelButton.interactable,"travel available");
        Canvas.ForceUpdateCanvases();
        var canvas=view.travelButton.GetComponentInParent<Canvas>();
        var position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,view.travelButton.transform.position);
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=position,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);
        foreach(var hit in hits)File.AppendAllText(Report,"RAYCAST "+hit.gameObject.name+" parent="+hit.gameObject.transform.parent.name+"\n");
        Check(hits.Count>0 && UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject)==view.travelButton.gameObject,"travel button receives pointer click");
        UnityEngine.EventSystems.ExecuteEvents.Execute(view.travelButton.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        await Task.Delay(1200);
        Check(SceneManager.GetActiveScene().name=="World","travel button loads World");
        Object.FindFirstObjectByType<GameFoundation.MetaProgression.RunSceneRouter>().ReturnToBase();
        await Task.Delay(1200);
        day.NextDay();
        views=Object.FindObjectsByType<GameFoundation.Base.PortalPopupView>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var v in views)if(v.transform.parent.gameObject.activeInHierarchy){view=v;break;}
        view.gameObject.SetActive(true);await Task.Delay(100);
        Check(view.travelButton.interactable,"travel available after return and next day");
        view.travelButton.onClick.Invoke();await Task.Delay(1200);
        Check(SceneManager.GetActiveScene().name=="World","second expedition loads World");
        File.AppendAllText(Report,"PORTAL PASSED\n");
    }

    static async Task CheckResourceVisuals()
    {
        resources.SetResourceAmount(human, 40); resources.SetResourceAmount(cart, 40);
        foreach (var id in new[]{"Berry","Wood","Stone","MagicOre","IronOre","Sword","Bow"})
        {
            berry = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/"+id+".asset");
            resources.SetResourceAmount(berry, 10);
            for(int source=0;source<2;source++)
            {
                var job=Job(1); var p=warehouse.SpawnPorter();
                ResourceItem item=null;
                if(source==0)p.AssignWarehouseTask(job,berry);
                else
                {
                    item=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map Resources/D_"+id+".prefab"),p.transform.position,Quaternion.identity).GetComponent<ResourceItem>();
                    Check(item.GetComponent<SpriteRenderer>().sprite==berry.groundSprite,id+" ground outline sprite");
                    p.AssignTask(job,item);
                }
                await Until(()=>p.IsCarryingResource(),id+" pickup");
                var cargo=(SpriteRenderer)new SerializedObject(p).FindProperty("carrySlotRenderer").objectReferenceValue;
                Check(cargo.sprite==berry.defaultCarrySprite,id+" cargo source="+source);
                var body=cargo.transform.parent.GetComponent<SpriteRenderer>();
                Check(body!=null,id+" cargo follows body");
                foreach(float sign in new[]{-1f,1f})
                {
                    p.transform.localScale=new Vector3(sign,1,1);
                    var local=body.transform.InverseTransformPoint(cargo.transform.position)*32;
                    Check(Mathf.Abs(local.x-(18-body.sprite.pivot.x))<.001f && Mathf.Abs(local.y-(17-body.sprite.pivot.y))<.001f,id+" cart alignment direction="+sign);
                }
                Object.Destroy(p.gameObject);Object.Destroy(job.gameObject);if(item)Object.Destroy(item.gameObject);
                await Task.Delay(50);
            }
        }
    }

    static async Task CheckNotifications()
    {
        var canvas = new GameObject("Audit notification canvas", typeof(Canvas));
        var feed = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/HUD/Notification Feed.prefab"), canvas.transform)
            .GetComponent<GameFoundation.UI.NotificationFeed>();
        Action spawn = () =>
        {
            using var scope = GameFoundation.UI.GameNotifications.BeginPorterSpawn();
            GameFoundation.UI.GameNotifications.Resource(human, -1);
            GameFoundation.UI.GameNotifications.Resource(cart, -1);
        };
        Func<int> rowCount = () => feed.GetComponentsInChildren<GameFoundation.UI.NotificationRow>().Length;
        spawn(); Check(rowCount()==1,"porter costs produce one notification immediately");
        await Task.Delay(650); spawn();
        GameFoundation.UI.GameNotifications.Post("Unrelated event");
        await Task.Delay(650); spawn();
        Check(rowCount()==2,"rolling one-second chain merges across unrelated event");
        int totals=0, icons=0;
        foreach(var text in feed.GetComponentsInChildren<UnityEngine.UI.Text>()) if(text.text=="-3") totals++;
        foreach(var icon in feed.GetComponentsInChildren<UnityEngine.UI.Image>())
            if(icon.sprite==human.resourceIcon || icon.sprite==cart.resourceIcon)
            { icons++; Check(icon.rectTransform.sizeDelta==icon.sprite.rect.size*2,"resource notification icon uses x2 scale"); }
        Check(totals==2 && icons==2,"human and cart totals are -3 without duplicate visuals");
        await Task.Delay(1100); spawn();
        Check(rowCount()==3,"gap over one second starts new notification");
        GameFoundation.UI.GameNotifications.Resource(berry, 2);
        await Task.Delay(650);
        GameFoundation.UI.GameNotifications.Resource(berry, 3);
        GameFoundation.UI.GameNotifications.Resource(cart, 1);
        await Task.Delay(650);
        GameFoundation.UI.GameNotifications.Resource(berry, 4);
        GameFoundation.UI.GameNotifications.Resource(berry, -2);
        Check(rowCount()==4,"all resource changes share a rolling one-second row");
        bool gain=false, spend=false;
        foreach(var text in feed.GetComponentsInChildren<UnityEngine.UI.Text>())
        { if(text.text=="+9")gain=true; if(text.text=="-2")spend=true; }
        Check(gain && spend,"resource gains sum while spending remains visible");
        using(GameFoundation.UI.GameNotifications.BeginAction())
        {
            GameFoundation.UI.GameNotifications.Resource(berry,-1);
            GameFoundation.UI.GameNotifications.Post("Named action");
        }
        Check(rowCount()==5,"named action keeps its own resource costs");
        await Task.Delay(1100);
        GameFoundation.UI.GameNotifications.Resource(berry,1);
        Check(rowCount()==6,"resource chain restarts after one-second gap");
        Object.Destroy(canvas); await Task.Delay(50);
    }

    static async Task CheckCombatRecoil()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/CursedMage.prefab");
        for (int trial = 0; trial < 9; trial++)
        {
            var target = new GameObject("Combat audit target"); target.tag = "Player";
            float angle = trial % 8 * 45f;
            target.transform.position = Quaternion.Euler(0, 0, angle) * Vector3.right * 2.5f;
            var health = target.AddComponent<Health>(); target.AddComponent<BoxCollider2D>().size = Vector2.one * .3f;
            var mage = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            var ai = mage.GetComponent<EnemyAI_Ranged>();
            var visual = mage.GetComponent<EnemyVisuals_Ranged>();
            var sprite = (Transform)new SerializedObject(visual).FindProperty("spriteParent").objectReferenceValue;
            var baseline = sprite.localPosition;
            var scale = sprite.localScale;
            EnemyProjectile projectile = null;
            await Until(() => { projectile = Object.FindFirstObjectByType<EnemyProjectile>(); return projectile != null; }, "natural AI shot " + trial);
            Vector3 direction = projectile.transform.right;
            if (trial == 8) target.transform.position = -target.transform.position;
            int samples = 0; float peak = 0;
            double timeout = EditorApplication.timeSinceStartup + 4;
            do
            {
                Vector3 displacement = sprite.position - sprite.parent.TransformPoint(baseline);
                peak = Mathf.Max(peak, displacement.magnitude);
                if (displacement.magnitude > .002f)
                {
                    if (Vector3.Dot(displacement, direction) > .001f || Vector3.Cross(displacement, direction).magnitude > .002f)
                        throw new Exception("Combat recoil changed direction, trial " + trial + ": " + displacement);
                    samples++;
                }
                if (EditorApplication.timeSinceStartup > timeout) throw new Exception("Attack did not finish");
                await Task.Delay(10);
            } while (ai.GetIsAttacking());
            Check(samples > 0 && peak > .1f, "natural shot recoil sampled, angle " + angle + (trial == 8 ? " with target crossing" : ""));
            Check(Vector3.Distance(sprite.localPosition, baseline) < .002f && Vector3.Distance(sprite.localScale, scale) < .002f,
                "sprite returns to original pose, trial " + trial);
            File.AppendAllText(Report, "  recoil samples=" + samples + " peak=" + peak.ToString("F3") + "\n");
            Object.Destroy(mage); Object.Destroy(target);
            foreach (var p in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)) Object.Destroy(p.gameObject);
            await Task.Delay(100);
        }
    }

    static async Task CheckQuests()
    {
        var quest=AssetDatabase.LoadAssetAtPath<GameFoundation.Quests.QuestDefinition>(QuestSetup.QuestPath);
        var catalog=AssetDatabase.LoadAssetAtPath<GameFoundation.Quests.QuestCatalog>(QuestSetup.CatalogPath);
        string key="foundation.quest."+quest.id+".completed";
        GameFoundation.Saves.SaveSlotPrefs.DeleteKey(key);
        var manager=new GameObject("Quest test inventory").AddComponent<GlobalResourceManager>();
        foreach(var requirement in quest.requirements) manager.SetResourceAmount(requirement.resource,0);
        var tracker=manager.gameObject.AddComponent<GameFoundation.Quests.QuestTracker>();Set(tracker,"catalog",catalog);
        var canvas=new GameObject("Quest test UI",typeof(Canvas));
        var panel=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(QuestSetup.PanelPath),canvas.transform).GetComponent<GameFoundation.Quests.QuestPanel>();
        await Task.Delay(100);
        Check(!GameFoundation.Quests.QuestProgress.IsComplete(quest),"quest starts incomplete at zero stock");
        Check(panel.GetComponentsInChildren<GameFoundation.Quests.QuestObjectiveRow>().Length==6,"three compact and three detailed objectives authored");
        Canvas.ForceUpdateCanvases();
        float collapsedHeight=((RectTransform)panel.transform).rect.height;
        var events=new GameObject("Quest pointer test",typeof(UnityEngine.EventSystems.EventSystem)).GetComponent<UnityEngine.EventSystems.EventSystem>();
        var pointer=new UnityEngine.EventSystems.PointerEventData(events);
        Time.timeScale=0;
        UnityEngine.EventSystems.ExecuteEvents.Execute(panel.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
        await Task.Delay(400);Canvas.ForceUpdateCanvases();
        Check(((RectTransform)panel.transform).rect.height>collapsedHeight+150,"hover expands details even while paused");
        UnityEngine.EventSystems.ExecuteEvents.Execute(panel.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerExitHandler);
        await Task.Delay(400);Canvas.ForceUpdateCanvases();
        Check(Mathf.Abs(((RectTransform)panel.transform).rect.height-collapsedHeight)<1,"pointer exit restores compact height");
        Time.timeScale=1;Object.Destroy(events.gameObject);
        foreach(var requirement in quest.requirements) manager.SetResourceAmount(requirement.resource,requirement.amount-1);
        Check(!GameFoundation.Quests.QuestProgress.IsComplete(quest),"partial stock cannot complete quest");
        panel.gameObject.SetActive(false);
        foreach(var requirement in quest.requirements) manager.AddResource(requirement.resource,1);
        Check(GameFoundation.Quests.QuestProgress.IsComplete(quest),"quest completes independently of hidden UI");
        Check(GameFoundation.Saves.SaveSlotPrefs.GetInt(key,0)==1,"completion stored in active save slot");
        foreach(var requirement in quest.requirements)manager.TrySpendResource(requirement.resource,requirement.amount);
        Check(GameFoundation.Quests.QuestProgress.IsComplete(quest),"spending after completion does not reset quest");
        panel.gameObject.SetActive(true);await Task.Delay(50);
        bool completed=false;foreach(var t in panel.GetComponentsInChildren<UnityEngine.UI.Text>())if(t.text=="Р вЂ”Р В°Р Т‘Р В°Р Р…Р С‘Р Вµ Р Р†РЎвЂ№Р С—Р С•Р В»Р Р…Р ВµР Р…Р С•")completed=true;
        Check(completed,"reenabled panel displays saved completion");
        Object.Destroy(panel.gameObject);await Task.Delay(50);
        panel=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(QuestSetup.PanelPath),canvas.transform).GetComponent<GameFoundation.Quests.QuestPanel>();
        await Task.Delay(50);
        completed=false;foreach(var t in panel.GetComponentsInChildren<UnityEngine.UI.Text>())if(t.text=="Р вЂ”Р В°Р Т‘Р В°Р Р…Р С‘Р Вµ Р Р†РЎвЂ№Р С—Р С•Р В»Р Р…Р ВµР Р…Р С•")completed=true;
        Check(completed,"new scene UI instance preserves completed state");
        Check(!GameFoundation.Quests.QuestProgress.TryComplete(quest,manager),"completion cannot fire twice");
        Object.Destroy(canvas);
    }
}
