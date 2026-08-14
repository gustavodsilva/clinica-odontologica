using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicaOdontologica.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentMethodFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"PaymentMethods\" SET \"RequiresBrand\" = false, \"RequiresInstallments\" = false WHERE \"Name\" = 'Dinheiro'");
            migrationBuilder.Sql("UPDATE \"PaymentMethods\" SET \"RequiresBrand\" = false, \"RequiresInstallments\" = false WHERE \"Name\" = 'Pix'");
            migrationBuilder.Sql("UPDATE \"PaymentMethods\" SET \"RequiresBrand\" = false, \"RequiresInstallments\" = false WHERE \"Name\" = 'Boleto'");
            migrationBuilder.Sql("UPDATE \"PaymentMethods\" SET \"RequiresBrand\" = true, \"RequiresInstallments\" = false WHERE \"Name\" = 'Cartão de Débito'");
            migrationBuilder.Sql("UPDATE \"PaymentMethods\" SET \"RequiresBrand\" = true, \"RequiresInstallments\" = true WHERE \"Name\" = 'Cartão de Crédito'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
