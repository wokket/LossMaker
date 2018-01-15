using System.Diagnostics;

namespace LossMaker.Models
{

    [DebuggerDisplay("EddbSystem ({id}:{name})")]
    public class EddbSystem
    {
        public int? id { get; set; }
        public int? edsm_id { get; set; }
        public string name { get; set; }
        public float? x { get; set; }
        public float? y { get; set; }
        public float? z { get; set; }

        public float? distance { get; set; }

    }
}
