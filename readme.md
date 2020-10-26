
`/app/Properties/launchSettings.json`
```json
{
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "http://localhost:49677/",
      "sslPort": 44340
    }
  },
  "profiles": {
    "app": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "MockExternalSystems": "true",
        "ClientSecret": "*****",
        "OpenIdConnect:ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
        "OpenIdConnect:TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
        "OpenIdConnect:Instance": "https://login.microsoftonline.com/",
        "OpenIdConnect:BaseUrl": "https://localhost:5001",
        "OpenIdConnect:CallbackPath": "/signin-oidc"
      },
      "applicationUrl": "https://localhost:5001;http://localhost:5000"
    }
  }
}
```