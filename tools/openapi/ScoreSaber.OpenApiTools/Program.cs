using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var options = PreprocessOptions.Parse(args);
var sourceJson = await ReadSourceAsync(options.Input);
var document = JsonNode.Parse(sourceJson)?.AsObject()
    ?? throw new InvalidOperationException("OpenAPI document was empty.");

var components = new JsonObject();
var paths = new JsonObject();

foreach (var operation in Operations.All) {
    var sourceOperation = document["paths"]?[operation.Path]?[operation.Method]?.DeepClone()?.AsObject()
        ?? throw new InvalidOperationException($"Missing OpenAPI operation: {operation.Method.ToUpperInvariant()} {operation.Path}");

    sourceOperation["operationId"] = operation.OperationId;
    sourceOperation["responses"] = BuildResponses(sourceOperation, operation, components);

    if (operation.RequestSchemaName != null) {
        PromoteRequestBody(sourceOperation, operation.RequestSchemaName, components);
    }

    if (operation.InjectGameSessionHeaders) {
        InjectGameSessionHeaders(sourceOperation);
    }

    if (!paths.TryGetPropertyValue(operation.Path, out var pathNode) || pathNode == null) {
        pathNode = new JsonObject();
        paths[operation.Path] = pathNode;
    }

    pathNode.AsObject()[operation.Method] = sourceOperation;
}

document["paths"] = paths;
document["components"] = new JsonObject {
    ["schemas"] = components
};

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.Output))!);
await File.WriteAllTextAsync(
    options.Output,
    document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
    Encoding.UTF8);

static async Task<string> ReadSourceAsync(string input) {
    if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        input.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
        using var client = new HttpClient();
        return await client.GetStringAsync(input);
    }

    return await File.ReadAllTextAsync(input);
}

static JsonObject BuildResponses(JsonObject sourceOperation, OperationSpec operation, JsonObject components) {
    var responses = new JsonObject();
    var sourceResponses = sourceOperation["responses"]?.AsObject()
        ?? throw new InvalidOperationException($"Missing responses for {operation.OperationId}");

    foreach (var response in sourceResponses) {
        if (!response.Key.StartsWith("2", StringComparison.Ordinal)) {
            continue;
        }

        var responseObject = response.Value?.DeepClone()?.AsObject()
            ?? throw new InvalidOperationException($"Invalid response for {operation.OperationId}");

        if (operation.ResponseSchemaName != null) {
            PromoteContentSchemas(responseObject, operation.ResponseSchemaName, components);
        }

        responses[operation.SuccessStatusCode ?? response.Key] = responseObject;
    }

    if (responses.Count == 0) {
        throw new InvalidOperationException($"No success responses found for {operation.OperationId}");
    }

    return responses;
}

static void PromoteRequestBody(JsonObject operation, string schemaName, JsonObject components) {
    var requestBody = operation["requestBody"]?.AsObject();
    if (requestBody == null) {
        return;
    }

    PromoteContentSchemas(requestBody, schemaName, components);
}

static void PromoteContentSchemas(JsonObject owner, string schemaName, JsonObject components) {
    var content = owner["content"]?.AsObject();
    if (content == null) {
        return;
    }

    foreach (var mediaType in content) {
        var mediaTypeObject = mediaType.Value?.AsObject();
        var schema = mediaTypeObject?["schema"];
        if (mediaTypeObject == null || schema == null) {
            continue;
        }

        mediaTypeObject["schema"] = PromoteOperationSchema(schema, schemaName, components);
    }
}

static JsonNode PromoteOperationSchema(JsonNode schema, string schemaName, JsonObject components) {
    if (schema is not JsonObject schemaObject) {
        return schema.DeepClone();
    }

    if (GetString(schemaObject, "type") == "array" && schemaObject["items"] != null) {
        var arraySchema = schemaObject.DeepClone().AsObject();
        arraySchema["items"] = PromoteSchema(arraySchema["items"]!, schemaName, components);
        return arraySchema;
    }

    return PromoteSchema(schemaObject, schemaName, components);
}

static JsonNode PromoteSchema(JsonNode schema, string schemaName, JsonObject components) {
    if (schema is not JsonObject schemaObject) {
        return schema.DeepClone();
    }

    if (schemaObject.ContainsKey("$ref")) {
        return schema.DeepClone();
    }

    var type = GetString(schemaObject, "type");
    if (type == "array" && schemaObject["items"] != null) {
        var arraySchema = schemaObject.DeepClone().AsObject();
        arraySchema["items"] = PromoteSchema(arraySchema["items"]!, $"{schemaName}Item", components);
        return arraySchema;
    }

    if (type != "object" && schemaObject["properties"] == null) {
        return schema.DeepClone();
    }

    var component = schemaObject.DeepClone().AsObject();
    var nullable = component["nullable"]?.DeepClone();
    component.Remove("nullable");

    if (component["properties"] is JsonObject properties) {
        foreach (var property in properties.ToList()) {
            if (property.Value == null) {
                continue;
            }

            properties[property.Key] = PromoteSchema(
                property.Value,
                schemaName + ToPascalCase(property.Key),
                components);
        }
    }

    if (component["items"] != null) {
        component["items"] = PromoteSchema(component["items"]!, $"{schemaName}Item", components);
    }

    components[schemaName] = component;

    var reference = new JsonObject {
        ["$ref"] = $"#/components/schemas/{schemaName}"
    };

    if (nullable != null) {
        reference["nullable"] = nullable;
    }

    return reference;
}

