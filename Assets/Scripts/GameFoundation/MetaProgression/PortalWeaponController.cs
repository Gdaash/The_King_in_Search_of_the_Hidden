using System.Collections;
using System.Collections.Generic;
using GameFoundation.Audio;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameFoundation.MetaProgression
{
    [DefaultExecutionOrder(-50)]
    public sealed class PortalWeaponController : MonoBehaviour
    {
        public PortalTowerProgression progression;
        public GameObject beamPrefab;
        public Sprite lightningSprite;
        private ArcherTower tower;
        private FlashlightController beam;
        private Light2D spot;
        private Vector3 spotScale;
        private Vector2 aim;
        private float beamTime, ringTime, lightningTime, cacheTime;
        private float trailTime, minimumEnemyHeight=1, maximumEnemyHeight=1;
        private sealed class LightTrail
        {
            public LineRenderer line;
            public Vector2 origin;
            public float age, duration, height, drift;
        }
        private readonly List<LightTrail> lightTrails=new();
        private Health[] enemies = System.Array.Empty<Health>();
        private Material effectMaterial;
        private readonly List<GameObject> effects = new();
        public static PortalWeaponController Instance { get; private set; }
        public static bool OwnsPointer => false;
        public Vector2 BeamAim => aim;
        public float BeamRadius => Radius(PortalTowerBalance.Weapon.Beam);
        private void Awake() { Instance=this; }
        private void Start()
        {
            foreach(var t in FindObjectsByType<ArcherTower>(FindObjectsSortMode.None))
                if(t.GetStats()!=null && t.GetStats().IsPortalTower){tower=t;break;}
            effectMaterial=new Material(Shader.Find("Sprites/Default"));
        }
        private PortalTowerBalance.WeaponDefinition Def(PortalTowerBalance.Weapon w) => progression.Balance.FindWeapon(w);
        private float Range(PortalTowerBalance.Weapon w) => Mathf.Max(0,Def(w).range+(tower.GetStats()?.bonusAttackRange??0))*progression.Multiplier(w,PortalTowerBalance.Effect.Range);
        private float Radius(PortalTowerBalance.Weapon w) => Def(w).radius*progression.Multiplier(w,PortalTowerBalance.Effect.Area);
        private float Damage(PortalTowerBalance.Weapon w) => (Def(w).damage+(tower.GetStats()?.damageSettings.Find(d=>d.type==Def(w).damageType)?.bonusDamage??0))*progression.Multiplier(w,PortalTowerBalance.Effect.Damage);
        private float Cooldown(PortalTowerBalance.Weapon w) => Mathf.Max(.05f,Def(w).cooldown-(tower.GetStats()?.bonusAttackSpeed??0))/progression.Multiplier(w,PortalTowerBalance.Effect.AttackSpeed);
        private Transform Source => tower.GetComponent<TowerVisuals>()?.ShootPoint ?? tower.transform;
        private void Update()
        {
            if(tower==null || progression.ExpeditionEnded){if(beam!=null)beam.transform.root.gameObject.SetActive(false);ClearLightTrails();return;}
            if(Time.timeScale<=0){return;}
            if(Time.time>=cacheTime){enemies=FindObjectsByType<Health>(FindObjectsSortMode.None);RefreshEnemyHeights();cacheTime=Time.time+.15f;}
            if(progression.HasWeapon(PortalTowerBalance.Weapon.Beam)) UpdateBeam();
            else {if(beam!=null)beam.transform.root.gameObject.SetActive(false);ClearLightTrails();}
            if(progression.HasWeapon(PortalTowerBalance.Weapon.Rings) && Time.time>=ringTime)
            {
                ringTime=Time.time+Cooldown(PortalTowerBalance.Weapon.Rings);
                StartCoroutine(RingVolley());
            }
            if(progression.HasWeapon(PortalTowerBalance.Weapon.Lightning) && Time.time>=lightningTime)
                LightningVolley();
        }
        private bool Enemy(Health h) => h!=null && h.isActiveAndEnabled && !h.IsDead && h.CompareTag("Enemy1");
        private void UpdateBeam()
        {
            Health target=null;
            float nearest=Range(PortalTowerBalance.Weapon.Beam);
            nearest*=nearest;
            foreach(var h in enemies)
            {
                if(!Enemy(h))continue;
                float distance=((Vector2)h.transform.position-(Vector2)tower.transform.position).sqrMagnitude;
                if(distance<=nearest){nearest=distance;target=h;}
            }
            if(target==null)
            {
                if(beam!=null)beam.transform.root.gameObject.SetActive(false);ClearLightTrails();
                return;
            }
            aim=target.transform.position;
            if(beam==null)
            {
                var g=Instantiate(beamPrefab);g.name="Portal Burning Beam";
                beam=g.GetComponentInChildren<FlashlightController>(true);
                foreach(var l in g.GetComponentsInChildren<Light2D>(true))if(l.lightCookieSprite!=null){spot=l;break;}
                spotScale=spot.transform.localScale;
                beam.SetCrystalTarget(aim,true);
            }
            beam.transform.root.gameObject.SetActive(true);
            beam.transform.position=Source.position;
            beam.SetCrystalTarget(aim,true);
            float originalRadius=spot.lightCookieSprite.bounds.extents.x*Mathf.Abs(spotScale.x);
            spot.transform.localScale=spotScale*(BeamRadius/Mathf.Max(.01f,originalRadius));
            UpdateLightTrails();
            if(Time.time>=beamTime)
            {
                beamTime=Time.time+Cooldown(PortalTowerBalance.Weapon.Beam);
                bool hit=false;
                foreach(var h in enemies)if(Enemy(h) && Vector2.Distance(h.transform.position,aim)<=BeamRadius)
                {h.TakeDamage(Damage(PortalTowerBalance.Weapon.Beam),Def(PortalTowerBalance.Weapon.Beam).damageType,tower.transform);hit=true;}
                if(hit)GameAudioController.PlayAt(GameAudioCue.MagicAttack,aim);
            }
        }
        private void RefreshEnemyHeights()
        {
            minimumEnemyHeight=float.PositiveInfinity;maximumEnemyHeight=0;
            foreach(var h in enemies)
            {
                if(!Enemy(h))continue;
                bool found=false;Bounds bounds=default;
                foreach(var renderer in h.GetComponentsInChildren<SpriteRenderer>())
                {
                    if(!renderer.enabled || renderer.sprite==null)continue;
                    if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);
                }
                if(!found || bounds.size.y<=0)continue;
                minimumEnemyHeight=Mathf.Min(minimumEnemyHeight,bounds.size.y);
                maximumEnemyHeight=Mathf.Max(maximumEnemyHeight,bounds.size.y);
            }
            if(maximumEnemyHeight<=0)minimumEnemyHeight=maximumEnemyHeight=1;
        }
        private void UpdateLightTrails()
        {
            if(Time.time>=trailTime)
            {
                trailTime=Time.time+.055f;
                LightTrail trail=lightTrails.Find(t=>!t.line.gameObject.activeSelf);
                if(trail==null && lightTrails.Count<24)
                {
                    trail=new LightTrail{line=Line("Burning light trail",Color.white,1f/32,2)};
                    trail.line.transform.SetParent(beam.transform.root,false);
                    trail.line.sortingOrder=151;
                    trail.line.endWidth=.012f;
                    lightTrails.Add(trail);
                }
                if(trail!=null)
                {
                    trail.origin=aim+Random.insideUnitCircle*BeamRadius*.95f;
                    trail.age=0;trail.duration=Random.Range(.45f,.9f);
                    trail.height=Random.Range(minimumEnemyHeight,maximumEnemyHeight);
                    trail.drift=Random.Range(-.12f,.12f);
                    trail.line.gameObject.SetActive(true);
                }
            }
            foreach(var trail in lightTrails)
            {
                if(!trail.line.gameObject.activeSelf)continue;
                trail.age+=Time.deltaTime;
                float progress=trail.age/trail.duration;
                if(progress>=1){trail.line.gameObject.SetActive(false);continue;}
                float head=trail.height*progress;
                float tail=Mathf.Max(0,head-trail.height*.2f);
                Vector2 top=trail.origin+new Vector2(trail.drift*progress,head);
                Vector2 bottom=trail.origin+new Vector2(trail.drift*Mathf.Max(0,progress-.2f),tail);
                trail.line.SetPosition(0,bottom);trail.line.SetPosition(1,top);
                float alpha=Mathf.Sin(progress*Mathf.PI)*.85f;
                trail.line.startColor=new Color(1,.65f,.25f,alpha*.15f);
                trail.line.endColor=new Color(1,.95f,.7f,alpha);
            }
        }
        private void ClearLightTrails()
        {
            foreach(var trail in lightTrails)trail.line.gameObject.SetActive(false);
        }
        private IEnumerator RingVolley()
        {
            GameAudioController.PlayAt(GameAudioCue.MagicAttack,tower.transform.position);
            for(int i=0;i<progression.Count(PortalTowerBalance.Weapon.Rings);i++)
            {
                if(progression.ExpeditionEnded)yield break;
                StartCoroutine(Ring());yield return new WaitForSeconds(.22f);
            }
        }
        private LineRenderer Line(string name,Color color,float width,int count)
        {
            var go=new GameObject(name);effects.Add(go);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=effectMaterial;
            line.useWorldSpace=true;line.positionCount=count;line.startWidth=line.endWidth=width;
            line.startColor=line.endColor=color;line.sortingLayerName="OverLight";line.sortingOrder=150;
            return line;
        }
        private void ClearEffect(LineRenderer line){effects.Remove(line.gameObject);Destroy(line.gameObject);}
        private IEnumerator Ring()
        {
            var w=PortalTowerBalance.Weapon.Rings;
            Vector2 origin=tower.transform.position;
            var line=Line("Portal expanding ring",new Color(.65f,.55f,1,.85f),.12f,65);
            var hit=new HashSet<Health>();float radius=0,previous=0;
            float speed=Def(w).expansionSpeed*progression.Multiplier(w,PortalTowerBalance.Effect.ExpansionSpeed);
            float range=Range(w),thickness=Radius(w),damage=Damage(w);
            while(radius<range && !progression.ExpeditionEnded)
            {
                if(Time.timeScale<=0){yield return null;continue;}
                previous=radius;radius=Mathf.Min(range,radius+speed*Time.deltaTime);
                for(int i=0;i<65;i++)
                {
                    float angle=i*Mathf.PI*2/64;Vector2 p=origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                    line.SetPosition(i,new Vector3(Mathf.Round(p.x*32)/32,Mathf.Round(p.y*32)/32,0));
                }
                foreach(var h in enemies)if(Enemy(h) && !hit.Contains(h))
                {
                    float d=Vector2.Distance(origin,h.transform.position);
                    if(d>=previous-thickness && d<=radius+thickness){hit.Add(h);h.TakeDamage(damage,Def(w).damageType,tower.transform);}
                }
                line.startColor=line.endColor=new Color(.65f,.55f,1,Mathf.Lerp(.85f,.2f,radius/range));
                yield return null;
            }
            ClearEffect(line);
        }
        private void LightningVolley()
        {
            var w=PortalTowerBalance.Weapon.Lightning;var candidates=new List<Health>();
            foreach(var h in enemies)if(Enemy(h) && Vector2.Distance(tower.transform.position,h.transform.position)<=Range(w))candidates.Add(h);
            if(candidates.Count==0)return;
            lightningTime=Time.time+Cooldown(w);
            for(int i=0;i<progression.Count(w) && candidates.Count>0;i++)
            {
                int index=Random.Range(0,candidates.Count);var h=candidates[index];candidates.RemoveAt(index);
                Vector3 end=h.transform.position;
                h.TakeDamage(Damage(w),Def(w).damageType,tower.transform);
                CombatImpactBurst.Spawn(end,new Color(.55f,.8f,1));
                GameAudioController.PlayAt(GameAudioCue.MagicAttack,end);
                StartCoroutine(Lightning(end));
            }
        }
        private IEnumerator Lightning(Vector3 end)
        {
            if(lightningSprite==null)yield break;
            var w=PortalTowerBalance.Weapon.Lightning;
            var go=new GameObject("Portal pixel lightning");effects.Add(go);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=lightningSprite;
            renderer.sharedMaterial=effectMaterial;renderer.sortingLayerName="OverLight";renderer.sortingOrder=150;
            float height=lightningSprite.rect.height/lightningSprite.pixelsPerUnit;
            go.transform.position=new Vector3(Mathf.Round(end.x*32)/32,Mathf.Round((end.y+height*.5f)*32)/32,end.z);
            if(Random.value>.5f)renderer.flipX=true;
            var glow=go.AddComponent<Light2D>();glow.lightType=Light2D.LightType.Point;
            glow.color=new Color(.25f,.65f,1);glow.intensity=1.5f;glow.pointLightOuterRadius=1.2f;
            float duration=Def(w).duration*progression.Multiplier(w,PortalTowerBalance.Effect.Duration);
            float elapsed=0;
            while(elapsed<duration)
            {
                elapsed+=Time.deltaTime;float alpha=1-Mathf.Clamp01(elapsed/duration);
                renderer.color=new Color(1,1,1,alpha);glow.intensity=1.5f*alpha;
                yield return null;
            }
            effects.Remove(go);Destroy(go);
        }
        private void OnDestroy()
        {
            if(Instance==this)Instance=null;
            if(beam!=null)Destroy(beam.transform.root.gameObject);
            foreach(var g in effects)if(g!=null)Destroy(g);
            if(effectMaterial!=null)Destroy(effectMaterial);
        }
    }
}

