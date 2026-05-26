using System.Net.Http.Json;

namespace ProyectoProgra3.Services
{
    public class CementerioService
    {
        private readonly HttpClient _http;

        private readonly IConfiguration _config;

        public CementerioService(
            HttpClient http,
            IConfiguration config
        )
        {
            _http = http;

            _config = config;
        }

        public async Task<object?> ObtenerCuotas(
            int clienteId
        )
        {
            var url =
                _config["Servicios:Cementerio"];

            var respuesta =
                await _http.GetAsync(
                    $"{url}/api/cuotas/cliente/{clienteId}"
                );

            if (!respuesta.IsSuccessStatusCode)
            {
                return null;
            }

            return await respuesta
                .Content
                .ReadFromJsonAsync<object>();
        }

        public async Task<bool>

MarcarPagada(
    int cuotaId
)
        {
            try
            {
                var url =
                    _config[
                        "Servicios:Cementerio"
                    ];

                var response =
                    await _http
                    .PostAsJsonAsync(
                        $"{url}/api/cuotas/pagar",

                        new
                        {
                            cuotaId =
                                cuotaId,

                            metodoPago =
                                "App Banco"
                        }
                    );

                var texto =
                    await response
                    .Content
                    .ReadAsStringAsync();

                Console.WriteLine(
                    texto
                );

                return response
                    .IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    ex.Message
                );

                return false;
            }
        } }
    }