static void InjectGameSessionHeaders(JsonObject operation) {
    var parameters = operation["parameters"] as JsonArray;
    if (parameters == null) {
        parameters = new JsonArray();
        operation["parameters"] = parameters;
    }

    AddHeaderParameter(parameters, "x-session-id", "Game session ID from auth.");
    AddHeaderParameter(parameters, "x-session-key", "Game session key from auth.");
}

static void AddHeaderParameter(JsonArray parameters, string name, string description) {
    foreach (var parameter in parameters) {
        if (parameter?["in"]?.GetValue<string>() == "header" &&
            parameter?["name"]?.GetValue<string>() == name) {
            return;
        }
    }

    parameters.Add(new JsonObject {
        ["name"] = name,
        ["in"] = "header",
        ["required"] = false,
        ["description"] = description,
        ["schema"] = new JsonObject {
            ["type"] = "string"
        }
    });
}

static string? GetString(JsonObject node, string name) {
    return node.TryGetPropertyValue(name, out var value) ? value?.GetValue<string>() : null;
}

static string ToPascalCase(string value) {
    var builder = new StringBuilder();
    var capitalize = true;

    foreach (var character in value) {
        if (!char.IsLetterOrDigit(character)) {
            capitalize = true;
            continue;
        }

        builder.Append(capitalize ? char.ToUpperInvariant(character) : character);
        capitalize = false;
    }

    if (builder.Length == 0) {
        return "Value";
    }

    if (char.IsDigit(builder[0])) {
        builder.Insert(0, "Value");
    }

    return builder.ToString();
}

internal sealed class PreprocessOptions {
    public string Input { get; private set; } = "https://scoresaber.com/api/openapi.json";
    public string Output { get; private set; } = "openapi-plugin.generated.json";

    public static PreprocessOptions Parse(string[] args) {
        var options = new PreprocessOptions();

        for (var i = 0; i < args.Length; i++) {
            switch (args[i]) {
                case "--input":
                    options.Input = args[++i];
                    break;
                case "--output":
                    options.Output = args[++i];
                    break;
                default:
                    throw new InvalidOperationException($"Unknown argument: {args[i]}");
            }
        }

        return options;
    }
}

internal sealed class OperationSpec {
    public OperationSpec(
        string path,
        string method,
        string operationId,
        string? requestSchemaName,
        string? responseSchemaName,
        string? successStatusCode = null,
        bool injectGameSessionHeaders = false) {
        Path = path;
        Method = method;
        OperationId = operationId;
        RequestSchemaName = requestSchemaName;
        ResponseSchemaName = responseSchemaName;
        SuccessStatusCode = successStatusCode;
        InjectGameSessionHeaders = injectGameSessionHeaders;
    }

    public string Path { get; }
    public string Method { get; }
    public string OperationId { get; }
    public string? RequestSchemaName { get; }
    public string? ResponseSchemaName { get; }
    public string? SuccessStatusCode { get; }
    public bool InjectGameSessionHeaders { get; }
}

internal static class Operations {
    public static readonly OperationSpec[] All = {
        new("/api/v2/game/auth", "post", "AuthenticateGame", "GameAuthenticateRequest", "GameAuthenticateResponse", "201"),
        new("/api/v2/game/official-builds", "post", "RegisterOfficialBuild", "GameOfficialBuildRequest", "GameOfficialBuildResponse", "201"),
        new("/api/v2/game/upload", "post", "UploadScore", "GameUploadRequest", "GameUploadResponse"),
        new("/api/v2/leaderboards/hash/{hash}/{mode}/{difficulty}", "get", "GetLeaderboard", null, "LeaderboardResponse"),
        new("/api/v2/leaderboards/hash/{hash}/{mode}/{difficulty}/scores", "get", "GetLeaderboardScores", null, "LeaderboardScoresResponse", injectGameSessionHeaders: true),
        new("/api/v2/players", "get", "GetPlayers", null, "PlayerListResponse", injectGameSessionHeaders: true),
        new("/api/v2/players/{id}", "get", "GetPlayer", null, "PlayerProfileResponse"),
        new("/api/v2/players/{id}/basic", "get", "GetPlayerBasic", null, "PlayerBasicProfileResponse"),
        new("/api/v2/players/{id}/history", "get", "GetPlayerHistory", null, "PlayerHistoryEntry"),
        new("/api/v2/players/{id}/global-history", "get", "GetGlobalPlayerHistory", null, "GlobalPlayerHistoryEntry"),
        new("/api/v2/realms", "get", "GetRealms", null, "RealmSummary"),
        new("/api/v2/realms/{id}", "get", "GetRealm", null, "RealmDetailsResponse"),
        new("/api/v2/scores/{id}/replay", "get", "DownloadReplay", null, null),
    };
}
