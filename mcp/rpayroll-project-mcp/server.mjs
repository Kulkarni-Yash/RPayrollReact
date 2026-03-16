import fs from 'node:fs';
import fsp from 'node:fs/promises';
import path from 'node:path';
import os from 'node:os';
import { spawnSync } from 'node:child_process';

const PROJECT_ROOT = path.resolve(process.env.RPAYROLL_MCP_ROOT || process.cwd(), '..', '..');
const AUDIT_LOG_PATH = path.resolve(
  process.env.RPAYROLL_MCP_AUDIT_LOG ||
    path.join(PROJECT_ROOT, 'docs', 'ollama-change-log.json'),
);

const SERVER_INFO = {
  name: 'rpayroll-project-mcp',
  version: '0.1.0',
};

const EXCLUDED_DIRS = new Set(['.git', 'node_modules', 'bin', 'obj', 'dist']);
const ALLOWED_ROOT = path.resolve(PROJECT_ROOT);

function send(message) {
  const json = JSON.stringify(message);
  const payload = `Content-Length: ${Buffer.byteLength(json, 'utf8')}\r\n\r\n${json}`;
  process.stdout.write(payload);
}

function respond(id, result) {
  send({ jsonrpc: '2.0', id, result });
}

function respondError(id, code, message, data) {
  send({ jsonrpc: '2.0', id, error: { code, message, data } });
}

function toTextResult(text) {
  return {
    content: [
      {
        type: 'text',
        text,
      },
    ],
  };
}

function humanizeFilePath(filePath) {
  return filePath.replace(`${ALLOWED_ROOT}${path.sep}`, '');
}

function resolveSafePath(relativePath = '.') {
  const cleaned = relativePath.trim() || '.';
  const resolved = path.resolve(ALLOWED_ROOT, cleaned);
  if (!resolved.startsWith(ALLOWED_ROOT)) {
    throw new Error('Path escapes the project root.');
  }
  return resolved;
}

async function pathExists(targetPath) {
  try {
    await fsp.access(targetPath);
    return true;
  } catch {
    return false;
  }
}

async function listFilesRecursive(rootDir, options = {}, currentDepth = 0, baseDir = rootDir, acc = []) {
  const { maxDepth = 4, extensions = [] } = options;
  if (currentDepth > maxDepth) {
    return acc;
  }

  const entries = await fsp.readdir(rootDir, { withFileTypes: true });
  for (const entry of entries) {
    if (entry.name.startsWith('.') && entry.name !== '.env' && entry.name !== '.gitignore') {
      continue;
    }

    const fullPath = path.join(rootDir, entry.name);
    const relative = path.relative(baseDir, fullPath).split(path.sep).join('/');

    if (entry.isDirectory()) {
      if (EXCLUDED_DIRS.has(entry.name)) {
        continue;
      }
      await listFilesRecursive(fullPath, options, currentDepth + 1, baseDir, acc);
      continue;
    }

    if (extensions.length > 0 && !extensions.some((ext) => entry.name.endsWith(ext))) {
      continue;
    }

    acc.push(relative);
  }

  return acc;
}

function inferProjectSummary() {
  const solutionPath = path.join(ALLOWED_ROOT, 'RPayroll.sln');
  const uiPath = path.join(ALLOWED_ROOT, 'rpayroll-ui');
  return {
    projectName: 'RPayrollReact',
    rootPath: ALLOWED_ROOT,
    hasSolution: fs.existsSync(solutionPath),
    hasUi: fs.existsSync(uiPath),
    platform: os.platform(),
  };
}

function readFileLines(content, startLine, endLine) {
  const lines = content.split(/\r?\n/);
  const start = Math.max((startLine || 1) - 1, 0);
  const end = Math.min(endLine || lines.length, lines.length);
  return lines.slice(start, end).join('\n');
}

async function searchWithRipgrep(query, cwd) {
  const args = ['-n', '--hidden', '--glob', '!.git', '--glob', '!node_modules', '--glob', '!bin', '--glob', '!obj', query, '.'];
  const result = spawnSync('rg', args, { cwd, encoding: 'utf8' });
  if (result.error) {
    throw result.error;
  }
  if (result.status !== 0 && result.status !== 1) {
    throw new Error(result.stderr || 'rg search failed');
  }
  return result.stdout.trim();
}

