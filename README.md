# JWT "Machine to Machine" usando AWS Cognito and C#

Muitas vezes você necessita publicar seus serviços/APIs na internet e para mitigar o uso indevido, é necessário usar um método de autenticação. Se você já faz uso da estrutura da nuvem da AWS, uma opção é fazer uso do Cognito é um serviço de identidade voltado a administração de Grupos de Usuário porem também pode ser utilizados facilmente entre APIs. Então vamos configurar e fazer um exemplo em .Net Core 3.1 C#.


![IMG](images\0.jpg)

## Configuração

No Console da AWS procure pelo Cognito ao acessar escolha Gerenciar Grupos de Usuários conforme a imagem.

![IMG1](images\1.png)

Em seguida em Criar um Grupo de Usuários  

![IMG2](images\2.png)

Especifique um nome para o Grupo 

![IMG3](images\3.png)

E em seguida em Revisar Padrões e Adicionar cliente de aplicativo 

![IMG4](images\4.png)

E em Especifique um nome para o cliente de aplicativo 

![IMG5](images\5.png)

Utilize o botão Criar aplicativo de cliente em seguida salve o grupo
 
![IMG6](images\6.png)

Var em Servidores de recursos (barra lateral esquerda inferior) e Adicione um novo servidor 

![IMG7](images\7.png)

Preencha como na imagem lembrando que a combinação identificador/scopo sera utilizada na geração do token 

![IMG8](images\8.png)

Agora vá em Configuração do cliente do aplicativo e habilite o acesso por credenciais e o scopo configurado no servidor de recursos, conforme na imagem

![IMG9](images\9.png)

Então vá em Integração do aplicativo - Nome do domínio e configure um domínio para gerar uma url para obtenção do token  

![IMG10](images\10.png)

Agora é só resgatar as configurações, AppClientId e AppClientSecret ficam na tela de Ciente de aplicativo (acessível pela barra lateral esquerda)

![IMG11](images\11.png)

PoolId é o Id do grupo na tela Configurações gerais

![IMG12](images\12.png)

## Criar o código de exemplo 

Para o efeito de teste vamos criar um API e um console a partir do exemplo gerado pole Dotnet CLI, onde a API usar o JWT como autenticação e o console gera um token e faz a chamada de exemplo


### Vamos criar uma API de teste 

Abra um Pronpt de Comando (Ou terminar de sua escolha)

```cmd
mkdir Service1
cd Service1
dotnet new webapi
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 3.1.3

```
![IMG13](images\13.png)


Abra o VisualStudio Code (ou Editor de sua  preferência )

```cmd
code ...
```
![IMG14](images\14.png)

No arquivo de configurações (appsettings.json) adicione as seguintes chaves para a configuração da validação de token JWT pelo Cognito, com os valores anteriormente recuperados na configuração no console AWS

```json
"CognitoConfig": {
    "Region": "us-east-1",
    "PoolId": "us-east-1_zkrVREg",
    "AppClientId": "4vpujju640gqge78e9l8"
  }
```

![IMG15](images\15.png)

Na sequencia altere o Startup para incluir o seguinte código de validação JWT e configuração de autorização 

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Net;
```
No método ConfigureServices inclua
```csharp
    void ConfigureAuthenticationOptions(AuthenticationOptions options)
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }

    void ConfigureJwtBearerOptions(JwtBearerOptions options)
    {
        var Region = Configuration["CognitoConfig:Region"];
        var PoolId = Configuration["CognitoConfig:PoolId"];
        var AppClientId = Configuration["CognitoConfig:AppClientId"];
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (s, securityToken, identifier, parameters) =>
            {
                // Get JsonWebKeySet from AWS
                var json = new WebClient().DownloadString(parameters.ValidIssuer + "/.well-known/jwks.json");
                // Serialize the result
                return JsonConvert.DeserializeObject<JsonWebKeySet>(json).Keys;
            },
            ValidateIssuer = true,
            ValidIssuer = $"https://cognito-idp.{Region}.amazonaws.com/{PoolId}",
            ValidateLifetime = true,
            LifetimeValidator = (before, expires, token, param) => expires > DateTime.UtcNow,
            ValidateAudience = false,
            ValidAudience = AppClientId,
        };

    }

    services.AddAuthorization(auth =>
    {
        auth.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser().Build());
    });

    services.AddCors()
            .AddAuthentication(ConfigureAuthenticationOptions)
            .AddJwtBearer(ConfigureJwtBearerOptions);
```
No método Configure

```csharp
        app.UseAuthentication();

        app.UseAuthorization();
