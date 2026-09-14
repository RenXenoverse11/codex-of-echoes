using System.Collections;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Game.Battle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CodexOfEchoes.Tests
{
    /// <summary>
    /// Seam checks: the scene loads, the engine is constructed, commands reach it, and
    /// the presenter drains without exceptions. Combat rules are verified in Tests.Core.
    /// </summary>
    public class BattleSeamTests
    {
        [UnityTest]
        public IEnumerator BattleSceneLoadsAndBuildsAnEngine()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);

            var runner = Object.FindFirstObjectByType<BattleRunner>();

            Assert.That(runner, Is.Not.Null, "No BattleRunner in the Battle scene.");
            Assert.That(runner.Engine, Is.Not.Null);
            Assert.That(runner.Engine.State.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(runner.Engine.State.Grid.Tiles.Count, Is.EqualTo(16));
        }

        [UnityTest]
        public IEnumerator SubmittingACommandReachesTheEngine()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            var runner = Object.FindFirstObjectByType<BattleRunner>();

            runner.Submit(new SelectTileCommand(0));
            yield return null;

            Assert.That(runner.Engine.State.Selection, Is.EqualTo(new[] { 0 }));
        }

        [UnityTest]
        public IEnumerator ThePresenterDrainsAndReleasesInput()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            var runner = Object.FindFirstObjectByType<BattleRunner>();

            runner.Submit(new ScrambleCommand());

            // Drain the queue; the longest beat is well under two seconds.
            yield return new WaitForSeconds(3f);

            Assert.That(runner.IsBusy, Is.False, "Presenter never finished draining.");
            Assert.That(runner.Engine.State.TurnNumber, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator InputIsRefusedWhileAnimating()
        {
            yield return SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            var runner = Object.FindFirstObjectByType<BattleRunner>();

            runner.Submit(new ScrambleCommand());
            yield return null;

            var turnDuringAnimation = runner.Engine.State.TurnNumber;
            runner.Submit(new ScrambleCommand());

            Assert.That(runner.Engine.State.TurnNumber, Is.EqualTo(turnDuringAnimation));
        }
    }
}
