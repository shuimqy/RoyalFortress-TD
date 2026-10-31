using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using KingdomBorder.TwinTowers;

namespace RoyalFortress.ForestSanctuary
{
    /// <summary>Playable-level HUD, purification combat and victory/failure handoff.</summary>
    public sealed class ForestSanctuaryGameplay : MonoBehaviour
    {
        public WaveGameController game;
        public ForestSanctuaryPresentation presentation;
        public TowerBuildController building;
        public FortressHud hud;
        public Button pulseButton, routeButton;
        public Text pulseText, pulseHint, ritualText, routeText;
        public GameObject routeOverlay;
        [Min(.1f)] public float pulseRadius = 3.5f;
        [Min(0)] public float pulseDamage = 110;
        [Range(0,1)] public float pulseArmorPenetration = .65f;
        [Range(0,.9f)] public float pulseSlowFraction = .45f;
        [Min(0)] public float pulseSlowSeconds = 2;
        public bool VictoryStarted { get; private set; }
        public bool VictoryFinished { get; private set; }
        public bool Failed { get; private set; }
        public int LastPulseHitCount { get; private set; }
        public float LastPulseDamage { get; private set; }
        private bool stoppedBuilding;
        private IEnumerator Start()
        {
            while (!hud.IsReady) yield return null;
            RestoreRitualControls();
            presentation.showControls = false;
            presentation.simulationControlsEnabled = false;
            game.StateChanged += Refresh;
            presentation.StateChanged += Refresh;
            presentation.onPurificationPulse.AddListener(ApplyPulse);
            presentation.onBlessingCompleted.AddListener(FinishVictory);
            pulseButton.onClick.AddListener(ReleasePulse);
            routeButton.onClick.AddListener(ToggleRoutes);
            routeOverlay.SetActive(false); Refresh();
        }
        private void RestoreRitualControls()
        {
            PlaceSupplement(ritualText.transform.parent.gameObject,new Vector2(18,-57),new Vector2(278,48));
            PlaceSupplement(pulseButton.gameObject,new Vector2(18,-112),new Vector2(278,54));
            PlaceSupplement(pulseHint.transform.parent.gameObject,new Vector2(18,-173),new Vector2(278,62));
            pulseHint.rectTransform.sizeDelta=new Vector2(252,54);pulseHint.fontSize=14;
            PlaceSupplement(routeButton.gameObject,new Vector2(18,-242),new Vector2(142,40));
        }
        private void PlaceSupplement(GameObject control,Vector2 position,Vector2 size)
        {
            var rect=control.GetComponent<RectTransform>();rect.SetParent(hud.ExtraControlsRoot,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.up;rect.anchoredPosition=position;rect.sizeDelta=size;
            foreach(var label in control.GetComponentsInChildren<Text>(true))label.font=hud.lifeText.font;
            control.SetActive(true);
        }
        public void ReleasePulse() { presentation.TryReleasePulse(); }
        public void ToggleRoutes()
        {
            routeOverlay.SetActive(!routeOverlay.activeSelf);
            routeText.text = routeOverlay.activeSelf ? "隐藏路径" : "显示路径";
        }
        private void ApplyPulse()
        {
            LastPulseHitCount = 0; LastPulseDamage = 0;
            Vector3 center = presentation.purificationPulse.transform.position;
            // Snapshot only this level's enemies: damage may remove them immediately.
            var targets = game.enemiesRoot.GetComponentsInChildren<WaypointEnemy>().Where(e => !e.Resolved && e.Combat != null && e.Combat.IsAlive).ToArray();
            foreach (var enemy in targets)
            {
                if (((Vector2)(enemy.transform.position-center)).sqrMagnitude > pulseRadius*pulseRadius) continue;
                enemy.Combat.ApplySlow(pulseSlowFraction,pulseSlowSeconds,GetInstanceID());
                LastPulseDamage += enemy.Combat.TakeDamage(pulseDamage,pulseArmorPenetration);
                LastPulseHitCount++;
            }
            Refresh();
        }
        private void StopBuildingAndTowers()
        {
            if (stoppedBuilding) return;
            stoppedBuilding = true; building.Cancel(); building.enabled = false;
            foreach (var site in building.sites)
            {
                if (site.clickArea != null) site.clickArea.enabled = false;
                site.Hover(false); site.Select(false);
            }
            foreach (var tower in building.builtTowersRoot.GetComponentsInChildren<TowerBehaviour>()) tower.attackEnabled = false;
        }
        private void Refresh()
        {
            if (game == null || presentation == null) return;
            presentation.interactionEnabled = !game.IsPaused;
            Failed = game.IsGameOver || presentation.Stage == ForestSanctuaryPresentation.RitualStage.Interrupted;
            VictoryStarted = presentation.Stage == ForestSanctuaryPresentation.RitualStage.Completing || presentation.Stage == ForestSanctuaryPresentation.RitualStage.Blessed;
            if (Failed || VictoryStarted) StopBuildingAndTowers();
            ritualText.text = Failed ? "仪式中断" : VictoryFinished ? "已获得圣林加护" : "加护进度  " + Mathf.RoundToInt(presentation.RitualProgress*100) + "%";
            pulseButton.interactable = presentation.CanReleasePulse && !Failed && !VictoryStarted;
            pulseText.text = VictoryStarted || Failed ? "净化脉冲 · 已结束" : presentation.StoredPulse > 0 ? "净化脉冲 · 已就绪" : "净化脉冲 · 充能中";
            int next = presentation.CompletedWaves < 2 ? 2 : presentation.CompletedWaves < 4 ? 4 : presentation.CompletedWaves < 6 ? 6 : 0;
            pulseHint.text = Failed ? "祭坛失守，可重新挑战" : VictoryFinished ? "水晶力量已传给勇者" : VictoryStarted ? "水晶力量转移中…" :
                presentation.CanReleasePulse ? "点击按钮或水晶：中央范围伤害并短暂减速" : next > 0 ? "守住第 " + next + " 波后补充一次，最多储存一次" : "守住最后一波，完成加护";
            if (Failed) hud.SetEncounterMessage("祭坛失守 · 仪式中断", "重新开始", true);
            else if (VictoryStarted && !VictoryFinished) hud.SetEncounterMessage("防守成功 · 正在完成加护", "仪式完成中…", false);
            else if (VictoryFinished) hud.SetEncounterMessage("获得圣林加护 · 防守成功", "重新开始", true);
        }
        private void FinishVictory() { if (Failed || !game.AllWavesFinished || game.Lives<=0) return; VictoryFinished=true; Refresh(); }
        private void OnDestroy()
        {
            if (game!=null) game.StateChanged-=Refresh;
            if (presentation!=null) { presentation.StateChanged-=Refresh; presentation.onPurificationPulse.RemoveListener(ApplyPulse); presentation.onBlessingCompleted.RemoveListener(FinishVictory); }
            if(pulseButton!=null)pulseButton.onClick.RemoveListener(ReleasePulse);
            if(routeButton!=null)routeButton.onClick.RemoveListener(ToggleRoutes);
        }
    }
}
