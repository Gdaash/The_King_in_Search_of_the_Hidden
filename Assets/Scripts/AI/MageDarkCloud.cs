using UnityEngine;

/// <summary>After launch this effect belongs to the scene, never to its caster.</summary>
public sealed class MageDarkCloud : MonoBehaviour
{
    private static Sprite mistSprite;
    private readonly SpriteRenderer[] wisps = new SpriteRenderer[12];
    private Transform destination;
    private TowerDarkness darkness;
    private float age, remaining;
    private bool launched, attached;
    public static Vector3 CrystalPosition(ArcherTower tower)
    {
        // The firing socket is halfway down the tower; the crystal is at the sprite's top.
        var sprite=tower.GetComponent<SpriteRenderer>();
        return sprite!=null&&sprite.sprite!=null
            ? new Vector3(sprite.bounds.center.x,sprite.bounds.max.y-.2f,tower.transform.position.z)
            : tower.transform.position+Vector3.up*2.2f;
    }
    public void Initialize(Sprite sprite)
    {
        if(sprite==null)
        {
            if(mistSprite==null)
            {
                var texture=new Texture2D(32,32,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name="Dark mist particle"};
                for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                {
                    float radius=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/16f;
                    float noise=Mathf.PerlinNoise(x*.23f,y*.23f);
                    texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01((1-radius)*2)*( .45f+noise*.55f)));
                }
                texture.Apply();mistSprite=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),32);
            }
            sprite=mistSprite;
        }
        for(int i=0;i<wisps.Length;i++)
        {
            var child=new GameObject("Dark fog wisp");child.transform.SetParent(transform,false);
            var r=child.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingLayerName="OverLight";r.sortingOrder=180;
            r.color=new Color(.19f,.18f,.22f,.65f);wisps[i]=r;
        }
        transform.localScale=Vector3.zero;
    }
    public void Grow(float progress) => transform.localScale=Vector3.one*Mathf.SmoothStep(0,1,progress);
    public void Launch(ArcherTower tower,float duration)
    {
        transform.SetParent(null,true);destination=tower.transform;remaining=duration;launched=true;Grow(1);
    }
    private void Update()
    {
        age+=Time.deltaTime;
        for(int i=0;i<wisps.Length;i++)
        {
            var r=wisps[i];if(r==null||r.sprite==null)continue;
            float angle=i*2.39996f+age*(.2f+i*.015f);
            r.transform.localPosition=new Vector3(Mathf.Cos(angle)*.42f,Mathf.Sin(angle)*.19f,0);
            float size=.65f+.12f*Mathf.Sin(age*1.6f+i);
            r.transform.localScale=Vector3.one*(size/Mathf.Max(.01f,r.sprite.bounds.size.x));
        }
        if(!launched)return;
        if(destination==null){Destroy(gameObject);return;}
        var tower=destination.GetComponent<ArcherTower>();
        if(tower==null){Destroy(gameObject);return;}
        var point=CrystalPosition(tower);
        if(!attached)
        {
            transform.position=Vector3.MoveTowards(transform.position,point,4f*Time.deltaTime);
            if(Vector3.Distance(transform.position,point)>.03f)return;
            darkness=destination.GetComponent<TowerDarkness>();if(darkness==null)darkness=destination.gameObject.AddComponent<TowerDarkness>();
            darkness.Attach();attached=true;
        }
        transform.position=point;remaining-=Time.deltaTime;
        if(remaining<=0)Destroy(gameObject);
    }
    private void OnDestroy(){if(attached&&darkness!=null)darkness.Detach();}
}
