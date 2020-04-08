using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
namespace ISMP_ClientMod.Requests
{

    [ProtoContract]
    public class MergedListActionRequest : RemoteRequest
    {
        [ProtoMember(1)]
        public ListUpdateAction MergedListAction = ListUpdateAction.ADD;
        [ProtoMember(2)]
        public Dictionary<long, string> WhiteList = new Dictionary<long, string>();

        public MergedListActionRequest() { }

        public MergedListActionRequest(ulong sender, ListUpdateAction action, Dictionary<long,string> whitelist) : base(sender, RemoteRequestType.MERGEDLIST_ACTION)
        {
            WhiteList = whitelist;
            MergedListAction = action;
        }
    }
    public enum ListUpdateAction : byte { ADD, REMOVE }
}
