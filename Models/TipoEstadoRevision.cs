namespace AppPrediosDemo.Models
{
    public partial class TipoEstadoRevision
    {
        public byte IdTipoEstadoRevision { get; set; }
        public string NombreTipoEstadoRevision { get; set; } = null!;
        public virtual ICollection<ConceptoFinal> ConceptoFinals { get; set; } = new HashSet<ConceptoFinal>();

    }
}
