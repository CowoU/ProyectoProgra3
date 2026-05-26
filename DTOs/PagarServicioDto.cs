namespace ProyectoProgra3.DTOs
{
    public class PagarServicioDto
    {
        public int UsuarioId { get; set; }

        public int CuotaId { get; set; }

        public decimal Monto { get; set; }

        public string Concepto { get; set; }
            = "Cementerio";
    }
}