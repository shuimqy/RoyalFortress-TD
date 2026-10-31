using UnityEngine;

namespace RoyalFortress.ForestSanctuary
{
    /// <summary>Optional adapter for the existing eight-wave combat controller once paths are approved.</summary>
    public sealed class ForestSanctuaryEncounter : MonoBehaviour
    {
        public WaveGameController game;
        public ForestSanctuaryPresentation presentation;
        private bool subscribed;
        private void Start()
        {
            if (game == null || presentation == null) return;
            if (game.TotalWaves != ForestSanctuaryPresentation.TotalWaves)
            {
                Debug.LogError("Forest Sanctuary requires exactly eight configured waves.", this); return;
            }
            presentation.startingLives = game.startingLives;
            presentation.ResetPresentation();
            presentation.simulationControlsEnabled = false;
            game.StateChanged += Refresh; subscribed = true; Refresh();
        }
        private void Refresh()
        {
            presentation.ReportHomeLives(game.Lives);
            if (game.IsGameOver || game.Lives <= 0) return;
            // Do not grant a charge when the wave starts or while spawning/enemies remain.
            if (game.IsWaveActive || game.IsSpawning || game.AliveCount > 0) return;
            while (presentation.CompletedWaves < game.CompletedWaves)
                if (!presentation.NotifyWaveCleared(presentation.CompletedWaves + 1, game.Lives)) break;
        }
        private void OnDestroy() { if (subscribed && game != null) game.StateChanged -= Refresh; }
    }
}
