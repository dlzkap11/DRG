#!/usr/bin/env node
const { execSync } = require('child_process');

try {
  const output = execSync('git diff --check', { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] });
  process.stdout.write(output || '');
  process.exit(0);
} catch (error) {
  const stdout = error.stdout ? error.stdout.toString() : '';
  const stderr = error.stderr ? error.stderr.toString() : '';
  process.stderr.write('DRG hook: git diff --check found a problem.\n');
  process.stderr.write(stdout);
  process.stderr.write(stderr);
  process.exit(2);
}