async function fallbackSearch(query, cwd, maxDepth = 4) {
  const files = await listFilesRecursive(cwd, { maxDepth });
  const matches = [];
  for (const relative of files) {
    const fullPath = path.join(cwd, relative);
    const content = await fsp.readFile(fullPath, 'utf8');
    const lines = content.split(/\r?\n/);
    lines.forEach((line, index) => {
      if (line.includes(query)) {
        matches.push(`${relative}:${index + 1}:${line}`);
      }
    });
  }
  return matches.join('\n');
}

async function ensureAuditLog() {
  await fsp.mkdir(path.dirname(AUDIT_LOG_PATH), { recursive: true });
  if (!(await pathExists(AUDIT_LOG_PATH))) {
    await fsp.writeFile(
      AUDIT_LOG_PATH,
      JSON.stringify(
        {
          projectName: 'RPayrollReact',
          updatedAt: new Date().toISOString(),
          entries: [],
        },
        null,
        2,
      ),
      'utf8',
    );
  }
}

async function appendAuditLog(entry) {
  await ensureAuditLog();
  const raw = await fsp.readFile(AUDIT_LOG_PATH, 'utf8');
  const parsed = JSON.parse(raw);
  const nextEntry = {
    ...entry,
    timestamp: entry.timestamp || new Date().toISOString(),
  };
  parsed.updatedAt = nextEntry.timestamp;
  parsed.entries = Array.isArray(parsed.entries) ? parsed.entries : [];
  parsed.entries.push(nextEntry);
  await fsp.writeFile(AUDIT_LOG_PATH, JSON.stringify(parsed, null, 2), 'utf8');
  return nextEntry;
}

const toolHandlers = {
  async project_info() {
    return toTextResult(JSON.stringify(inferProjectSummary(), null, 2));
  },

  async list_files(args = {}) {
    const targetDir = resolveSafePath(args.directory || '.');
    const extensions = Array.isArray(args.extensions) ? args.extensions : [];
    const maxDepth = Number.isFinite(args.maxDepth) ? args.maxDepth : 4;
    const files = await listFilesRecursive(targetDir, { maxDepth, extensions: extensions.filter(Boolean) });
    return toTextResult(JSON.stringify({ directory: humanizeFilePath(targetDir), files }, null, 2));
  },

  async search_files(args = {}) {
    if (!args.query || typeof args.query !== 'string') {
      throw new Error('query is required');
    }
    const targetDir = resolveSafePath(args.directory || '.');
    let output = '';
    try {
      output = await searchWithRipgrep(args.query, targetDir);
    } catch {
      output = await fallbackSearch(args.query, targetDir, Number.isFinite(args.maxDepth) ? args.maxDepth : 4);
    }
    return toTextResult(output || 'No matches found.');
  },

  async read_file(args = {}) {
    if (!args.path || typeof args.path !== 'string') {
      throw new Error('path is required');
    }
    const targetPath = resolveSafePath(args.path);
    const content = await fsp.readFile(targetPath, 'utf8');
    const selected = readFileLines(content, args.startLine, args.endLine);
    return toTextResult(selected);
  },

  async write_file(args = {}) {
    if (!args.path || typeof args.path !== 'string') {
      throw new Error('path is required');
    }
    if (typeof args.content !== 'string') {
      throw new Error('content is required');
    }
    const targetPath = resolveSafePath(args.path);
    await fsp.mkdir(path.dirname(targetPath), { recursive: true });
    await fsp.writeFile(targetPath, args.content, 'utf8');
    const totalLines = args.content.split(/\r?\n/).length;
    return toTextResult(JSON.stringify({ path: humanizeFilePath(targetPath), totalLines }, null, 2));
  },

  async replace_text(args = {}) {
    if (!args.path || typeof args.path !== 'string') {
      throw new Error('path is required');
    }
    if (typeof args.search !== 'string' || typeof args.replace !== 'string') {
      throw new Error('search and replace are required');
    }
    const targetPath = resolveSafePath(args.path);
    const current = await fsp.readFile(targetPath, 'utf8');
    if (!current.includes(args.search)) {
      throw new Error('search text not found in file');
    }
    const updated = current.replaceAll(args.search, args.replace);
    await fsp.writeFile(targetPath, updated, 'utf8');
    return toTextResult(JSON.stringify({ path: humanizeFilePath(targetPath), changed: true }, null, 2));
  },

  async append_audit_log(args = {}) {
    if (!args.entry || typeof args.entry !== 'object') {
      throw new Error('entry is required');
    }
    const appended = await appendAuditLog(args.entry);
    return toTextResult(JSON.stringify(appended, null, 2));
  },
};

