namespace AppPrediosDemo.Models
{
    public partial class TipoInforme
    {
        public byte IdTipoInforme { get; set; }
        public string NombreTipoInforme { get; set; } = null!;
        public virtual ICollection<ConceptoFinal> ConceptoFinals { get; set; } = new HashSet<ConceptoFinal>();

    }
}
