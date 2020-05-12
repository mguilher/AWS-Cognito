using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Service2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello AWS Cognito!");
            var configuration = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            var config = configuration.GetSection("CognitoConfig").Get<CognitoConfig>();

            try
            {
                var token = GetToken(config);

                using var httpClientHandler = new HttpClientHandler();
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
                using var httpClient = new HttpClient(httpClientHandler);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.Item1, token.Item2);
                HttpResponseMessage result = httpClient.GetAsync("https://localhost:5001/weatherforecast").GetAwaiter().GetResult();
                string json = result.Content.ReadAsStringAsync().Result;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(json);
                result.EnsureSuccessStatusCode();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                var forecasts = Newtonsoft.Json.JsonConvert.DeserializeObject<List<WeatherForecast>>(json);
                foreach (WeatherForecast forecast in forecasts)
                {
                    Console.WriteLine($"{forecast.Date} - {forecast.TemperatureC} - {forecast.TemperatureF} - {forecast.Summary}");
                }
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        public static Tuple<string, string> GetToken(CognitoConfig config)
        {
            var url = config.GetAuthUrlAuthDomain;

            Console.WriteLine(url);
            var form = new Dictionary<string, string>
            {
                {"grant_type", "client_credentials"},
            };

            if (!string.IsNullOrWhiteSpace(config.Scopes))
            {
                form.Add("scope", config.Scopes);
            }

            using var httpClient = new HttpClient();

            var auth = Convert.ToBase64String(System.Text.Encoding.Default.GetBytes($"{config.AppClientId}:{config.AppClientSecret}"));
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(auth);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

            HttpResponseMessage result = httpClient.PostAsync(url, new FormUrlEncodedContent(form)).Result;

            string json = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(json);

            result.EnsureSuccessStatusCode();

            var data = Newtonsoft.Json.Linq.JObject.Parse(json);

            return new Tuple<string, string>(data["token_type"].ToString(), data["access_token"].ToString());
        }
    }
}
