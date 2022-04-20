using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    class InterceptingMessageBus : IMessageBus
    {
        List<IMessageSinkMessage> _messages = new List<IMessageSinkMessage>();
        IMessageBus _innerBus;

        public InterceptingMessageBus(IMessageBus innerBus = null)
        {
            _innerBus = innerBus ?? NullMessageBus.Instance;
        }

        public IMessageBus InnerBus { get; private set; }

        public IEnumerable<IMessageSinkMessage> Messages { get { return _messages; } }

        public bool QueueMessage(IMessageSinkMessage message)
        {
            _messages.Add(message);
            return _innerBus.QueueMessage(message);
        }

        public void Dispose()
        {
            _innerBus.Dispose();
        }

        class NullMessageBus : IMessageBus
        {
            public static IMessageBus Instance { get; private set; }

            static NullMessageBus() { Instance = new NullMessageBus(); }

            NullMessageBus() { }

            public void Dispose()
            {
            }

            public bool QueueMessage(IMessageSinkMessage message)
            {
                return true;
            }
        }
    }
}
