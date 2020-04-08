using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISMP_ClientMod.Conf
{
    public static class Config
    {
        public static Guid GUID { get; } = new Guid("796E31C4-C9A3-49A6-BB36-8DD41403DDD4");
        public const ushort MessageHandlerID = 44595;

        public static KeyValuePair<long, string> NOSCRIPT { get; } = new KeyValuePair<long, string>(-1L, "NONE");
    }
}
