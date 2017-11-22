using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    internal class RemoteMessageBus : LongLivedMarshalByRefObject, IMessageBus
    {
        private IMessageBus _localMessageBus;

        public RemoteMessageBus(IMessageBus localMessageBus)
        {
            _localMessageBus = localMessageBus;
        }

        public void Dispose()
        {
            _localMessageBus.Dispose();
        }

        public bool QueueMessage(IMessageSinkMessage message)
        {
            return _localMessageBus.QueueMessage(message);
        }
    }
}
