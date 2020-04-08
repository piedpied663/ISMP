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
    [ProtoInclude(2, typeof(MergedListActionRequest))]
    public class RemoteRequest
    {
        [ProtoMember(1)]
        private RemoteRequestType requestType;

        [ProtoMember(2)]
        public ulong Sender = 0;

        public RemoteRequestType RequestType { get => requestType; set => requestType = value; }

        public RemoteRequest() { }

        public RemoteRequest(ulong sender, RemoteRequestType type)
        {
            Sender = sender;
            RequestType = type;
        }

    }
    public enum RemoteRequestType : byte { RECOMPILE, MERGEDLIST_ACTION, CLIENT_REGISTRATION}
}
