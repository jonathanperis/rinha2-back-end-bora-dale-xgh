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

        builder.Property(c => c.Valor)
            .IsRequired();

        builder.Property(c => c.ClienteId)
            .IsRequired();

        builder.Property(c => c.Tipo)
            .IsRequired();

        builder.Property(c => c.Descricao)
            .IsRequired();

        builder.Property(c => c.RealizadoEm)
            .IsRequired();

        if (Transacoes != null)
        {
            builder.HasData(Transacoes);
        }
    }
}