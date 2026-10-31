using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoyalFortress.ForestSanctuary
{
    /// <summary>Independent ritual visuals. Cleared-wave events, not time, advance ritual progress.</summary>
    public sealed class ForestSanctuaryPresentation : MonoBehaviour
    {
        public enum RitualStage { Channeling, Completing, Blessed, Interrupted }
        public const int TotalWaves = 8;
        public const float CompletionDuration = 4.8f;
        public SpriteRenderer sage, hero, crystal, purificationPulse, blessingMark;
        public SpriteRenderer sageEnergy, heroEnergy;
        public LineRenderer sageConnection, heroConnection;
        public SpriteRenderer[] transferParticles;
        public Sprite[] sageChannelFrames, sageFinishFrames, heroKneelFrames, heroRiseFrames;
        public Sprite[] crystalIdleFrames, crystalShatterFrames, energyFrames, pulseFrames;
        public Transform sageHand, heroChest, crystalOrigin, crystalInput, crystalOutput;
        public Transform[] foundations;
        public Collider2D crystalClickTarget;
        public int startingLives = 10;
        public bool showControls = true;
        public bool simulationControlsEnabled = true;
        public bool paused;
        public bool interactionEnabled = true;
        public UnityEvent onPurificationPulse = new UnityEvent();
        public UnityEvent onBlessingCompleted = new UnityEvent();
        public event Action StateChanged;

        public float Clock { get; private set; }
        public float EndingTime { get; private set; }
        public float PulseTime { get; private set; } = -1;
        public int CompletedWaves { get; private set; }
        public int StoredPulse { get; private set; }
        public int HomeLives { get; private set; }
        public int PulseCount { get; private set; }
        public int ShatterCount { get; private set; }
        public int CompletionCount { get; private set; }
        public RitualStage Stage { get; private set; }
        public float RitualProgress => CompletedWaves / (float)TotalWaves;
        public bool CanReleasePulse => interactionEnabled && Stage == RitualStage.Channeling && HomeLives > 0 && StoredPulse > 0;
        public bool AutoDemonstration { get; private set; }
        private float autoTime;
        private Font uiFont;
        private GUIStyle labelStyle, buttonStyle, smallStyle;
        private readonly List<RaycastResult> pointerHits = new List<RaycastResult>();

        public static Vector3 MapPoint(float x, float y) => new Vector3((x - 768) / 64f, (512 - y) / 64f, 0);
        private void Awake() { ResetPresentation(); }
        private void Update()
        {
            if (!paused)
            {
                Advance(Time.deltaTime);
                if (AutoDemonstration && simulationControlsEnabled && Stage == RitualStage.Channeling)
                {
                    autoTime += Time.deltaTime;
                    if (autoTime >= 1.2f)
                    {
                        autoTime -= 1.2f;
                        if (CanReleasePulse) TryReleasePulse();
                        NotifyWaveCleared(CompletedWaves + 1, HomeLives);
                    }
                }
            }
            bool clicked = false; Vector2 pointer = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) { clicked = Mouse.current.leftButton.wasPressedThisFrame; pointer = Mouse.current.position.ReadValue(); }
#elif ENABLE_LEGACY_INPUT_MANAGER
            clicked = Input.GetMouseButtonDown(0); pointer = Input.mousePosition;
#endif
            if (clicked && CanReleasePulse && crystalClickTarget != null && Camera.main != null && !PointerOverUI(pointer))
            {
                Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, -Camera.main.transform.position.z));
                if (crystalClickTarget.OverlapPoint(world)) TryReleasePulse();
            }
        }

        private bool PointerOverUI(Vector2 pointer)
        {
            if (EventSystem.current == null) return false;
            pointerHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = pointer },pointerHits);
            foreach (var hit in pointerHits) if (hit.module is GraphicRaycaster) return true;
            return false;
        }

        public void ResetPresentation()
        {
            Clock = EndingTime = 0; PulseTime = -1;
            CompletedWaves = StoredPulse = PulseCount = ShatterCount = CompletionCount = 0;
            HomeLives = Mathf.Max(1, startingLives); Stage = RitualStage.Channeling;
            AutoDemonstration = false; autoTime = 0; paused = false;
            RenderLayers(); StateChanged?.Invoke();
        }

        /// <summary>Accept each consecutive, fully cleared wave exactly once.</summary>
        public bool NotifyWaveCleared(int waveNumber, int remainingHomeLives)
        {
            if (Stage != RitualStage.Channeling) return false;
            if (remainingHomeLives <= 0) { InterruptRitual(); return false; }
            if (waveNumber != CompletedWaves + 1 || waveNumber > TotalWaves) return false;
            HomeLives = remainingHomeLives; CompletedWaves = waveNumber;
            if (waveNumber == 2 || waveNumber == 4 || waveNumber == 6) StoredPulse = 1;
            if (waveNumber == TotalWaves) TryBeginCompletion();
            RenderLayers(); StateChanged?.Invoke(); return true;
        }
        public void ReportHomeLives(int remaining)
        {
            if (Stage != RitualStage.Channeling) return;
            HomeLives = Mathf.Max(0, remaining);
            if (HomeLives == 0) InterruptRitual(); else StateChanged?.Invoke();
        }
        public bool TryReleasePulse()
        {
            if (!CanReleasePulse) return false;
            StoredPulse = 0; PulseTime = 0; PulseCount++;
            RenderLayers(); StateChanged?.Invoke(); onPurificationPulse.Invoke(); return true;
        }
        public bool TryBeginCompletion()
        {
            if (Stage != RitualStage.Channeling || CompletedWaves != TotalWaves || HomeLives <= 0) return false;
            Stage = RitualStage.Completing; EndingTime = 0; StoredPulse = 0; AutoDemonstration = false;
            RenderLayers(); return true;
        }
        public bool InterruptRitual()
        {
            if (Stage != RitualStage.Channeling) return false;
            HomeLives = 0; Stage = RitualStage.Interrupted; StoredPulse = 0;
            PulseTime = -1; AutoDemonstration = false; RenderLayers(); StateChanged?.Invoke(); return true;
        }
        public void BeginAutomaticDemonstration()
        {
            if (!simulationControlsEnabled || Stage != RitualStage.Channeling) return;
            AutoDemonstration = true; autoTime = 0; paused = false;
        }
        public void Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            Clock += seconds;
            if (PulseTime >= 0) { PulseTime += seconds; if (PulseTime >= 1.1f) PulseTime = -1; }
            bool completed = false;
            if (Stage == RitualStage.Completing)
            {
                float before = EndingTime; EndingTime = Mathf.Min(CompletionDuration, EndingTime + seconds);
                if (before < .8f && EndingTime >= .8f) ShatterCount++;
                if (EndingTime >= CompletionDuration)
                {
                    Stage = RitualStage.Blessed; CompletionCount++; completed = true;
                }
            }
            RenderLayers();
            if (completed) { StateChanged?.Invoke(); onBlessingCompleted.Invoke(); }
        }
        private static void Frame(SpriteRenderer renderer, Sprite[] frames, int index, float alpha = 1)
        {
            if (renderer == null) return;
            renderer.enabled = frames != null && frames.Length > 0 && alpha > 0;
            if (!renderer.enabled) return;
            renderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            renderer.color = new Color(1, 1, 1, Mathf.Clamp01(alpha));
        }
        private void Energy(SpriteRenderer renderer, LineRenderer connection, Vector3 from, Vector3 to, int frame, float alpha)
        {
            Vector3 delta = to - from;
            Frame(renderer, energyFrames, frame, alpha);
            if (connection != null)
            {
                connection.enabled = alpha > 0;
                connection.SetPosition(0, from); connection.SetPosition(1, to);
                connection.startColor = connection.endColor = new Color(.72f, 1, .86f, Mathf.Clamp01(alpha));
            }
            if (renderer == null || !renderer.enabled) return;
            renderer.transform.position = (from + to) * .5f;
            renderer.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            renderer.transform.localScale = new Vector3(delta.magnitude / (176 / 64f), 14 / 72f, 1);
        }
        public void RenderLayers()
        {
            if (sage == null || hero == null || crystal == null || crystalOrigin == null) return;
            int loop = Mathf.FloorToInt(Clock * 8) % 4;
            bool success = Stage == RitualStage.Completing || Stage == RitualStage.Blessed;
            int sageFrame = success ? Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0, EndingTime - .9f) / .5f), 0, 3) : loop;
            int heroFrame = success ? Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0, EndingTime - 1.4f) / .45f), 0, 3) : loop;
            Frame(sage, success || Stage == RitualStage.Interrupted ? sageFinishFrames : sageChannelFrames, Stage == RitualStage.Interrupted ? 3 : sageFrame);
            Frame(hero, success ? heroRiseFrames : heroKneelFrames, Stage == RitualStage.Interrupted ? 0 : heroFrame);
            float chestY = success && heroFrame >= 2 ? 59 : success && heroFrame == 1 ? 44 : 31;
            if (heroChest != null) heroChest.localPosition = new Vector3(-9 / 64f, chestY / 64f, 0);
            float bob = !success && Stage != RitualStage.Interrupted ? Mathf.Round(Mathf.Sin(Clock * 2) * 1.5f) : 0;
            crystal.transform.position = crystalOrigin.position + new Vector3(0, -bob / 64f, 0);
            if (!success) Frame(crystal, crystalIdleFrames, Stage == RitualStage.Interrupted ? 0 : loop, Stage == RitualStage.Interrupted ? .45f : 1);
            else Frame(crystal, EndingTime < .8f ? crystalIdleFrames : crystalShatterFrames,
                EndingTime < .8f ? loop : Mathf.Clamp(Mathf.FloorToInt((EndingTime - .8f) / .4f), 0, 3),
                EndingTime < 2 ? 1 : Mathf.Clamp01(1 - (EndingTime - 2) / .7f));
            float energyAlpha = Stage == RitualStage.Interrupted || Stage == RitualStage.Blessed ? 0 : success ? Mathf.Clamp01(1 - (EndingTime - 1.7f)) : 1;
            Energy(sageEnergy, sageConnection, sageHand.position, crystalInput.position, loop, success && EndingTime >= 1 ? 0 : energyAlpha);
            Energy(heroEnergy, heroConnection, crystalOutput.position, heroChest.position, loop, energyAlpha);
            Frame(purificationPulse, pulseFrames, PulseTime < 0 ? 0 : Mathf.FloorToInt(PulseTime / .275f), PulseTime < 0 ? 0 : Mathf.Clamp01((1.1f - PulseTime) * 3));
            if (transferParticles != null) for (int i = 0; i < transferParticles.Length; i++)
            {
                float t = Mathf.Clamp01((EndingTime - .9f - i * .13f) / 1.1f);
                var particle = transferParticles[i];
                Frame(particle, crystalShatterFrames, 3, success && EndingTime < 3 ? Mathf.Sin(t * Mathf.PI) : 0);
                if (particle != null) particle.transform.position = Vector3.Lerp(crystalOrigin.position, heroChest.position, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 22 / 64f);
            }
            Frame(blessingMark, crystalIdleFrames, loop, success ? Mathf.Clamp01((EndingTime - 2.4f) * 2) : 0);
            if (blessingMark != null) blessingMark.transform.position = heroChest.position;
            if (crystalClickTarget != null) crystalClickTarget.enabled = !success && Stage != RitualStage.Interrupted;
        }
        private string StatusLabel()
        {
            if (Stage == RitualStage.Blessed) return "获得圣林加护";
            if (Stage == RitualStage.Interrupted) return "仪式中断";
            if (Stage == RitualStage.Completing) return EndingTime < .8f ? "最后引导" : EndingTime < 2 ? "水晶力量转移" : "勇者起身 · 加护完成";
            return "仪式进行中";
        }
        private void OnGUI()
        {
            if (!showControls) return;
            if (labelStyle == null)
            {
                uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 16);
                labelStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 16 };
                smallStyle = new GUIStyle(labelStyle) { fontSize = 13, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { font = uiFont, fontSize = 15 };
            }
            GUILayout.BeginArea(new Rect(12, 12, 415, simulationControlsEnabled ? 264 : 137), GUI.skin.box);
            GUILayout.Label("圣林加护 · " + StatusLabel(), labelStyle);
            GUILayout.Label($"守住波次 {CompletedWaves}/8  ·  仪式进度 {RitualProgress:P0}  ·  生命 {HomeLives}", labelStyle);
            GUILayout.Label(StoredPulse > 0 ? "净化脉冲：已就绪（最多一次）" : "净化脉冲：第 2 / 4 / 6 波结束后补充", smallStyle);
            GUILayout.BeginHorizontal();
            GUI.enabled = CanReleasePulse;
            if (GUILayout.Button("释放净化脉冲", buttonStyle)) TryReleasePulse();
            GUI.enabled = true;
            if (GUILayout.Button(paused ? "继续" : "暂停", buttonStyle)) paused = !paused;
            if (GUILayout.Button("逐帧", buttonStyle)) { paused = true; Advance(.125f); }
            GUILayout.EndHorizontal();
            if (simulationControlsEnabled)
            {
                GUILayout.Space(5); GUILayout.Label("分层演出试样 · 以下按钮模拟守住波次", smallStyle);
                GUILayout.BeginHorizontal(); GUI.enabled = Stage == RitualStage.Channeling;
                if (GUILayout.Button("模拟守住一波", buttonStyle)) NotifyWaveCleared(CompletedWaves + 1, HomeLives);
                if (GUILayout.Button("自动演示八波", buttonStyle)) BeginAutomaticDemonstration();
                GUILayout.EndHorizontal(); GUILayout.BeginHorizontal();
                if (GUILayout.Button("预览仪式中断", buttonStyle)) InterruptRitual();
                GUI.enabled = true;
                if (GUILayout.Button("重置演示", buttonStyle)) ResetPresentation();
                GUILayout.EndHorizontal();
                GUILayout.Label("脉冲不扣仪式进度；滚轮缩放，中键拖动，Home 复位。", smallStyle);
            }
            GUI.enabled = true; GUILayout.EndArea();
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.6f, 1, .8f);
            if (foundations != null) foreach (var site in foundations) if (site != null) Gizmos.DrawWireSphere(site.position, .35f);
        }
        private void OnDestroy() { if (uiFont != null) Destroy(uiFont); }
    }
}
