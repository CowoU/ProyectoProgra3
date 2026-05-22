using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoProgra3.Models
{
    /// <summary>
    /// Modelo que representa una Cuota que debe pagar un usuario
    /// (Ej: Cuota de mantenimiento del cementerio, cuota de condominio)
    /// FASE 1: Datos en memoria | FASE 2: Conectado a tabla Cuotas de MySQL
    /// </summary>
    [Table("Cuotas")]
    public class Cuota
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("usuario_id")]
        public int IdUsuario { get; set; }

        [Column("empresa_id")]
        public int IdEmpresa { get; set; }

        [Column("mes")]
        public string Mes { get; set; }

        [Column("monto")]
        public decimal Monto { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        [Column("fecha_vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        [Column("mora")]
        public decimal Mora { get; set; } = 0m;
    }
}
