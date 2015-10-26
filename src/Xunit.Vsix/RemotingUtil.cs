using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Services;

namespace Xunit
{
	class RemotingUtil
	{
		public static string HostName { get { return typeof (IVsRemoteRunner).FullName; } }

		static RemotingUtil()
		{
			TrackingServices.RegisterTrackingHandler (new RemoteTracker ());
		}

		public static string GetHostUri (string pipeName)
		{
			return "ipc://" + pipeName + "/" + HostName;
		}

		public static IChannel CreateChannel (string channelName, string pipeName)
		{
			var props = new Hashtable ();
			props["name"] = channelName;
			props["portName"] = pipeName;
            props["authorizedGroup"] = "Everyone";

			var serverProvider = new BinaryServerFormatterSinkProvider {
				TypeFilterLevel = System.Runtime.Serialization.Formatters.TypeFilterLevel.Full
			};

			var clientProvider = new BinaryClientFormatterSinkProvider {
			};

			var channel = new IpcChannel (props, clientProvider, serverProvider);

			ChannelServices.RegisterChannel (channel, false);

			return channel;
		}

		class RemoteTracker : ITrackingHandler
		{
			public void DisconnectedObject (object obj)
			{
			}

			public void MarshaledObject (object obj, ObjRef or)
			{
			}

			public void UnmarshaledObject (object obj, ObjRef or)
			{
			}
		}
	}
}
