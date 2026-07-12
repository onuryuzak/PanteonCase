using NUnit.Framework;
using Panteon.Core;

namespace Panteon.Tests
{
    public sealed class EventBusTests
    {
        private readonly struct TestMessage
        {
            public readonly int Value;
            public TestMessage(int value) => Value = value;
        }

        [Test]
        public void Unsubscribe_RemovesTheOriginalTypedHandler()
        {
            var bus = new EventBus();
            var received = 0;
            System.Action<TestMessage> handler = message => received += message.Value;

            bus.Subscribe(handler);
            bus.Publish(new TestMessage(3));
            bus.Unsubscribe(handler);
            bus.Publish(new TestMessage(5));

            Assert.That(received, Is.EqualTo(3));
        }
    }
}
