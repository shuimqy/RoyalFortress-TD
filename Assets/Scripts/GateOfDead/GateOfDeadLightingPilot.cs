using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoyalFortress.GateOfDead
{
    [ExecuteAlways, DefaultExecutionOrder(500)]
    public sealed class GateOfDeadLightingPilot : MonoBehaviour
    {
        [Serializable] public sealed class TorchRig
        {
            public SpriteRenderer flame;
            public Light2D illumination;
            public SpriteRenderer halo;
            public Color color;
            public float intensity = .9f;
            public float radius = 1.7f;
            public float phase;
        }
        [Serializable] public sealed class BeamRig
        {
            public SpriteRenderer source;
            public Light2D illumination;
            public SpriteRenderer halo;
            public Color color;
            public float intensity = .4f;
            [NonSerialized] public float lastLength = -1;
        }
        [Serializable] public sealed class EmissionLayer
        {
            public SpriteRenderer renderer;
            public Material originalMaterial;
            public Material luminousMaterial;
        }

        public GateOfDeadPresentation presentation;
        public Camera viewCamera;
        public Light2D ambient;
        public TorchRig orange, violet;
        public BeamRig heroBeam, bossBeam;
        public Light2D collisionLight;
        public SpriteRenderer collisionHalo;
        public EmissionLayer[] emissionLayers;
        public Volume bloomVolume;
        public bool dusk = true;
        public bool localLighting = true;
        public bool softHalos = true;
        public bool bloom = true;
        public bool showControls = true;
        [Range(.3f, 1)] public float ambientIntensity = .62f;
        public Color ambientColor = new Color(.72f, .8f, 1);
        [Range(.2f, 1.5f)] public float localStrength = 1;
        [Range(0, .3f)] public float bloomStrength = .16f;
        private Font uiFont;
        private GUIStyle labelStyle, buttonStyle;

        public void SetView(bool useDusk, bool useLocal = true, bool useHalos = true, bool useBloom = true)
        {
            dusk = useDusk; localLighting = useLocal; softHalos = useHalos; bloom = useBloom;
            RefreshNow();
        }
        private void OnEnable() { RefreshNow(); }
        private void LateUpdate() { RefreshNow(); }

        public void RefreshNow()
        {
            if (ambient == null || presentation == null) return;
            ambient.color = dusk ? ambientColor : Color.white;
            ambient.intensity = dusk ? ambientIntensity : 1;
            if (bloomVolume != null)
            {
                bloomVolume.enabled = dusk && bloom;
                bloomVolume.weight = bloomStrength / .3f;
            }
            if (viewCamera != null)
            {
                viewCamera.allowHDR = true;
                var data = viewCamera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = dusk && bloom;
            }
            if (emissionLayers != null) foreach (var e in emissionLayers)
                if (e.renderer != null) e.renderer.sharedMaterial = dusk ? e.luminousMaterial : e.originalMaterial;
            Torch(orange); Torch(violet);
            Beam(heroBeam); Beam(bossBeam);
            bool collision = dusk && (presentation.clash.enabled || presentation.hit.enabled);
            if (collisionLight != null)
            {
                collisionLight.enabled = collision && localLighting;
                collisionLight.transform.position = presentation.hit.enabled ? presentation.hit.transform.position : presentation.clash.transform.position;
                collisionLight.intensity = .52f * localStrength;
            }
            if (collisionHalo != null)
            {
                collisionHalo.enabled = collision && softHalos;
                collisionHalo.transform.position = collisionLight.transform.position;
                collisionHalo.color = new Color(1, .91f, .55f, .16f);
            }
        }
        private void Torch(TorchRig rig)
        {
            if (rig == null || rig.flame == null || rig.illumination == null) return;
            float time = presentation.Clock;
            float flicker = 1 + .04f * Mathf.Sin(time * 5.3f + rig.phase) + .025f * Mathf.Sin(time * 8.1f + rig.phase);
            Vector3 centre = rig.flame.transform.position + Vector3.up * .22f;
            rig.illumination.transform.position = centre;
            rig.illumination.enabled = dusk && localLighting && rig.flame.enabled;
            rig.illumination.color = rig.color;
            rig.illumination.intensity = rig.intensity * localStrength * flicker;
            rig.illumination.pointLightOuterRadius = rig.radius;
            rig.halo.enabled = dusk && softHalos && rig.flame.enabled;
            rig.halo.transform.position = centre;
            rig.halo.color = new Color(rig.color.r, rig.color.g, rig.color.b, .19f * flicker);
        }
        private void Beam(BeamRig rig)
        {
            if (rig == null || rig.source == null || rig.illumination == null) return;
            bool visible = dusk && rig.source.enabled && rig.source.color.a > .001f;
            rig.illumination.enabled = visible && localLighting;
            rig.halo.enabled = visible && softHalos;
            if (!visible) return;
            float length = rig.source.size.y;
            rig.illumination.transform.SetPositionAndRotation(rig.source.transform.position, rig.source.transform.rotation);
            rig.illumination.color = rig.color;
            rig.illumination.intensity = rig.intensity * localStrength * rig.source.color.a;
            if (Mathf.Abs(length - rig.lastLength) > .002f)
            {
                float half = length * .5f;
                rig.illumination.SetShapePath(new[] { new Vector3(-.11f, -half), new Vector3(-.11f, half), new Vector3(.11f, half), new Vector3(.11f, -half) });
                rig.lastLength = length;
            }
            rig.halo.transform.SetPositionAndRotation(rig.source.transform.position, rig.source.transform.rotation);
            rig.halo.transform.localScale = new Vector3(.5f, length * 1.06f, 1);
            rig.halo.color = new Color(rig.color.r, rig.color.g, rig.color.b, .17f * rig.source.color.a);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || !showControls) return;
            if (labelStyle == null)
            {
                uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 16);
                labelStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 16 };
                buttonStyle = new GUIStyle(GUI.skin.button) { font = uiFont, fontSize = 15 };
            }
            GUILayout.BeginArea(new Rect(12, 12, 430, 200), GUI.skin.box);
            GUILayout.Label("暮色照明试样 · 左橙火 / 左下紫火 / 对波", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("原亮度", buttonStyle)) SetView(false);
            if (GUILayout.Button("暮色试样", buttonStyle)) SetView(true);
            if (GUILayout.Button("仅环境光", buttonStyle)) SetView(true, false, false, false);
            GUILayout.EndHorizontal();
            GUILayout.Label("环境亮度  " + ambientIntensity.ToString("0.00"), labelStyle);
            ambientIntensity = GUILayout.HorizontalSlider(ambientIntensity, .3f, 1);
            GUILayout.Label("局部照明  " + localStrength.ToString("0.00"), labelStyle);
            localStrength = GUILayout.HorizontalSlider(localStrength, .2f, 1.5f);
            GUILayout.Label("柔光扩散  " + bloomStrength.ToString("0.00"), labelStyle);
            bloomStrength = GUILayout.HorizontalSlider(bloomStrength, 0, .3f);
            GUILayout.EndArea();
        }
        private void OnDestroy()
        {
            if (uiFont == null) return;
            if (Application.isPlaying) Destroy(uiFont); else DestroyImmediate(uiFont);
        }
    }
}