const toolDefinitions = [
  {
    name: 'project_info',
    description: 'Returns basic metadata about the RPayrollReact workspace.',
    inputSchema: { type: 'object', properties: {}, additionalProperties: false },
  },
  {
    name: 'list_files',
    description: 'Lists files inside the project root or a subdirectory.',
    inputSchema: {
      type: 'object',
      properties: {
        directory: { type: 'string', description: 'Relative directory from the workspace root.' },
        maxDepth: { type: 'number', description: 'Maximum recursion depth.' },
        extensions: {
          type: 'array',
          items: { type: 'string' },
          description: 'Optional file extensions such as .ts or .cs.',
        },
      },
      additionalProperties: false,
    },
  },
  {
    name: 'search_files',
    description: 'Searches the workspace for a text query.',
    inputSchema: {
      type: 'object',
      properties: {
        query: { type: 'string' },
        directory: { type: 'string' },
        maxDepth: { type: 'number' },
      },
      required: ['query'],
      additionalProperties: false,
    },
  },
  {
    name: 'read_file',
    description: 'Reads a file from the workspace. Optional line range supported.',
    inputSchema: {
      type: 'object',
      properties: {
        path: { type: 'string' },
        startLine: { type: 'number' },
        endLine: { type: 'number' },
      },
      required: ['path'],
      additionalProperties: false,
    },
  },
  {
    name: 'write_file',
    description: 'Writes a file inside the workspace.',
    inputSchema: {
      type: 'object',
      properties: {
        path: { type: 'string' },
        content: { type: 'string' },
      },
      required: ['path', 'content'],
      additionalProperties: false,
    },
  },
  {
    name: 'replace_text',
    description: 'Replaces all occurrences of one text block with another in a file.',
    inputSchema: {
      type: 'object',
      properties: {
        path: { type: 'string' },
        search: { type: 'string' },
        replace: { type: 'string' },
      },
      required: ['path', 'search', 'replace'],
      additionalProperties: false,
    },
  },
  {
    name: 'append_audit_log',
    description: 'Appends a structured change record to docs/ollama-change-log.json.',
    inputSchema: {
      type: 'object',
      properties: {
        entry: { type: 'object' },
      },
      required: ['entry'],
      additionalProperties: false,
    },
  },
];

function handleRequest(message) {
  const { id, method, params } = message;

  if (method === 'initialize') {
    respond(id, {
      protocolVersion: '2024-11-05',
      serverInfo: SERVER_INFO,
      capabilities: {
        tools: {},
      },
    });
    return;
  }

  if (method === 'notifications/initialized') {
    return;
  }

  if (method === 'tools/list') {
    respond(id, { tools: toolDefinitions });
    return;
  }

  if (method === 'tools/call') {
    const toolName = params?.name;
    const handler = toolHandlers[toolName];
    if (!handler) {
      respondError(id, -32601, `Unknown tool: ${toolName}`);
      return;
    }

    Promise.resolve(handler(params?.arguments || {}))
      .then((result) => respond(id, result))
      .catch((error) => respondError(id, -32000, error.message || 'Tool execution failed'));
    return;
  }

  respondError(id, -32601, `Method not found: ${method}`);
}

let buffer = Buffer.alloc(0);
process.stdin.on('data', (chunk) => {
  buffer = Buffer.concat([buffer, chunk]);

  while (true) {
    const headerEnd = buffer.indexOf('\r\n\r\n');
    if (headerEnd === -1) {
      break;
    }

    const headerText = buffer.slice(0, headerEnd).toString('utf8');
    const contentLengthMatch = headerText.match(/Content-Length:\s*(\d+)/i);
    if (!contentLengthMatch) {
      buffer = Buffer.alloc(0);
      break;
    }

    const contentLength = Number.parseInt(contentLengthMatch[1], 10);
    const totalLength = headerEnd + 4 + contentLength;
    if (buffer.length < totalLength) {
      break;
    }

    const body = buffer.slice(headerEnd + 4, totalLength).toString('utf8');
    buffer = buffer.slice(totalLength);

    try {
      const message = JSON.parse(body);
      handleRequest(message);
    } catch (error) {
      respondError(null, -32700, 'Parse error', error.message);
    }
  }
});

process.stdin.resume();
