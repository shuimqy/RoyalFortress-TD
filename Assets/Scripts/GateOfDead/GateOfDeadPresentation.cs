using System;
using UnityEngine;
using UnityEngine.Events;

namespace RoyalFortress.GateOfDead
{
    /// <summary>Layered map presentation in original colours. Gameplay calls PlaySealingSequence after victory.</summary>
    public sealed class GateOfDeadPresentation : MonoBehaviour
    {
        [Serializable] public sealed class Flame
        {
            public SpriteRenderer renderer;
            public int phase;
        }
        [Serializable] public sealed class FloatingStone
        {
            public Transform target;
            public Vector3 origin;
            public float amplitudePixels = 3;
            public float period = 3;
            public float phase;
        }

        public SpriteRenderer hero, boss, portal, upperRunes, lowerRunes;
        public SpriteRenderer heroBeam, bossBeam, clash, hit;
        public Sprite[] heroFrames, bossFrames, portalFrames, sealFrames;
        public Sprite[] heroBeamFrames, bossBeamFrames, clashFrames, hitFrames;
        public Sprite[] violetFrames, orangeFrames;
        public Flame[] violetFlames, orangeFlames;
        public FloatingStone[] stones;
        public Transform heroSocket, bossSocket, bossHit, portalCenter;
        [Range(0, 1)] public float pushProgress;
        public bool showControls = true;
        public bool paused;
        public UnityEvent onSealed = new UnityEvent();

        public float Clock { get; private set; }
        public float EndingTime { get; private set; } = -1;
        public int ImpactCount { get; private set; }
        public int SealCount { get; private set; }
        public bool IsSealed => EndingTime >= 4.55f;
        private GUIStyle labelStyle, buttonStyle;
        private Font uiFont;

        public static Vector3 MapPoint(float x, float y) => new Vector3((x - 768) / 64f, (512 - y) / 64f, 0);
        private static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
        private void Awake() { ResetPresentation(); }
        private void Update() { if (!paused) Advance(Time.deltaTime); }

        public void Advance(float seconds)
        {
            seconds = Mathf.Max(0, seconds);
            Clock += seconds;
            if (EndingTime >= 0)
            {
                float before = EndingTime;
                EndingTime += seconds;
                if (before < 2.2f && EndingTime >= 2.2f) ImpactCount++;
                if (before < 3.55f && EndingTime >= 3.55f) SealCount++;
                if (before < 4.55f && IsSealed) onSealed.Invoke();
            }
            RenderLayers();
        }

        public bool TryBeginVictory()
        {
            if (EndingTime >= 0) return false;
            EndingTime = 0;
            ImpactCount = SealCount = 0;
            RenderLayers();
            return true;
        }
        public void PlaySealingSequence() { TryBeginVictory(); }
        public void ResetPresentation()
        {
            Clock = 0; EndingTime = -1; ImpactCount = SealCount = 0;
            pushProgress = 0; paused = false;
            RenderLayers();
        }

