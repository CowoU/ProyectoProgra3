using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoProgra3.Models
{
    /// <summary>
    /// Modelo que representa una Empresa prestadora de servicios (Cementerio o Condominio)
    /// Conectado a tabla Empresas de MySQL
    /// </summary>
    [Table("Empresas")]
    public class Empresa
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; }

        [Column("saldo_acumulado")]
        public decimal SaldoAcumulado { get; set; }
    }
}
