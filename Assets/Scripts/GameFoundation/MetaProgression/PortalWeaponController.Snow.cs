using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    public sealed partial class PortalWeaponController
    {
        private Mesh frostEmitterMesh, frostFlakeMesh;
        private void EnsureSnowMeshes()
        {
            if (frostEmitterMesh != null) return;
            frostEmitterMesh = new Mesh { name = "Frost hex emission" };
            frostEmitterMesh.vertices = new[]{Vector3.zero,new Vector3(1,0),new Vector3(.5f,1),new Vector3(-.5f,1),new Vector3(-1,0),new Vector3(-.5f,-1),new Vector3(.5f,-1)};
            frostEmitterMesh.triangles = new[]{0,1,2,0,2,3,0,3,4,0,4,5,0,5,6,0,6,1};frostEmitterMesh.RecalculateNormals();
            frostFlakeMesh = CreateParticleMesh("Pixel snowflake", new[] { "..#..", ".###.", "#####", ".###.", "..#.." });
        }

        private static Mesh CreateParticleMesh(string name, string[] rows)
        {
            var vertices = new List<Vector3>();var triangles = new List<int>();var uv = new List<Vector2>();
            void Pixel(int x,int y)
            {
                int n=vertices.Count;float l=(x-.5f)/5, b=(y-.5f)/5;
                vertices.Add(new Vector3(l,b));vertices.Add(new Vector3(l+.2f,b));vertices.Add(new Vector3(l+.2f,b+.2f));vertices.Add(new Vector3(l,b+.2f));
                uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.one);uv.Add(Vector2.up);triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (rows[y][x] == '#') Pixel(x - 2, 2 - y);
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals();
            return mesh;
        }
        private ParticleSystem CreateFrostSnow(Vector2 center,float radius,float ratio)
        {
            EnsureSnowMeshes();
            var go = new GameObject("Frost rising pixel snow");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,tower.gameObject.scene);go.transform.position=center;
            var snow=go.AddComponent<ParticleSystem>();snow.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=snow.main;main.loop=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.65f,1.1f);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.08f,.14f);main.maxParticles=16;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            main.startColor=new ParticleSystem.MinMaxGradient(new Color(.55f,.83f,1,.65f),new Color(.9f,.97f,1,.85f));
            var emission=snow.emission;emission.rateOverTime=6;
            var shape=snow.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Mesh;shape.mesh=frostEmitterMesh;shape.meshShapeType=ParticleSystemMeshShapeType.Triangle;shape.scale=new Vector3(radius,radius*ratio,1);
            var velocity=snow.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
            velocity.x=new ParticleSystem.MinMaxCurve(-.08f,.08f);velocity.y=new ParticleSystem.MinMaxCurve(.3f,.55f);velocity.z=new ParticleSystem.MinMaxCurve(0,0);
            var color=snow.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(0,1)});color.color=gradient;
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=frostFlakeMesh;renderer.sharedMaterial=GameFoundation.Combat.CombatStatusVfx.PixelMaterial;
            var tint=new MaterialPropertyBlock();tint.SetColor("_TintColor",DamagePalette.Ice);renderer.SetPropertyBlock(tint);renderer.sortingOrder=4;renderer.alignment=ParticleSystemRenderSpace.World;
            snow.Play();return snow;
        }
    }
}
