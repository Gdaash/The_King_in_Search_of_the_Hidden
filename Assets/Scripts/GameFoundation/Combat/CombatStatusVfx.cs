using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.Combat
{
    /// <summary>Small bounded pixel meshes; no new textures, materials or particles per frame.</summary>
    public sealed class CombatStatusVfx : MonoBehaviour
    {
        private static Material material;
        private static readonly Mesh[] meshes = new Mesh[4];
        private Health owner;
        private SpriteRenderer body;
        private ParticleSystem stun, fire, echo, electric;
        private float stunEnd, fireEnd, echoEnd;
        private const float Pixel = 1f / 32f;
        public int VisibleParticleCount => Count(stun) + Count(fire) + Count(echo) + Count(electric);
        private static int Count(ParticleSystem value) => value != null ? value.particleCount : 0;

        public static CombatStatusVfx Attach(Health target, SpriteRenderer visual = null)
        {
            var existing = target.GetComponentInChildren<CombatStatusVfx>(true);
            if (existing != null) return existing;
            var root = new GameObject("Combat Status Particles");
            root.transform.SetParent(target.transform, false);
            var effect = root.AddComponent<CombatStatusVfx>();
            effect.owner = target;
            effect.body = visual != null ? visual : target.GetComponentInChildren<SpriteRenderer>();
            effect.RefreshPosition();
            return effect;
        }

        internal static Material PixelMaterial => SharedMaterial;
        private static Material SharedMaterial
        {
            get
            {
                if (material == null) material = new Material(Resources.Load<Shader>("Shaders/Combat Status Particles")) { name = "Pixel status particles", hideFlags = HideFlags.DontSave };
                return material;
            }
        }

        private static Mesh MeshFor(int kind)
        {
            if (meshes[kind] != null) return meshes[kind];
            string[] pixels = kind switch
            {
                // Four-point pixel star, deliberately smaller and paler than level stars.
                0 => new[] { "....#....", "....#....", "...###...", "..#####..", "#########", "..#####..", "...###...", "....#....", "....#...." },
                1 => new[] { "...#...", "...##..", "..###..", "..####.", ".#####.", ".######", "#######", "#######", ".#####.", "..###.." },
                2 => new[] { ".#.", "###", ".#." },
                _ => new[] { ".#.", "###", ".#." }
            };
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>(); var uv = new List<Vector2>();
            float unit = 1f / Mathf.Max(pixels.Length, pixels[0].Length);
            for (int y = 0; y < pixels.Length; y++) for (int x = 0; x < pixels[y].Length; x++)
            {
                if (pixels[y][x] != '#') continue;
                int n = vertices.Count;
                float left = (x - pixels[0].Length * .5f) * unit, bottom = (pixels.Length * .5f - y - 1) * unit;
                vertices.Add(new Vector3(left, bottom)); vertices.Add(new Vector3(left + unit, bottom));
                vertices.Add(new Vector3(left + unit, bottom + unit)); vertices.Add(new Vector3(left, bottom + unit));
                triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
                Color shade = kind == 1 && y > pixels.Length / 2 && x > 1 && x < 5 ? new Color(1, 1, .55f) : Color.white;
                for (int c = 0; c < 4; c++) colors.Add(shade);
                uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.one); uv.Add(Vector2.up);
            }
            var mesh = new Mesh { name = new[] { "Pixel stun star", "Pixel flame tongue", "Pixel echo mote", "Pixel electric spark" }[kind], hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetColors(colors); mesh.SetUVs(0, uv); mesh.RecalculateNormals();
            return meshes[kind] = mesh;
        }

        private ParticleSystem Create(string label, int kind, Color color, float size, float lifetime, bool orbit)
        {
            var root = new GameObject(label); root.transform.SetParent(transform, false);
            var system = root.AddComponent<ParticleSystem>(); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            if (kind == 3) main.useUnscaledTime = true;
            main.maxParticles = 24; main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * .7f, lifetime);
            main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(size * .75f, size); main.startColor = color;
            var emission = system.emission; emission.enabled = false; emission.rateOverTime = kind == 0 ? 12 : kind == 1 ? 10 : 16;
            var shape = system.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = orbit ? .32f : .22f;
            shape.radiusThickness = orbit ? 0 : 1;
            var velocity = system.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(orbit ? 0 : -.12f, orbit ? 0 : .12f);
            velocity.y = new ParticleSystem.MinMaxCurve(orbit ? 0 : .35f, orbit ? 0 : .65f); velocity.z = new ParticleSystem.MinMaxCurve(0, 0);
            if (orbit) { velocity.orbitalZ = new ParticleSystem.MinMaxCurve(kind == 0 ? 5f : -6f); }
            var fade = system.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.9f, .12f), new GradientAlphaKey(.7f, .65f), new GradientAlphaKey(0, 1) }); fade.color = gradient;
            var renderer = root.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = MeshFor(kind);
            renderer.sharedMaterial = SharedMaterial;
            var tint = new MaterialPropertyBlock(); tint.SetColor("_TintColor", color); renderer.SetPropertyBlock(tint);
            renderer.alignment = ParticleSystemRenderSpace.World;
            renderer.sortingLayerID = body != null ? body.sortingLayerID : 0; renderer.sortingOrder = body != null ? body.sortingOrder + 8 : 150;
            return system;
        }

        public void ShowStun(float until)
        {
            if (stun == null) stun = Create("Stun orbiting pixel stars", 0, new Color(.9f, .92f, .94f, .95f), .19f, .38f, true);
            stun.transform.localPosition = Vector3.up * .35f;
            stunEnd = until; Play(stun); stun.Emit(3);
        }
        public void ShowBurn(float until)
        {
            if (fire == null) fire = Create("Burning pixel flames", 1, new Color(1f, .38f, .12f, .9f), .28f, .6f, false);
            fire.transform.localPosition = Vector3.down * .16f;
            fireEnd = until; Play(fire); fire.Emit(3);
        }
        public void BurnTick() { if (fire != null) fire.Emit(5); }
        public void ShowEcho()
        {
            if (echo == null) echo = Create("Magic echo orbiting motes", 2, new Color(.76f, .42f, 1f, .85f), .12f, .45f, true);
            echoEnd = Time.time + CombatHitEffects.EchoDelaySeconds; Play(echo); echo.Emit(5);
        }
        public void EchoBurst() { if (echo != null) echo.Emit(9); }
        public void ElectricBurst()
        {
            if (electric == null) electric = Create("Electric pixel sparks", 3, DamagePalette.Electric, .1f, .22f, false);
            electric.Play(); electric.Emit(8);
        }
        private static void Play(ParticleSystem value) { var emission = value.emission; emission.enabled = true; if (!value.isPlaying) value.Play(); }
        private static void StopEmission(ParticleSystem value) { if (value == null) return; var emission = value.emission; emission.enabled = false; }
        private void RefreshPosition()
        {
            if (owner == null) return;
            transform.position = body != null ? body.bounds.center : owner.transform.position;
            Vector3 scale = owner.transform.lossyScale;
            transform.localScale = new Vector3(1 / Mathf.Max(.01f, Mathf.Abs(scale.x)), 1 / Mathf.Max(.01f, Mathf.Abs(scale.y)), 1);
        }
        private void LateUpdate()
        {
            RefreshPosition();
            if (Time.time >= stunEnd) StopEmission(stun);
            if (Time.time >= fireEnd) StopEmission(fire);
            if (Time.time >= echoEnd) StopEmission(echo);
        }
        public void Clear()
        {
            stunEnd = fireEnd = echoEnd = 0;
            foreach (var system in new[] { stun, fire, echo, electric })
                if (system != null) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnDisable() => Clear();

        public static void LightningArc(Health from, Health to, Scene scene, float delay = 0)
        {
            var root = new GameObject("Chain lightning pixel arc"); SceneManager.MoveGameObjectToScene(root, scene);
            var arc = root.AddComponent<CombatLightningArc>();
            arc.Track(from, to);
            arc.Launch(from.HitEffectPosition, to.HitEffectPosition, SharedMaterial, delay);
        }

        public static void LightningArc(Vector3 from, Vector3 to, Scene scene, float delay = 0)
        {
            var root = new GameObject("Chain lightning pixel arc"); SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<CombatLightningArc>().Launch(from, to, SharedMaterial, delay);
        }
    }

    internal sealed class CombatLightningArc : MonoBehaviour
    {
        private LineRenderer core, glow;
        private MaterialPropertyBlock tintBlock;
        private void Awake() => tintBlock = new MaterialPropertyBlock();
        private float delay;
        private Health source, target;
        private int sourceVersion, targetVersion;
        private Vector3 start, end;
        private Vector3[] path;
        public void Track(Health from, Health to)
        { source = from; target = to; sourceVersion = from.SpawnVersion; targetVersion = to.SpawnVersion; }
        private void LateUpdate()
        {
            if (source != null && source.SpawnVersion == sourceVersion && !source.IsDead && source.isActiveAndEnabled) start = source.HitEffectPosition;
            else source = null;
            if (target != null && target.SpawnVersion == targetVersion && !target.IsDead && target.isActiveAndEnabled) end = target.HitEffectPosition;
            else target = null;
            if (path == null || core == null) return;
            BuildPath(); core.SetPositions(path); glow.SetPositions(path);
        }
        private void BuildPath()
        {
            Vector3 normal = Vector3.Cross((end - start).normalized, Vector3.forward);
            for (int i = 0; i < path.Length; i++)
            {
                Vector3 point = Vector3.Lerp(start, end, (float)i / (path.Length - 1));
                if (i > 0 && i < path.Length - 1) point += normal * (Mathf.Sin(i * 2.71f) * .09f);
                point.x = Mathf.Round(point.x * 32) / 32; point.y = Mathf.Round(point.y * 32) / 32;
                path[i] = point;
            }
            path[0] = start; path[path.Length - 1] = end;
        }
        private static float PresentationDelta => Time.timeScale > 0 ? Time.unscaledDeltaTime : 0;
        public void Launch(Vector3 from, Vector3 to, Material material, float startDelay)
        {
            delay = startDelay;
            start = from; end = to;
            int segments = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(from, to) / .18f), 3, 24);
            path = new Vector3[segments + 1]; BuildPath();
            glow = CreateLine("Yellow lightning glow", material, path, 3.5f / 32, 210);
            core = CreateLine("White yellow lightning core", material, path, 1.5f / 32, 211);
            StartCoroutine(Fade());
        }
        private LineRenderer CreateLine(string label, Material material, Vector3[] points, float width, int order)
        {
            var root = new GameObject(label); root.transform.SetParent(transform, false);
            var line = root.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.useWorldSpace = true;
            line.startWidth = line.endWidth = width; line.numCapVertices = line.numCornerVertices = 0;
            line.sortingLayerName = "OverLight"; line.sortingOrder = order;
            line.positionCount = points.Length; line.SetPositions(points); line.enabled = false;
            return line;
        }
        private void Tint(LineRenderer line, Color tint, float alpha)
        {
            tint.a *= alpha;
            tintBlock.SetColor("_TintColor", tint); line.SetPropertyBlock(tintBlock);
            line.startColor = line.endColor = Color.white;
        }
        private IEnumerator Fade()
        {
            float elapsed = 0;
            while (elapsed < delay) { elapsed += PresentationDelta; yield return null; }
            core.enabled = glow.enabled = true;
            elapsed = 0;
            const float hold = .18f, fade = .26f;
            while (elapsed < hold + fade)
            {
                float alpha = elapsed < hold ? 1 : 1 - (elapsed - hold) / fade;
                Tint(core, new Color(1, .98f, .74f, 1), alpha);
                Tint(glow, new Color(1, .7f, .08f, .7f), alpha);
                elapsed += PresentationDelta; yield return null;
            }
            Destroy(gameObject);
        }
    }
}
