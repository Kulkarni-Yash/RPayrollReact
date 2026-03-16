# RPayroll Project MCP

This folder contains a lightweight MCP server scaffold for the `RPayrollReact` workspace.

## What It Does

It exposes project-aware tools that an MCP-capable host can give to a model:

- `project_info`
- `list_files`
- `search_files`
- `read_file`
- `write_file`
- `replace_text`
- `append_audit_log`

The audit tool writes to:

- `/Users/Yash/Dev/RPayrollReact/docs/ollama-change-log.json`

## Important Constraint

Ollama does **not** automatically use MCP tools just because this folder exists.
You still need an MCP-aware host/client that:

1. launches this server over stdio
2. gives the tools to the model
3. decides when tool calls are allowed

So this is the project MCP scaffold, not the full orchestration layer.

## Start the MCP Server

From the repo root:

```bash
cd /Users/Yash/Dev/RPayrollReact/mcp/rpayroll-project-mcp
node server.mjs
```

Or:

```bash
npm start
```

No extra package install is required for this scaffold.

## Environment Overrides

Optional environment variables:

- `RPAYROLL_MCP_ROOT`
- `RPAYROLL_MCP_AUDIT_LOG`

Defaults:

- workspace root: `/Users/Yash/Dev/RPayrollReact`
- audit log: `/Users/Yash/Dev/RPayrollReact/docs/ollama-change-log.json`

## Example MCP Client Config

See:

- `/Users/Yash/Dev/RPayrollReact/mcp/rpayroll-project-mcp/mcp-config.example.json`

You will need to adapt it to the MCP-capable client you use.

## Tool Notes

### `read_file`
Reads a file, with optional line slicing.

### `write_file`
Writes a full file under the workspace root.

### `replace_text`
Replaces all matches of a text block in one file.

### `append_audit_log`
Appends a structured JSON entry to the audit log. A good entry shape is:

```json
{
  "projectName": "RPayrollReact",
  "branch": "codex/ollama-autoupdates",
  "model": "deepseek-coder:6.7b",
  "prompt": "Add IFSC validation to employee save flow",
  "summary": "Updated backend validation and frontend toast handling.",
  "files": [
    {
      "fileName": "EmployeeService.cs",
      "filePath": "/Users/Yash/Dev/RPayrollReact/RPayroll.Repositories/Services/EmployeeService.cs",
      "changeType": "modified",
      "totalLines": 214,
      "linesChangedCount": 18,
      "lineNumbers": [12, 13, 48, 49]
    }
  ]
}
```

## Recommended Next Step

When you are ready for controlled AI edits, the clean architecture is:

- frontend chat UI
- backend orchestrator
- Ollama model
- MCP server for file tools
- audit log append after every write/update

That gives you the same kind of safe, inspectable flow you asked for.
