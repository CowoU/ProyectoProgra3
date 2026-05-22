using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoProgra3.Models
{
    [Table("solicitudes_recuperacion_pin")]
    public class SolicitudRecuperacionPin
    {
        [Key]
        public int Id { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("nombre_usuario")]
        public string NombreUsuario { get; set; }

        [Column("fecha_solicitud")]
        public DateTime FechaSolicitud { get; set; }

        [Column("procesada")]
        public bool Procesada { get; set; }
    }
}