        // Useful for editor scrubbing and deterministic visual checks; does not dispatch gameplay events.
        public void Sample(float time, float endingTime = -1)
        {
            Clock = Mathf.Max(0, time); EndingTime = endingTime; RenderLayers();
        }
        public float RuneAlpha(bool lower)
        {
            float t = Mathf.Repeat(Clock + (lower ? .4f : 0), 6);
            if (t <= 1.5f) return 1;
            if (t < 2.3f) return 1 - Ease((t - 1.5f) / .8f);
            if (t <= 3.3f) return 0;
            if (t < 4.1f) return Ease((t - 3.3f) / .8f);
            return 1;
        }
        private static void Alpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer != null) renderer.color = new Color(1, 1, 1, Mathf.Clamp01(alpha));
        }
        private static void Frame(SpriteRenderer renderer, Sprite[] frames, int index)
        {
            if (renderer != null && frames != null && frames.Length > 0)
                renderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }
        private void Flames(Flame[] instances, Sprite[] frames)
        {
            if (instances == null || frames == null || frames.Length == 0) return;
            foreach (var flame in instances)
                Frame(flame.renderer, frames, (Mathf.FloorToInt(Clock * 8) + flame.phase) % frames.Length);
        }
        private void Beam(SpriteRenderer renderer, Sprite[] frames, Vector3 start, Vector3 end, bool upward, float alpha = 1)
        {
            var delta = end - start;
            renderer.enabled = delta.sqrMagnitude > .000001f && alpha > 0;
            if (!renderer.enabled) return;
            Frame(renderer, frames, Mathf.FloorToInt(Clock * 8) % frames.Length);
            renderer.transform.position = (start + end) * .5f;
            renderer.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + (upward ? -90 : 90));
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(1, delta.magnitude);
            Alpha(renderer, alpha);
        }
        public Vector3 ClashPosition
        {
            get
            {
                float p = pushProgress;
                if (EndingTime >= 0 && EndingTime < 2.2f) p = Mathf.Lerp(p, 1, Ease(EndingTime / 2.2f));
                return Vector3.Lerp(MapPoint(797, 485), bossHit.position, p);
            }
        }

        public void RenderLayers()
        {
            if (hero == null || boss == null || portal == null) return;
            int loop = Mathf.FloorToInt(Clock * 8) % 6;
            Frame(hero, heroFrames, IsSealed ? 5 : loop);
            Frame(boss, bossFrames, loop);
            Alpha(boss, EndingTime < 2.2f ? 1 : 1 - (EndingTime - 2.2f) / .65f);
            Alpha(upperRunes, RuneAlpha(false)); Alpha(lowerRunes, RuneAlpha(true));
            Flames(violetFlames, violetFrames); Flames(orangeFlames, orangeFrames);
            if (stones != null) foreach (var stone in stones)
            {
                float pixels = Mathf.Round(stone.amplitudePixels * Mathf.Sin(2 * Mathf.PI * Clock / stone.period + stone.phase));
                stone.target.localPosition = stone.origin + new Vector3(0, pixels / 64f, 0);
            }
            if (EndingTime >= 3.55f) Frame(portal, sealFrames, Mathf.Min(7, Mathf.FloorToInt((EndingTime - 3.55f) * 8)));
            else Frame(portal, portalFrames, loop);

            heroBeam.enabled = bossBeam.enabled = clash.enabled = hit.enabled = false;
            if (EndingTime < 2.2f)
            {
                Vector3 point = ClashPosition;
                Beam(heroBeam, heroBeamFrames, heroSocket.position, point, true);
                Beam(bossBeam, bossBeamFrames, bossSocket.position, point, false);
                clash.enabled = true; clash.transform.position = point;
                Frame(clash, clashFrames, loop);
            }
            else if (EndingTime < 2.95f)
            {
                Beam(heroBeam, heroBeamFrames, heroSocket.position, bossHit.position, true);
                hit.enabled = true; hit.transform.position = bossHit.position;
                Frame(hit, hitFrames, Mathf.Min(5, Mathf.FloorToInt((EndingTime - 2.2f) * 8)));
            }
            else if (EndingTime < 3.55f)
                Beam(heroBeam, heroBeamFrames, heroSocket.position, Vector3.Lerp(bossHit.position, portalCenter.position, Ease((EndingTime - 2.95f) / .6f)), true);
            else if (EndingTime < 4.05f)
                Beam(heroBeam, heroBeamFrames, heroSocket.position, portalCenter.position, true, 1 - Mathf.Max(0, EndingTime - 3.85f) / .2f);
        }

        private void OnGUI()
        {
            if (!showControls) return;
            if (labelStyle == null)
            {
                uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 16);
                labelStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 16 };
                buttonStyle = new GUIStyle(GUI.skin.button) { font = uiFont, fontSize = 15 };
            }
            GUILayout.BeginArea(new Rect(12, 12, 380, 105), GUI.skin.box);
            GUILayout.Label(IsSealed ? "亡者之门 · 封印完成" : "亡者之门 · 原亮度", labelStyle);
            GUILayout.BeginHorizontal();
            GUI.enabled = EndingTime < 0;
            if (GUILayout.Button("封印演出", buttonStyle)) PlaySealingSequence();
            GUI.enabled = true;
            if (GUILayout.Button(paused ? "继续" : "暂停", buttonStyle)) paused = !paused;
            if (GUILayout.Button("重置", buttonStyle)) ResetPresentation();
            GUILayout.EndHorizontal();
            if (EndingTime < 0) pushProgress = GUILayout.HorizontalSlider(pushProgress, 0, 1);
            GUILayout.EndArea();
        }
        private void OnDestroy() { if (uiFont != null) Destroy(uiFont); }
    }
}
