using System.Collections.Generic;
using NUnit.Framework;
using PofudukFilo.Enemies;
using UnityEngine;

namespace PofudukFilo.Tests
{
    public sealed class DifficultyDirectorTests
    {
        [Test]
        public void TwoHitsInAMinute_EaseByTenPercent()
        {
            var dda = new DifficultyDirector();
            dda.Reset(0f);
            dda.RegisterPlayerHit(10f);
            dda.RegisterPlayerHit(30f);

            dda.Evaluate(31f);

            Assert.That(dda.Multiplier, Is.EqualTo(0.9f).Within(1e-5f));
        }

        [Test]
        public void Easing_IsRateLimited_AndFloorsAtMinus25Percent()
        {
            var dda = new DifficultyDirector();
            dda.Reset(0f);

            for (float t = 0f; t < 600f; t += 5f)
            {
                dda.RegisterPlayerHit(t);
                dda.Evaluate(t);
            }

            Assert.That(dda.Multiplier, Is.EqualTo(DifficultyDirector.Min).Within(1e-5f));

            var fresh = new DifficultyDirector();
            fresh.Reset(0f);
            fresh.RegisterPlayerHit(1f);
            fresh.RegisterPlayerHit(2f);
            fresh.Evaluate(3f);
            fresh.Evaluate(4f); // inside cooldown — no second step
            Assert.That(fresh.Multiplier, Is.EqualTo(0.9f).Within(1e-5f));
        }

        [Test]
        public void CleanPlay_HardensUpToPlus15Percent()
        {
            var dda = new DifficultyDirector();
            dda.Reset(0f);

            dda.Evaluate(89f);
            Assert.That(dda.Multiplier, Is.EqualTo(1f));

            for (float t = 90f; t < 1000f; t += 1f) dda.Evaluate(t);
            Assert.That(dda.Multiplier, Is.EqualTo(DifficultyDirector.Max).Within(1e-5f));
        }

        [Test]
        public void OldHits_FallOutOfTheWindow()
        {
            var dda = new DifficultyDirector();
            dda.Reset(0f);
            dda.RegisterPlayerHit(0f);
            dda.RegisterPlayerHit(1f);

            dda.Evaluate(62f); // both hits older than 60 s, and not yet 90 s clean

            Assert.That(dda.Multiplier, Is.EqualTo(1f));
        }
    }

    public sealed class FormationTests
    {
        [Test]
        public void Group_ClearedOnlyWhenAllMembersKilled()
        {
            var group = new FormationGroup(Vector2.zero);
            group.AddMember();
            group.AddMember();
            int cleared = 0, finished = 0;
            group.Cleared += _ => cleared++;
            group.Finished += _ => finished++;

            group.OnMemberKilled();
            Assert.That(cleared, Is.EqualTo(0));
            group.OnMemberKilled();

            Assert.That(cleared, Is.EqualTo(1));
            Assert.That(finished, Is.EqualTo(1));
        }

        [Test]
        public void Group_LosingAMember_ForfeitsBonusButStillFinishes()
        {
            var group = new FormationGroup(Vector2.zero);
            group.AddMember();
            group.AddMember();
            int cleared = 0, finished = 0;
            group.Cleared += _ => cleared++;
            group.Finished += _ => finished++;

            group.OnMemberLost();
            group.OnMemberKilled();

            Assert.That(cleared, Is.EqualTo(0));
            Assert.That(finished, Is.EqualTo(1));
        }

        [TestCase(FormationShape.Grid, 3, 5, 15)]
        [TestCase(FormationShape.Line, 3, 5, 5)]
        [TestCase(FormationShape.V, 1, 7, 7)]
        [TestCase(FormationShape.Arc, 1, 6, 6)]
        public void Layout_ProducesExpectedSlotCount_CenteredOnX(FormationShape shape, int rows, int cols, int expected)
        {
            var offsets = new List<Vector2>();
            FormationLayout.Build(shape, rows, cols, 0.8f, offsets);

            Assert.That(offsets.Count, Is.EqualTo(expected));
            float sumX = 0f;
            foreach (Vector2 o in offsets) sumX += o.x;
            Assert.That(sumX / offsets.Count, Is.EqualTo(0f).Within(1e-4f));
        }
    }

    public sealed class RunDefinitionTests
    {
        [Test]
        public void PhaseIndexAt_FollowsTimeline()
        {
            var run = ScriptableObject.CreateInstance<RunDefinition>();
            run.phases = new[]
            {
                new RunPhase { startMinute = 0f },
                new RunPhase { startMinute = 3f, kind = PhaseKind.MiniBoss },
                new RunPhase { startMinute = 3.2f },
                new RunPhase { startMinute = 8f, kind = PhaseKind.FinalBoss }
            };

            Assert.That(run.PhaseIndexAt(0f), Is.EqualTo(0));
            Assert.That(run.PhaseIndexAt(2.99f), Is.EqualTo(0));
            Assert.That(run.PhaseIndexAt(3f), Is.EqualTo(1));
            Assert.That(run.PhaseIndexAt(5f), Is.EqualTo(2));
            Assert.That(run.PhaseIndexAt(12f), Is.EqualTo(3));

            Object.DestroyImmediate(run);
        }
    }
}
