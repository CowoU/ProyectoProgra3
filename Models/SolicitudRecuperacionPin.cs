using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoProgra3.Models
{
    [Table("solicitudes_recuperacion_pin")]
    public class SolicitudRecuperacionPin
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("id_usuario")]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }

        [Required]
        [Column("fecha_solicitud")]
        public DateTime FechaSolicitud { get; set; }

        [Required]
        [Column("procesada")]
        public bool Procesada { get; set; }
    }
}
