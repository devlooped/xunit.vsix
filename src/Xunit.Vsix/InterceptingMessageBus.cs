using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	internal class InterceptingMessageBus : IMessageBus
	{
		List<IMessageSinkMessage> messages = new List<IMessageSinkMessage>();
		IMessageBus innerBus;

		public InterceptingMessageBus (IMessageBus innerBus = null)
		{
			this.innerBus = innerBus ?? NullMessageBus.Instance;
		}

		public IMessageBus InnerBus { get; private set; }

		public IEnumerable<IMessageSinkMessage> Messages { get { return messages; } }

		public bool QueueMessage (IMessageSinkMessage message)
		{
			messages.Add (message);
			return innerBus.QueueMessage (message);
		}

		public void Dispose ()
		{
			innerBus.Dispose ();
		}

		class NullMessageBus : IMessageBus
		{
			public static IMessageBus Instance { get; private set; }

			static NullMessageBus () { Instance = new NullMessageBus (); }

			private NullMessageBus () { }

			public void Dispose ()
			{
			}

			public bool QueueMessage (IMessageSinkMessage message)
			{
				return true;
			}
		}
	}
}
