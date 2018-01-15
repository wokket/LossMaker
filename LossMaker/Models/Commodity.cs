using Jil;

namespace LossMaker.Models
{
    public sealed class Commodity
    {
        [JilDirective(Name="id")]
        public int Id { get; set; }

        [JilDirective(Name = "name")]
        public string Name { get; set; }

    }
}
