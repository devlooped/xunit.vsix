using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	class RemoteMessageBus : LongLivedMarshalByRefObject, IMessageBus
	{
		IMessageBus localMessageBus;

		public RemoteMessageBus (IMessageBus localMessageBus)
		{
			this.localMessageBus = localMessageBus;
		}

		public void Dispose ()
		{
			localMessageBus.Dispose ();
		}

		public bool QueueMessage (IMessageSinkMessage message)
		{
			return localMessageBus.QueueMessage (message);
		}
	}
}
