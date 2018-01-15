using Jil;
using System.Diagnostics;

namespace LossMaker.Models
{
    [DebuggerDisplay("EDSMSystem ({Id}:{Name})")]
    public class EdsmSystem
    {
        [JilDirective(Name="id")]
        public int Id { get; set; }

        [JilDirective(Name = "name")]
        public string Name { get; set; }

        [JilDirective(Name = "coords")]
        public CoOrds Coords { get; set; }

        [JilDirective(Name = "distance")]
        public float? Distance { get; set; }

    }

    public class CoOrds
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}
