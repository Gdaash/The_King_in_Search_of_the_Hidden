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
}
