using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	class InterceptingMessageBus : LongLivedMarshalByRefObject, IMessageBus
	{
		List<Action<IMessageSinkMessage>> callbacks = new List<Action<IMessageSinkMessage>> ();
		IMessageBus innerBus;

		public InterceptingMessageBus (IMessageBus innerBus, params Action<IMessageSinkMessage>[] callbacks)
		{
			this.innerBus = innerBus;
			this.callbacks.AddRange (callbacks);
		}

		public void Dispose ()
		{
			try {
				innerBus.Dispose ();
			} catch (ObjectDisposedException) {
				// Others consuming the inner bus could dispose it before us.
			}
		}

		public void OnMessage (Action<IMessageSinkMessage> action)
		{
			callbacks.Add (action);
		}

		public bool QueueMessage (IMessageSinkMessage message)
		{
			var result = innerBus.QueueMessage (message);

			callbacks.ForEach (callback => callback (message));

			return result;
		}
	}
}