```


![IMG16](images\16.png)

![IMG17](images\17.png)

![IMG17](images\18.png)

Depois altere o controller gerado pelo exemplo WeatherForecastController, incluindo o atributo de autorização na classe

```csharp
using Microsoft.AspNetCore.Authorization;
```
```csharp
[Authorize]
```
![IMG19](images\19.png)

### Vamos criar o console de teste 

Abra um novo Pronpt de Comando incluído os pacotes necessários para a configuração em arquivo Json

```cmd
mkdir Service2
cd Service2
dotnet new console
dotnet add package Microsoft.Extensions.Configuration --version 3.1.3
dotnet add package Microsoft.Extensions.Configuration.FileExtensions --version 3.1.3
dotnet add package Microsoft.Extensions.Configuration.Json --version 3.1.3
dotnet add package Microsoft.Extensions.Configuration.Binder --version 3.1.3
dotnet add package Newtonsoft.Json --version 12.0.3
```
![IMG20](images\20.png)

![IMG21](images\21.png)


Abra o VisualStudio Code

![IMG22](images\22.png)

Adicione um novo arquivo CognitoConfig.cs para a classe de configuração das informações do AWS Cognito

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace Service2
{
    public class CognitoConfig
    {
        public string Region { get; set; }
        public string AuthDomain { get; set; }
        public string AppClientId { get; set; }
        public string AppClientSecret { get; set; }
        public string Scopes { get; set; }
        public string PoolId { get; set; }

        public string GetAuthUrlAuthDomain => $"https://{AuthDomain}.auth.{Region}.amazoncognito.com/oauth2/token";

        public string GetAuthUrlJwks => $"https://cognito-idp.{Region}.amazonaws.com/{PoolId}/.well-known/jwks.json";
    }
}

```

![IMG23](images\23.png)

Adicione o novo arquivo de configurações (appsettings.json) conforme a estrutura da classe acima , com os valores anteriormente recuperados na configuração no console AWS

```json
{
    "CognitoConfig": {
      "Region": "us-east-1",
      "PoolId": "us-east-1_zkrVREg",
      "AppClientId": "4vpujju640gqge78e9l8",
      "AppClientSecret": "19ed5f09q2eqlt3v38o5il6",
      "Scopes": "ResourceAuthorization/API_ACCESS",
      "AuthDomain": "mgapp"
    }
}
```
![IMG24](images\24.png)

Na sequência vamos trazer a classe WeatherForecast para traduzir o resultado de consulta a API de exemplo 

```csharp
        using System;
        using System.Collections.Generic;
        using System.Text;

        namespace Service2
        {
            public class WeatherForecast
            {
                public DateTime Date { get; set; }

                public int TemperatureC { get; set; }

                public int TemperatureF { get; set; }

                public string Summary { get; set; }
            }
        }
```

![IMG25](images\25.png)

Agora vamos alterar o Program.cs para incluir o código que lê as configurações gera um token JWT no serviço do AWS Cognito e faz um get de exemplo na nossa API de teste  usando o token para autenticar 

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
```
No corpo do Main 

```csharp
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
```

Método para gerar o token infonando "grant_type" como "client_credentials" o scopo incluido com recurso e autorizado na configuração do cliente de aplicativo "ResourceAuthorization/API_ACCESS" usando o "Client Id" e "Client Secret" para autenticar no serviço do Cognito

```csharp
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
```
![IMG26](images\26.png)

![IMG27](images\27.png)

Agora precisamos alterar o arquivo de projeto Service2.csproj, para incluir o arquivo de configuração na saída do build 

```xml
        <ItemGroup>
            <Content Include="appsettings.json">
                <CopyToOutputDirectory>Always</CopyToOutputDirectory>
            </Content>
        </ItemGroup>
```

![IMG28](images\28.png)

## Teste

### No primiero Pronpt de Comando execute a API

```cmd
dotnet run
```
![IMG29](images\29.png)


### No segundo Pronpt de Comando execute o Console

```cmd
dotnet run
```
![IMG30](images\30.png)



>[!WARNING]
>No meu caso meu caso para usar o SSL local para a API de teste então incluir o codigo para ignorar a validação do certificado digital 
```csharp
    using var httpClientHandler = new HttpClientHandler();
    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
    using var httpClient = new HttpClient(httpClientHandler);
```


## Referências

* [https://aws.amazon.com/pt/blogs/mobile/understanding-amazon-cognito-user-pool-oauth-2-0-grants/](https://aws.amazon.com/pt/blogs/mobile/understanding-amazon-cognito-user-pool-oauth-2-0-grants/)
* [https://docs.aws.amazon.com/pt_br/cognito/latest/developerguide/token-endpoint.html](https://docs.aws.amazon.com/pt_br/cognito/latest/developerguide/token-endpoint.html)
* [https://docs.aws.amazon.com/pt_br/cognito/latest/developerguide/token-endpoint.html](https://docs.aws.amazon.com/pt_br/cognito/latest/developerguide/token-endpoint.html)

