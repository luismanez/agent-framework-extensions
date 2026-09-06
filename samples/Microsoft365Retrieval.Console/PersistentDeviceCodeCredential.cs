using Azure.Core;
using Azure.Identity;

internal static class PersistentDeviceCodeCredential
{
	private const string TokenCacheName = "Acterion.Microsoft365Retrieval.Console";
	private static readonly TokenRequestContext GraphTokenRequest = new(["https://graph.microsoft.com/.default"]);

	internal static string AuthenticationRecordPath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"Acterion",
		"Microsoft365Retrieval.Console",
		"authentication-record.json");

	internal static async Task<DeviceCodeCredential> CreateAsync(
		string tenantId,
		string clientId,
		Func<DeviceCodeInfo, CancellationToken, Task> deviceCodeCallback,
		CancellationToken cancellationToken)
	{
		AuthenticationRecordStore recordStore = new(AuthenticationRecordPath);
		AuthenticationRecord? authenticationRecord = await recordStore.LoadAsync(cancellationToken);

		if (authenticationRecord is not null &&
			(!string.Equals(authenticationRecord.TenantId, tenantId, StringComparison.OrdinalIgnoreCase) ||
			 !string.Equals(authenticationRecord.ClientId, clientId, StringComparison.OrdinalIgnoreCase)))
		{
			authenticationRecord = null;
		}

		DeviceCodeCredential credential = new(CreateOptions(
			tenantId,
			clientId,
			authenticationRecord,
			deviceCodeCallback));

		if (authenticationRecord is null)
		{
			authenticationRecord = await credential.AuthenticateAsync(GraphTokenRequest, cancellationToken);
			await recordStore.SaveAsync(authenticationRecord, cancellationToken);
		}

		return credential;
	}

	internal static DeviceCodeCredentialOptions CreateOptions(
		string tenantId,
		string clientId,
		AuthenticationRecord? authenticationRecord,
		Func<DeviceCodeInfo, CancellationToken, Task> deviceCodeCallback) =>
		new()
		{
			TenantId = tenantId,
			ClientId = clientId,
			AuthenticationRecord = authenticationRecord,
			DeviceCodeCallback = deviceCodeCallback,
			TokenCachePersistenceOptions = new TokenCachePersistenceOptions
			{
				Name = TokenCacheName,
			},
		};
}

internal sealed class AuthenticationRecordStore(string path)
{
	internal async Task<AuthenticationRecord?> LoadAsync(CancellationToken cancellationToken)
	{
		if (!File.Exists(path))
		{
			return null;
		}

		await using FileStream stream = new(
			path,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			bufferSize: 4096,
			useAsync: true);
		return await AuthenticationRecord.DeserializeAsync(stream, cancellationToken);
	}

	internal async Task SaveAsync(
		AuthenticationRecord authenticationRecord,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(authenticationRecord);

		string directory = Path.GetDirectoryName(path) ??
			throw new InvalidOperationException("The authentication record path must include a directory.");
		Directory.CreateDirectory(directory);

		string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
		try
		{
			await using (FileStream stream = new(
				temporaryPath,
				FileMode.CreateNew,
				FileAccess.Write,
				FileShare.None,
				bufferSize: 4096,
				useAsync: true))
			{
				await authenticationRecord.SerializeAsync(stream, cancellationToken);
			}

			if (!OperatingSystem.IsWindows())
			{
				File.SetUnixFileMode(
					temporaryPath,
					UnixFileMode.UserRead | UnixFileMode.UserWrite);
			}

			File.Move(temporaryPath, path, overwrite: true);
		}
		finally
		{
			File.Delete(temporaryPath);
		}
	}
}