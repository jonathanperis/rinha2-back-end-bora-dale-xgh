namespace Infrastructure.Common.Mappings;

internal class TransacaoMap : IEntityTypeConfiguration<Transacao>
{
    internal static List<Transacao>? Transacoes { get; set; }

    public void Configure(EntityTypeBuilder<Transacao> builder)
    {
        builder
            .ToTable("Transacoes", "public");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        if (Transacoes != null)
        {
            builder.HasData(Transacoes);
        }
    }
}