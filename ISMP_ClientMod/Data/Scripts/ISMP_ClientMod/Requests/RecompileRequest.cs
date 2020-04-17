using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using ProtoBuf;

namespace ISMP_ClientMod.Requests
{
    [ProtoContract]
    public class RecompileRequest : RemoteRequest
    {
        [ProtoMember(1)]
        public long PbId = 0;
        [ProtoMember(2)]
        [DefaultValue(-1)]
        public long ScriptId = -1;

        public RecompileRequest() { }

        public RecompileRequest(ulong sender, long programmableBlock, long scriptId) : base(sender, RemoteRequestType.RECOMPILE)
        {
            PbId = programmableBlock;
            ScriptId = scriptId;
        }
    }
}
