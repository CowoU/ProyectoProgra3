namespace ProyectoProgra3.DTOs
{
    public class TransaccionResponseDto
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public TransaccionDetalleDto Transaccion { get; set; }
    }

    public class TransaccionDetalleDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Concepto { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal ComisionBanco { get; set; }
        public decimal PagoEmpresa { get; set; }
        public DateTime Fecha { get; set; }
    }
}
