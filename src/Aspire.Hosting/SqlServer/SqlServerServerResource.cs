// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SQL Server container.
/// </summary>
public class SqlServerServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="password">The SQL Sever password, or <see langword="null"/> to generate a random password.</param>
    public SqlServerServerResource(string name, string? password = null) : base(name)
    {
        PrimaryEndpoint = new(this, PrimaryEndpointName);
        PasswordInput = new(this, "password");

        // The password must be at least 8 characters long and contain characters from three of the following four sets: Uppercase letters, Lowercase letters, Base 10 digits, and Symbols
        Annotations.Add(InputAnnotation.CreateDefaultPasswordInput(password, minLower: 1, minUpper: 1, minNumeric: 1));
    }

    /// <summary>
    /// Gets the primary endpoint for the Redis server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    internal InputReference PasswordInput { get; }

    /// <summary>
    /// Gets the password for the SQL Server container resource.
    /// </summary>
    public string Password => PasswordInput.Input.Value ?? throw new InvalidOperationException("Password cannot be null.");

    private ReferenceExpression ConnectionString =>
        ReferenceExpression.Create(
            $"Server={PrimaryEndpoint.Property(EndpointProperty.IPV4Host)},{PrimaryEndpoint.Property(EndpointProperty.Port)};User ID=sa;Password={PasswordInput};TrustServerCertificate=true");

    /// <summary>
    /// Gets the connection string expression for the SQL Server for the manifest.
    /// </summary>
    public string? ConnectionStringExpression
    {
        get
        {
            if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
            {
                return connectionStringAnnotation.Resource.ConnectionStringExpression;
            }

            return ConnectionString.ValueExpression;
        }
    }

    /// <summary>
    /// Gets the connection string for the SQL Server.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A connection string for the SQL Server in the form "Server=host,port;User ID=sa;Password=password;TrustServerCertificate=true".</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken);
        }

        return ConnectionString.GetValueAsync(cancellationToken);
    }

    private readonly Dictionary<string, string> _databases = new(StringComparers.ResourceName);

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
    }
}
