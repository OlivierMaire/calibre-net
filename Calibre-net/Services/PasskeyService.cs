using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Calibre_net.Api.Endpoints;
using Calibre_net.Client.Services;
using Calibre_net.Data;
using Calibre_net.Models;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.FileProviders;
using MudBlazor.Extensions;

namespace Calibre_net.Services;

[ScopedRegistration]
public class PasskeyService
{
    private readonly IFido2 fidoLib;
    private readonly ApplicationDbContext dbContext;
    private readonly AuthenticationStateProvider authenticationStateProvider;
    private static DbContextOptions<ApplicationDbContext> DbContextOptions = null!;
    private static ApplicationDbContext staticDbContext = null!;

    private Dictionary<string, AaGuidModel>? AaGuids = null;

    public PasskeyService(Fido2NetLib.IFido2 fidoLib,
    ApplicationDbContext dbContext,
    DbContextOptions<ApplicationDbContext> dbContextOptions,
    AuthenticationStateProvider authenticationStateProvider)
    {
        this.fidoLib = fidoLib;
        this.dbContext = dbContext;
        this.authenticationStateProvider = authenticationStateProvider;
        DbContextOptions = dbContextOptions;
        staticDbContext = new ApplicationDbContext(DbContextOptions);
    }


    public async Task<CredentialCreateOptions> RequestPasskeyAsync()
    {

        // 1. Get user from DB 
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var fidoUser = new Fido2NetLib.Fido2User()
        {
            DisplayName = authState.User.Identity?.Name,
            Id = Guid.Parse(userId).ToByteArray(),
            Name = authState.User.FindFirst(c => c.Type == ClaimTypes.Email)?.Value,
        };

        // 2. Get user existing keys by username
#pragma warning disable CS8604 // Possible null reference argument.
        List<PublicKeyCredentialDescriptor> existingKeys = dbContext.UserCredentials.Where(uc => uc.UserId == userId && uc.CredentialId != null)
        .Select(uc => new PublicKeyCredentialDescriptor(uc.CredentialId)).ToList();
#pragma warning restore CS8604 // Possible null reference argument.

        // 3. Create options
        var credentials = fidoLib.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fidoUser,
            ExcludeCredentials = existingKeys,
            Extensions = null,
            AuthenticatorSelection = new AuthenticatorSelection()
            {
                AuthenticatorAttachment = AuthenticatorAttachment.Platform,
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Preferred
            },
            AttestationPreference = AttestationConveyancePreference.None
        });

        return credentials;
    }

    public async Task<bool> StoreCredentialsAsync(CredentialCreateOptions options,
     AuthenticatorAttestationRawResponse rawResponse,
    CancellationToken cancellationToken = default(CancellationToken))
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        // var userCredentials = dbContext.UserCredentials.Where(uc => uc.UserId == userId).ToList();


        // 1. Create callback so that lib can verify credential id is unique to this user
        Fido2NetLib.IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, cancellationToken) =>
        {
            return !await dbContext.UserCredentials.AnyAsync(uc => uc.CredentialId == args.CredentialId, cancellationToken: cancellationToken);
        };

        // 2. Verify and make the credentials
        var success = await fidoLib.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = rawResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = callback
        }, cancellationToken:
        cancellationToken);

        // 3. Store the credentials in db
        if (success != null)
        {
            dbContext.UserCredentials.Add(new UserCredential
            {
                UserId = userId,
                ProviderName = (await GetPasskeyProviderAsync(success.AaGuid))?.Name ?? string.Empty,
                CreatedDate = DateTimeOffset.UtcNow,
                CredentialId = success.Id,
                AaGuid = success.AaGuid,
                UserHandle = success.User.Id,
                JsonData = JsonSerializer.Serialize(
                 new UserCredentialJson
                 {
                     Id = success.Id,
                    //  Descriptor = new Models.PublicKeyCredentialDescriptorModel(success.Id),
                     PublicKey = success.PublicKey,
                     UserHandle = success.User.Id,
                     SignCount = success.SignCount,
                     AttestationFormat = success.AttestationFormat,
                     RegDate = DateTimeOffset.UtcNow,
                     AaGuid = success.AaGuid,
                     Transports = success.Transports,
                     IsBackupEligible = success.IsBackupEligible,
                     IsBackedUp = success.IsBackedUp,
                     AttestationObject = success.AttestationObject,
                     AttestationClientDataJson = System.Text.Encoding.UTF8.GetString(success.AttestationClientDataJson),
                     //  DevicePublicKeys = success.DevicePublicKey != null ?
                     //     new List<DevicePublicKey> { new DevicePublicKey { Key = success.DevicePublicKey } } : new List<DevicePublicKey>(),
                     UserId = options.User.Id,
                 })
            });

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                return false;
            }
            return true;
        }
        return false;

    }

    private async Task<AaGuidModel?> GetPasskeyProviderAsync(Guid aaGuid)
    {
        if (AaGuids == null || AaGuids.Count == 0)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "calibre-net.Data.aaguid.json";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    AaGuids = await JsonSerializer.DeserializeAsync<Dictionary<string, AaGuidModel>>(stream);

                }
            }

        }
        if (AaGuids != null)
        {
            return AaGuids.ContainsKey(aaGuid.ToString()) ? AaGuids[aaGuid.ToString()] : null;
        }


        return null;
    }

    public async Task<List<PasskeyModel>> GetMyPasskeysAsync()
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;


        var passkeys = dbContext.UserCredentials.Where(uc => uc.UserId == userId).Select(uc => new PasskeyModel
        {
            Id = uc.Id,
            AaGuid = uc.AaGuid,
            ProviderName = uc.ProviderName,
            Name = uc.Name,
            CreatedDate = uc.CreatedDate,
            LastUsedDate = uc.LastUsedDate,
            LastUsedDevice = uc.LastUsedDevice,
            LastUsedLocation = uc.LastUsedLocation


        }).ToList();

        foreach (var passkey in passkeys)
        {
            passkey.ProviderIconLight = (await GetPasskeyProviderAsync(passkey.AaGuid))?.IconLight;
            passkey.ProviderIconLight = (await GetPasskeyProviderAsync(passkey.AaGuid))?.IconDark;
        }

        return passkeys;
    }

    public async Task DeletePasskey(int id)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        var passkey = dbContext.UserCredentials.FirstOrDefault(uc => uc.UserId == userId && uc.Id == id);
        if (passkey != null)
        {
            dbContext.UserCredentials.Remove(passkey);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task EditNameAsync(Models.PasskeyModel passkeyModel)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        var passkey = dbContext.UserCredentials.FirstOrDefault(uc => uc.UserId == userId && uc.Id == passkeyModel.Id);
        if (passkey != null)
            passkey.Name = passkeyModel.Name;

        await dbContext.SaveChangesAsync();

    }

    public AssertionOptions? GenerateAssertionOption(string UserName)
    {
        // 1. Get user from DB 
        var userId = dbContext.Users.FirstOrDefault(u => u.Email == UserName);

        List<PublicKeyCredentialDescriptor> existingKeys = new List<PublicKeyCredentialDescriptor>();

        if (userId != null)
        {
            // 2. Get user existing keys by username
#pragma warning disable CS8604 // Possible null reference argument.
            existingKeys = dbContext.UserCredentials.Where(uc => uc.UserId == userId.Id && uc.CredentialId != null)
             .Select(uc => new PublicKeyCredentialDescriptor(uc.CredentialId)).ToList();
#pragma warning restore CS8604 // Possible null reference argument.
        }
        // 3. Create options
        var credentials = fidoLib.GetAssertionOptions(new GetAssertionOptionsParams{
            AllowedCredentials = existingKeys,
            UserVerification = UserVerificationRequirement.Preferred,
            Extensions = null,
        });


        return credentials;
    }

    public UserCredentialJson? GetCredentialById(byte[] id)
    {
        var userCredential = dbContext.UserCredentials.FirstOrDefault(uc => uc.CredentialId == id);
        if (userCredential == null)
            return null;

        var credentialData = JsonSerializer.Deserialize<UserCredentialJson>(userCredential.JsonData ?? string.Empty);
        return credentialData;
    }

    public async Task<VerifyAssertionResult> MakeAssertionAsync(AuthenticatorAssertionRawResponse assertionResponse,
        AssertionOptions originalOptions,
        byte[] storedPublicKey,
        // List<byte[]> storedDevicePublicKeys,
        uint storedSignatureCounter,
        CancellationToken cancellationToken = default)
    {

        var res = await fidoLib.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = assertionResponse,
            IsUserHandleOwnerOfCredentialIdCallback = UserHandleOwnerOfCredentialIdAsync,
            OriginalOptions = originalOptions,
            StoredPublicKey = storedPublicKey,
            StoredSignatureCounter = storedSignatureCounter,
        },
                      //   storedDevicePublicKeys,
                      cancellationToken: cancellationToken);

        return res;
    }

    private async Task<bool> UserHandleOwnerOfCredentialIdAsync(IsUserHandleOwnerOfCredentialIdParams args, CancellationToken cancellationToken)
    {

        var storedCreds = await dbContext.UserCredentials.Where(uc => uc.UserHandle == args.UserHandle).ToListAsync(cancellationToken);
        return storedCreds.Exists(c => c.CredentialId != null && c.CredentialId.SequenceEqual(args.CredentialId));
    }

    public async Task UpdateCountersAsync(byte[] credentialId, uint signCount)
    {
        var credential = dbContext.UserCredentials.FirstOrDefault(uc => uc.CredentialId == credentialId);

        if (credential != null)
        {
            credential.LastUsedDate = DateTimeOffset.UtcNow;
            var credentialData = JsonSerializer.Deserialize<UserCredentialJson>(credential.JsonData ?? string.Empty);
            if (credentialData != null)
            {
                credentialData.SignCount = signCount;
                // credentialData.DevicePublicKeys ??= [];
                // credentialData.DevicePublicKeys.Add(new DevicePublicKey { Key = devicePublicKey });
                credential.JsonData = JsonSerializer.Serialize(credentialData);
            }
            await dbContext.SaveChangesAsync();
        }
    }

    public ApplicationUser? GetUserFromCredential(byte[] credentialId)
    {
        return dbContext.UserCredentials.Where(uc => uc.CredentialId == credentialId).Select(uc => uc.User).FirstOrDefault();

    }

}