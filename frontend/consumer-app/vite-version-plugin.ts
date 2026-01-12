import { execSync } from 'child_process';
import type { Plugin } from 'vite';

function getGitInfo() {
  try {
    const commit = execSync('git rev-parse --short HEAD', { encoding: 'utf-8' }).trim();
    const branch = execSync('git rev-parse --abbrev-ref HEAD', { encoding: 'utf-8' }).trim();

    let tag = '0.0.1'; // Default from package.json
    try {
      const gitTag = execSync('git describe --tags --abbrev=0', { encoding: 'utf-8' }).trim();
      tag = gitTag.startsWith('v') ? gitTag.substring(1) : gitTag;
    } catch {
      // No tags found, use default
    }

    return {
      version: tag,
      commit,
      branch,
      buildTime: new Date().toISOString(),
    };
  } catch (error) {
    // Fallback for environments without git
    return {
      version: '0.0.1',
      commit: 'dev',
      branch: 'unknown',
      buildTime: new Date().toISOString(),
    };
  }
}

export function viteVersionPlugin(): Plugin {
  const gitInfo = getGitInfo();

  return {
    name: 'vite-version-plugin',
    config() {
      return {
        define: {
          '__APP_VERSION__': JSON.stringify(gitInfo.version),
          '__GIT_COMMIT__': JSON.stringify(gitInfo.commit),
          '__GIT_BRANCH__': JSON.stringify(gitInfo.branch),
          '__BUILD_TIME__': JSON.stringify(gitInfo.buildTime),
        },
      };
    },
  };
}
