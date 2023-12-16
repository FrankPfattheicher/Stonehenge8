namespace IctBaden.Stonehenge.Hosting;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed record KeycloakAuthenticationOptions
{
    /// Client ID used to authenticate 
    public string ClientId { get; set; } = string.Empty;
    /// The Keycloak realm 
    public string Realm { get; set; } = string.Empty;
    /// Keycloak auth url
    /// https://my.Keycloak.com/auth 
    public string AuthUrl { get; set; } = string.Empty;

}