
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Fido2NetLib;
using Fido2NetLib.Objects;

namespace Calibre_net.Data;

[Table("UserCredentials")]
public class UserCredential
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }


    /// <summary>
    /// The Credential ID of the public key credential source.
    /// </summary>
    [JsonPropertyName("credentialId")]
    [JsonConverter(typeof(Base64UrlConverter))]
    public byte[]? CredentialId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser User { get; set; } = null!;


    public string ProviderName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? LastUsedDate { get; set; }
    public string? LastUsedDevice { get; set; }
    public string? LastUsedLocation { get; set; }
    public string? LastUsedIpAddress { get; set; }


    public string? JsonData { get; set; }
    public Guid AaGuid { get; internal set; }
    public byte[]? UserHandle { get; internal set; }
}

public class UserCredentialJson
{

    /// <summary>
    /// The Credential ID of the public key credential source.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(Base64UrlConverter))]
    public byte[] Id { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The credential public key of the public key credential source.
    /// </summary>
    [JsonPropertyName("publicKey")]
    [JsonConverter(typeof(Base64UrlConverter))]
    public byte[]? PublicKey { get; set; }

    /// <summary>
    /// The latest value of the signature counter in the authenticator data from any ceremony using the public key credential source.
    /// </summary>
    [JsonPropertyName("signCount")]
    public uint SignCount { get; set; }

    /// <summary>
    /// The value returned from getTransports() when the public key credential source was registered.
    /// </summary>
    [JsonPropertyName("transports")]
    public Fido2NetLib.Objects.AuthenticatorTransport[]? Transports { get; set; }

    /// <summary>
    /// The value of the BE flag when the public key credential source was created.
    /// </summary>
    [JsonPropertyName("isBackupEligible")]
    public bool IsBackupEligible { get; set; }

    /// <summary>
    /// The latest value of the BS flag in the authenticator data from any ceremony using the public key credential source.
    /// </summary>
    [JsonPropertyName("isBackedUp")]
    public bool IsBackedUp { get; set; }

    /// <summary>
    /// The value of the attestationObject attribute when the public key credential source was registered. 
    /// Storing this enables the Relying Party to reference the credential's attestation statement at a later time.
    /// </summary>
    [JsonPropertyName("attestationObject")]
    [JsonConverter(typeof(Base64UrlConverter))]
    public byte[]? AttestationObject { get; set; }

    /// <summary>
    /// The value of the clientDataJSON attribute when the public key credential source was registered. 
    /// Storing this in combination with the above attestationObject item enables the Relying Party to re-verify the attestation signature at a later time.
    /// </summary>
    [JsonPropertyName("attestationClientDataJson")]
    public string? AttestationClientDataJson { get; set; }

    // [JsonPropertyName("devicePublicKeys")]
    // [JsonConverter(typeof(Base64UrlConverter))]
    // public byte[]? DevicePublicKey { get; set; }
    // public List<DevicePublicKey>? DevicePublicKeys { get; set; }


    [JsonPropertyName("userId")]
    [JsonConverter(typeof(Base64UrlConverter))]
    public byte[]? UserId { get; set; }

    [JsonPropertyName("descriptor")]
    // public Models.PublicKeyCredentialDescriptorModel? Descriptor { get; set; }
     public PublicKeyCredentialDescriptor Descriptor => new(PublicKeyCredentialType.PublicKey, Id, Transports);


    [JsonPropertyName("userHandle")]
    public byte[]? UserHandle { get; set; }

    [JsonPropertyName("attestationFormat")]
    public string AttestationFormat { get; set; } = string.Empty;

    [JsonPropertyName("regDate")]
    public DateTimeOffset RegDate { get; set; }

    [JsonPropertyName("aaGuid")]
    public Guid AaGuid { get; set; }

}


public class DevicePublicKey
{
    [JsonPropertyName("key")]
    [JsonConverter(typeof(Base64UrlConverter))]
    public byte[]? Key { get; set; }


}