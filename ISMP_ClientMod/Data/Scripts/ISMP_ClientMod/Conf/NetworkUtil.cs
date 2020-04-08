using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISMP_ClientMod.Requests;
using Sandbox.ModAPI;

namespace ISMP_ClientMod.Conf
{
    public static class NetworkUtil
    {
        public static void SendToClient(ulong recip, RemoteRequest payload)
        {
            var bytes = MyAPIGateway.Utilities.SerializeToBinary(payload);
            MyAPIGateway.Multiplayer.SendMessageTo(Config.MessageHandlerID, bytes, recip);
        }
        public static RemoteRequest DeserializeRequest(byte[] bytes)
        {
            var request = MyAPIGateway.Utilities.SerializeFromBinary<RemoteRequest>(bytes);

            if (request == null)
                throw new Exception("Invalid packet data receeveid Could not parse Remoterequest");

            return request;
        }
    }
}
