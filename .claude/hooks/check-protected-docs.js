#!/usr/bin/env node
const fs = require('fs');

let input = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => input += chunk);
process.stdin.on('end', () => {
  try {
    const data = JSON.parse(input || '{}');
    const path = data?.tool_input?.file_path || data?.tool_input?.path || '';
    const normalized = path.replaceAll('\\', '/');
    const protectedFiles = [
      'Docs/DRG_게임_기획서_v1.0.md',
      'Docs/DRG_게임_개발_명세서_v1.0.md'
    ];

    const matched = protectedFiles.some(file => normalized.endsWith(file));
    if (matched) {
      process.stderr.write(
        'DRG hook warning: You are modifying a source-of-truth specification file. ' +
        'Confirm that the user explicitly requested a specification change and update related docs if needed.\n'
      );
    }
    process.exit(0);
  } catch {
    process.exit(0);
  }
});
