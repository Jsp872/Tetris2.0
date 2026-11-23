using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Tetris.Events;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tetris.Tests {
    public class EventBusTests {
        readonly struct TestEvent : IEvent {
            public readonly int Value;

            public TestEvent(int value) {
                Value = value;
            }
        }

        readonly struct OtherEvent : IEvent { }

        [TearDown]
        public void TearDown() {
            EventBus<TestEvent>.Clear();
            EventBus<OtherEvent>.Clear();
        }

        [Test]
        public void Publish_CallsEverySubscriberWithEvent() {
            var received = new List<int>();
            EventBus<TestEvent>.Subscribe(e => received.Add(e.Value));
            EventBus<TestEvent>.Subscribe(e => received.Add(e.Value * 10));

            EventBus<TestEvent>.Publish(new TestEvent(2));

            Assert.That(received, Is.EqualTo(new[] { 2, 20 }));
        }

        [Test]
        public void Publish_DoesNotReachOtherEventTypes() {
            bool called = false;
            EventBus<OtherEvent>.Subscribe(_ => called = true);

            EventBus<TestEvent>.Publish(new TestEvent(1));

            Assert.That(called, Is.False);
        }

        [Test]
        public void Subscribe_SameHandlerTwice_IsCalledOnce() {
            int count = 0;
            Action<TestEvent> handler = _ => count++;
            EventBus<TestEvent>.Subscribe(handler);
            EventBus<TestEvent>.Subscribe(handler);

            EventBus<TestEvent>.Publish(default);

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Unsubscribe_StopsReceivingEvents() {
            int count = 0;
            Action<TestEvent> handler = _ => count++;
            EventBus<TestEvent>.Subscribe(handler);
            EventBus<TestEvent>.Unsubscribe(handler);

            EventBus<TestEvent>.Publish(default);

            Assert.That(count, Is.Zero);
            Assert.That(EventBus<TestEvent>.SubscriberCount, Is.Zero);
        }

        [Test]
        public void Unsubscribe_DuringPublish_AppliesFromNextPublish() {
            int count = 0;
            Action<TestEvent> handler = null;
            handler = _ => {
                count++;
                EventBus<TestEvent>.Unsubscribe(handler);
            };
            EventBus<TestEvent>.Subscribe(handler);

            EventBus<TestEvent>.Publish(default);
            EventBus<TestEvent>.Publish(default);

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Publish_HandlerThrows_OtherHandlersStillCalled() {
            bool secondCalled = false;
            EventBus<TestEvent>.Subscribe(_ => throw new InvalidOperationException("boom"));
            EventBus<TestEvent>.Subscribe(_ => secondCalled = true);

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            EventBus<TestEvent>.Publish(default);

            Assert.That(secondCalled, Is.True);
        }

        [Test]
        public void ClearAll_RemovesSubscribersOfEveryChannel() {
            EventBus<TestEvent>.Subscribe(_ => { });
            EventBus<OtherEvent>.Subscribe(_ => { });

            EventBusRegistry.ClearAll();

            Assert.That(EventBus<TestEvent>.SubscriberCount, Is.Zero);
            Assert.That(EventBus<OtherEvent>.SubscriberCount, Is.Zero);
        }
    }
}
