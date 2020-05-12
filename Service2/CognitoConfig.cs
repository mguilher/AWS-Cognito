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
