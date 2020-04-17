using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
namespace ISMP_ClientMod.Requests
{

    [ProtoContract]
    public class WhiteListActionRequest : RemoteRequest
    {
        [ProtoMember(3)]
        public ListUpdateAction WhiteListAction = ListUpdateAction.ADD;

        [ProtoMember(4)]
        public Dictionary<long, string> WhiteList = new Dictionary<long, string>();

        public WhiteListActionRequest() {}

        public WhiteListActionRequest(ulong sender, ListUpdateAction action, Dictionary<long, string> whitelist) : base(sender, RemoteRequestType.WHITELIST_ACTION)
        {
            WhiteList = whitelist;
            WhiteListAction = action;
        }
    }
    public enum ListUpdateAction : byte { ADD, REMOVE}
}
