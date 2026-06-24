# Contributing

## Style

- Follow `.editorconfig`
- Keep warnings clean
- Keep generated API files in `src/Core/Api/Generated` generated only
- Keep ScoreSaber routes behind `ScoreSaberUrls` or local URL helpers

## API Client

Generated API files live in `src/Core/Api/Generated`

Do not edit them directly. Regenerate from ScoreSaber's OpenAPI spec:

```powershell
tools/openapi/generate-openapi.ps1
```

## Commits

Our commit style is `{feature}: {change_summary} (#{issue_number})`

Omit the issue suffix when there is no issue

Example:

```text
leaderboards: fix score row alignment (#12)
upload: keep replay save on upload failure
```
