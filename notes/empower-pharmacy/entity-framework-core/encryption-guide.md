# Securing Sensitive Data in EF Core and ASP.NET Core

To ensure fields are encrypted both in-flight and at-rest, you need a multi-layered approach involving transport security and application-level or database-level encryption.

## 1. Encryption-in-Flight (Transport Security)

Ensuring data is encrypted while moving between the client, server, and database.

### Client to Server (HTTPS)
In `Program.cs`, ensure HTTPS redirection is enabled:
```csharp
var app = builder.Build();
app.UseHttpsRedirection(); // Forces HTTPS
```

### Server to Database (SSL/TLS)
Update your connection string to enforce encryption. For SQL Server:
```text
Server=myServer;Database=myDb;User Id=myUser;Password=myPassword;Encrypt=True;TrustServerCertificate=False;
```
*   `Encrypt=True`: Ensures the connection is encrypted.
*   `TrustServerCertificate=False`: Ensures the server's SSL certificate is validated against a trusted CA.

---

## 2. Encryption-at-Rest (Column-Level Encryption)

The most common way to encrypt specific fields (like `SocialSecurityNumber` or `ApiKey`) before they reach the database is using **EF Core Value Converters**.

### Step A: Define an Encryption Provider
You should use a standard algorithm like AES-256.

```csharp
using System.Security.Cryptography;
using System.Text;

public interface IEncryptionProvider
{
    string Encrypt(string value);
    string Decrypt(string value);
}

public class AesEncryptionProvider : IEncryptionProvider
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionProvider(string key)
    {
        // Key should be 32 bytes for AES-256
        _key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        _iv = new byte[16]; // In production, use a unique IV per row or a consistent one for deterministic encryption
    }

    public string Encrypt(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(value);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var buffer = Convert.FromBase64String(value);
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(buffer);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
```

### Step B: Create the Value Converter
```csharp
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class EncryptedConverter : ValueConverter<string, string>
{
    public EncryptedConverter(IEncryptionProvider provider)
        : base(
            v => provider.Encrypt(v),
            v => provider.Decrypt(v))
    { }
}
```

### Step C: Apply to your DbContext
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    var encryptionProvider = new AesEncryptionProvider(Configuration["EncryptionKey"]);
    var converter = new EncryptedConverter(encryptionProvider);

    modelBuilder.Entity<User>()
        .Property(u => u.SocialSecurityNumber)
        .HasConversion(converter);
}
```

---

## 3. Alternative: Always Encrypted (SQL Server)

If you are using SQL Server, **Always Encrypted** is the "gold standard."
- **How it works**: The .NET Data Provider (SqlClient) transparently encrypts data before sending it to SQL Server and decrypts it after receiving it.
- **Key Management**: Keys can be stored in **Azure Key Vault** or the Windows Certificate Store.
- **Pros**: The database engine never sees the plaintext data or the encryption keys.
- **Setup**: Configured via SQL Server Management Studio (SSMS) or EF Core Migrations with custom SQL.

---

## 4. Key Management Best Practices

Never hardcode your encryption keys.
1. **Azure Key Vault / AWS KMS**: The most secure way to store and retrieve keys at runtime.
2. **Environment Variables**: Good for containerized apps, but ensure they are managed by a secret manager (like Kubernetes Secrets).
3. **User Secrets**: For local development only.

> [!WARNING]
> **Filtering & Searching**: Encrypted columns cannot be searched using `WHERE Column LIKE '%text%'` at the database level because the database only sees the ciphertext. You can only perform exact matches if your encryption is **Deterministic** (same input always yields same output), but this is less secure than **Randomized** encryption.
