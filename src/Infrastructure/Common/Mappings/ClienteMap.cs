namespace Infrastructure.Common.Mappings;

internal class ClienteMap : IEntityTypeConfiguration<Cliente>
{
    internal static List<Cliente>? Clientes { get; set; }

    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder
            .ToTable("Clientes", "public");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        if (Clientes != null)
        {
            builder.HasData(Clientes);
        }
    }
}