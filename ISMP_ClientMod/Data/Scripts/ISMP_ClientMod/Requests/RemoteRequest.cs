using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
namespace ISMP_ClientMod.Requests
{
    [ProtoContract]
    [ProtoInclude(1, typeof(RecompileRequest))]
    [ProtoInclude(2, typeof(WhiteListActionRequest))]
    public class RemoteRequest
    {
        [ProtoMember(5)]
        public RemoteRequestType RequestType;

        [ProtoMember(6)]
        public ulong Sender = 0;

        public RemoteRequest() { }

        public RemoteRequest(ulong sender, RemoteRequestType type)
        {
            Sender = sender;
            RequestType = type;
        }

    }
    public enum RemoteRequestType : byte { RECOMPILE, WHITELIST_ACTION, CLIENT_REGISTRATION }
}
